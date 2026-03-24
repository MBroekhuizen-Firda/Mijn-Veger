using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class GridGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject TilePrefab;
    public GameObject MinePrefab;
    public GameObject CharacterPrefab;

    [Header("Materials")]
    public Material HiddenMaterial;
    public Material RevealedMaterial;

    [Header("Runtime")]
    public Tile[,] Grid { get; private set; }
    public Vector2Int StartPosition { get; private set; }
    public Vector2Int GoalPosition { get; private set; }

    private GameObject gridParent;
    private GameObject characterInstance;
    private int width;
    private int height;

    // Direction offsets for 8 neighbours (including diagonals)
    private static readonly Vector2Int[] Directions = new Vector2Int[]
    {
        new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1),
        new Vector2Int(0, -1),                          new Vector2Int(0, 1),
        new Vector2Int(1, -1),  new Vector2Int(1, 0),  new Vector2Int(1, 1)
    };

    public void GenerateGrid(int w, int h, int mineCount)
    {
        ClearGrid();

        width = w;
        height = h;
        Grid = new Tile[width, height];
        gridParent = new GameObject("Grid");

        // Create tiles
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 worldPos = GridToWorldPosition(x, y);
                GameObject tileObj = Instantiate(TilePrefab, worldPos, Quaternion.identity, gridParent.transform);
                tileObj.name = $"Tile_{x}_{y}";

                Tile tile = tileObj.GetComponent<Tile>();
                tile.GridPosition = new Vector2Int(x, y);
                tile.HiddenMaterial = HiddenMaterial;
                tile.RevealedMaterial = RevealedMaterial;

                Grid[x, y] = tile;
            }
        }

        // Pick start and goal positions
        StartPosition = new Vector2Int(0, Random.Range(0, height));
        GoalPosition = new Vector2Int(width - 1, Random.Range(0, height));
        Grid[GoalPosition.x, GoalPosition.y].IsGoal = true;

        // Place mines
        PlaceMines(mineCount);

        // Ensure at least one path exists from start to goal
        EnsurePath();

        // Calculate adjacent mine counts
        CalculateAdjacentMineCounts();

        // Set up mine prefabs on mine tiles
        SetupMinePrefabs();

        // Reveal the start tile
        Grid[StartPosition.x, StartPosition.y].Reveal();

        // Bake NavMesh at runtime (must happen before spawning the character)
        BakeNavMesh();

        // Spawn character
        SpawnCharacter();

        // Draw grid overlay lines
        CreateGridOverlay();
    }

    void PlaceMines(int mineCount)
    {
        List<Vector2Int> availablePositions = new List<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Don't place mines on start or goal
                if ((x == StartPosition.x && y == StartPosition.y) ||
                    (x == GoalPosition.x && y == GoalPosition.y))
                    continue;

                availablePositions.Add(new Vector2Int(x, y));
            }
        }

        // Shuffle and pick
        for (int i = availablePositions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = availablePositions[i];
            availablePositions[i] = availablePositions[j];
            availablePositions[j] = temp;
        }

        int count = Mathf.Min(mineCount, availablePositions.Count);
        for (int i = 0; i < count; i++)
        {
            Vector2Int pos = availablePositions[i];
            Grid[pos.x, pos.y].IsMine = true;
        }
    }

    void EnsurePath()
    {
        // BFS from start to goal; if no path, remove blocking mines
        while (!HasPath(StartPosition, GoalPosition))
        {
            // Find the blocked frontier and remove a mine on it
            RemoveBlockingMine();
        }
    }

    bool HasPath(Vector2Int from, Vector2Int to)
    {
        bool[,] visited = new bool[width, height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(from);
        visited[from.x, from.y] = true;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == to) return true;

            foreach (Vector2Int dir in Directions)
            {
                Vector2Int next = current + dir;
                if (IsInBounds(next) && !visited[next.x, next.y] && !Grid[next.x, next.y].IsMine)
                {
                    visited[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }
        }

        return false;
    }

    void RemoveBlockingMine()
    {
        // BFS from start, find frontier tiles that are mines, remove one
        bool[,] visited = new bool[width, height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        List<Vector2Int> frontierMines = new List<Vector2Int>();

        queue.Enqueue(StartPosition);
        visited[StartPosition.x, StartPosition.y] = true;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (Vector2Int dir in Directions)
            {
                Vector2Int next = current + dir;
                if (!IsInBounds(next) || visited[next.x, next.y]) continue;

                visited[next.x, next.y] = true;

                if (Grid[next.x, next.y].IsMine)
                {
                    frontierMines.Add(next);
                }
                else
                {
                    queue.Enqueue(next);
                }
            }
        }

        if (frontierMines.Count > 0)
        {
            // Remove the mine closest to the goal
            frontierMines.Sort((a, b) =>
                Vector2Int.Distance(a, GoalPosition).CompareTo(Vector2Int.Distance(b, GoalPosition)));
            Grid[frontierMines[0].x, frontierMines[0].y].IsMine = false;
        }
    }

    void CalculateAdjacentMineCounts()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (Grid[x, y].IsMine) continue;

                int count = 0;
                foreach (Vector2Int dir in Directions)
                {
                    Vector2Int neighbor = new Vector2Int(x, y) + dir;
                    if (IsInBounds(neighbor) && Grid[neighbor.x, neighbor.y].IsMine)
                        count++;
                }
                Grid[x, y].AdjacentMineCount = count;
            }
        }
    }

    void SetupMinePrefabs()
    {
        if (MinePrefab == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!Grid[x, y].IsMine) continue;

                Tile tile = Grid[x, y];
                GameObject mine = Instantiate(MinePrefab, tile.transform);
                mine.transform.localPosition = new Vector3(0f, 0.01f, 0f);
                mine.SetActive(false);
                tile.MineSlot = mine;

                // Add explosion component
                if (mine.GetComponent<MineExplosion>() == null)
                {
                    mine.AddComponent<MineExplosion>();
                }
            }
        }
    }

    void SpawnCharacter()
    {
        if (CharacterPrefab == null) return;

        Vector3 startWorldPos = GridToWorldPosition(StartPosition.x, StartPosition.y);
        startWorldPos.y = 0f;
        characterInstance = Instantiate(CharacterPrefab, startWorldPos, Quaternion.identity);
        characterInstance.name = "Player";

        // Hook up PlayerController
        PlayerController playerController = GameManager.Instance.PlayerController;
        if (playerController != null)
        {
            CharacterMover mover = characterInstance.GetComponent<CharacterMover>();
            if (mover != null)
            {
                playerController.CharacterMover = mover;
            }

            CharacterAnimator animator = characterInstance.GetComponent<CharacterAnimator>();
            if (animator != null)
            {
                playerController.CharacterAnimator = animator;
            }
        }

        // Hook up CameraController
        CameraController cameraController = GameManager.Instance.CameraController;
        if (cameraController != null)
        {
            cameraController.Target = characterInstance.transform;

            CharacterMover mover = characterInstance.GetComponent<CharacterMover>();
            if (mover != null)
            {
                mover.OnStartedMoving += cameraController.OnCharacterStartedMoving;
                mover.OnArrived += cameraController.OnCharacterArrived;
            }
        }
    }

    void BakeNavMesh()
    {
        // Create a flat plane for NavMesh
        GameObject navPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        navPlane.name = "NavMeshPlane";
        navPlane.transform.parent = gridParent.transform;

        // Scale to cover the entire grid with some margin
        float scaleX = (width + 2f) / 10f;
        float scaleZ = (height + 2f) / 10f;
        navPlane.transform.localScale = new Vector3(scaleX, 1f, scaleZ);

        // Center the plane
        float centerX = (width - 1f) / 2f;
        float centerZ = (height - 1f) / 2f;
        navPlane.transform.position = new Vector3(centerX, -0.01f, centerZ);

        // Make it invisible but keep collider for NavMesh
        MeshRenderer renderer = navPlane.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.enabled = false;

        // Build NavMesh surface at runtime
        NavMeshSurface surface = navPlane.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
    }

    void CreateGridOverlay()
    {
        GameObject overlayParent = new GameObject("GridOverlay");
        overlayParent.transform.parent = gridParent.transform;

        Material lineMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        lineMat.color = new Color(0f, 0f, 0f, 0.4f);

        float lineY = 0.01f;
        float lineWidth = 0.02f;

        // Vertical lines (along Z axis)
        for (int x = 0; x <= width; x++)
        {
            GameObject lineObj = new GameObject($"VLine_{x}");
            lineObj.transform.parent = overlayParent.transform;
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = lineMat;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(x - 0.5f, lineY, -0.5f));
            lr.SetPosition(1, new Vector3(x - 0.5f, lineY, height - 0.5f));
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
        }

        // Horizontal lines (along X axis)
        for (int y = 0; y <= height; y++)
        {
            GameObject lineObj = new GameObject($"HLine_{y}");
            lineObj.transform.parent = overlayParent.transform;
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = lineMat;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(-0.5f, lineY, y - 0.5f));
            lr.SetPosition(1, new Vector3(width - 0.5f, lineY, y - 0.5f));
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
        }
    }

    public void FloodFillReveal(Vector2Int position)
    {
        // Iterative BFS flood-fill to avoid stack overflow on large grids
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        // Enqueue unrevealed neighbours of the origin tile
        foreach (Vector2Int dir in Directions)
        {
            Vector2Int neighbor = position + dir;
            if (IsInBounds(neighbor))
            {
                Tile tile = Grid[neighbor.x, neighbor.y];
                if (!tile.IsRevealed && !tile.IsMine && !tile.IsFlagged)
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            Tile tile = Grid[current.x, current.y];

            if (tile.IsRevealed || tile.IsMine || tile.IsFlagged) continue;

            // Directly set state to avoid recursive FloodFillReveal calls from Tile.Reveal()
            tile.IsRevealed = true;
            // Trigger visual update — we call Reveal() but the IsRevealed guard
            // inside Tile.Reveal() will skip it, so we set visuals manually here
            tile.SendMessage("OnFloodRevealed", SendMessageOptions.DontRequireReceiver);

            // If this tile also has 0 adjacent mines, continue expanding
            if (tile.AdjacentMineCount == 0)
            {
                foreach (Vector2Int dir in Directions)
                {
                    Vector2Int next = current + dir;
                    if (IsInBounds(next) && !Grid[next.x, next.y].IsRevealed)
                    {
                        queue.Enqueue(next);
                    }
                }
            }
        }
    }

    public bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    public bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) <= 1 && Mathf.Abs(a.y - b.y) <= 1 && a != b;
    }

    /// <summary>
    /// BFS pathfinding that only traverses revealed, non-flagged, non-mine tiles.
    /// Returns the path as a list of grid positions (excluding the start, including the destination).
    /// Returns null if no valid path exists.
    /// </summary>
    public List<Vector2Int> FindRevealedPath(Vector2Int from, Vector2Int to)
    {
        if (!IsInBounds(from) || !IsInBounds(to)) return null;

        bool[,] visited = new bool[width, height];
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(from);
        visited[from.x, from.y] = true;

        bool found = false;
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == to) { found = true; break; }

            foreach (Vector2Int dir in Directions)
            {
                Vector2Int next = current + dir;
                if (!IsInBounds(next) || visited[next.x, next.y]) continue;

                Tile tile = Grid[next.x, next.y];
                // Only traverse revealed, non-flagged, safe tiles
                if (!tile.IsRevealed || tile.IsFlagged || tile.IsMine) continue;

                visited[next.x, next.y] = true;
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }

        if (!found) return null;

        // Reconstruct path (excluding start, including destination)
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int step = to;
        while (step != from)
        {
            path.Add(step);
            step = cameFrom[step];
        }
        path.Reverse();
        return path;
    }

    public Vector3 GridToWorldPosition(int x, int y)
    {
        return new Vector3(x, 0f, y);
    }

    public Tile GetTileAtWorldPosition(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x);
        int y = Mathf.RoundToInt(worldPos.z);
        Vector2Int gridPos = new Vector2Int(x, y);
        if (IsInBounds(gridPos))
            return Grid[x, y];
        return null;
    }

    public void ClearGrid()
    {
        if (gridParent != null)
        {
            Destroy(gridParent);
        }
        if (characterInstance != null)
        {
            Destroy(characterInstance);
        }
        Grid = null;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (Grid == null) return;

        // Draw guaranteed path debug visualization
        bool[,] visited = new bool[width, height];
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(StartPosition);
        visited[StartPosition.x, StartPosition.y] = true;

        bool found = false;
        while (queue.Count > 0 && !found)
        {
            Vector2Int current = queue.Dequeue();
            if (current == GoalPosition) { found = true; break; }

            foreach (Vector2Int dir in Directions)
            {
                Vector2Int next = current + dir;
                if (IsInBounds(next) && !visited[next.x, next.y] && !Grid[next.x, next.y].IsMine)
                {
                    visited[next.x, next.y] = true;
                    cameFrom[next] = current;
                    queue.Enqueue(next);
                }
            }
        }

        if (found)
        {
            Gizmos.color = Color.green;
            Vector2Int step = GoalPosition;
            while (cameFrom.ContainsKey(step))
            {
                Vector2Int prev = cameFrom[step];
                Gizmos.DrawLine(
                    GridToWorldPosition(step.x, step.y) + Vector3.up * 0.5f,
                    GridToWorldPosition(prev.x, prev.y) + Vector3.up * 0.5f
                );
                step = prev;
            }
        }
    }
#endif
}
