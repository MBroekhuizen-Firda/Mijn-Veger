using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public CharacterMover CharacterMover;
    public CharacterAnimator CharacterAnimator;
    public Camera MainCamera;
    public LayerMask TileLayerMask;

    private bool inputEnabled;
    private Tile currentTile;
    private bool waitingForArrival;

    void Start()
    {
        if (MainCamera == null)
            MainCamera = Camera.main;
    }

    void OnEnable()
    {
        if (CharacterMover != null)
            CharacterMover.OnArrived += OnCharacterArrived;
    }

    void OnDisable()
    {
        if (CharacterMover != null)
            CharacterMover.OnArrived -= OnCharacterArrived;
    }

    void Update()
    {
        if (!inputEnabled || waitingForArrival) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Re-subscribe if mover was assigned at runtime
        if (CharacterMover != null && !waitingForArrival)
        {
            CharacterMover.OnArrived -= OnCharacterArrived;
            CharacterMover.OnArrived += OnCharacterArrived;
        }

        // Left click - move to tile
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleLeftClick();
        }

        // Right click - flag tile
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleRightClick();
        }
    }

    void HandleLeftClick()
    {
        Tile tile = RaycastToTile();
        if (tile == null) return;
        if (tile.IsRevealed || tile.IsFlagged) return;

        // Only allow movement to adjacent tiles
        GridGenerator generator = GameManager.Instance.GridGenerator;
        Tile playerTile = generator.GetTileAtWorldPosition(CharacterMover.transform.position);
        if (playerTile == null) return;
        if (!generator.IsAdjacent(playerTile.GridPosition, tile.GridPosition)) return;

        // Move character to the tile
        currentTile = tile;
        waitingForArrival = true;

        Vector3 targetPos = generator.GridToWorldPosition(
            tile.GridPosition.x, tile.GridPosition.y);
        CharacterMover.MoveTo(targetPos);
    }

    void HandleRightClick()
    {
        Tile tile = RaycastToTile();
        if (tile == null) return;
        if (tile.IsRevealed) return;

        // Only allow flagging adjacent tiles
        GridGenerator generator = GameManager.Instance.GridGenerator;
        Tile playerTile = generator.GetTileAtWorldPosition(CharacterMover.transform.position);
        if (playerTile == null) return;

        if (generator.IsAdjacent(playerTile.GridPosition, tile.GridPosition))
        {
            tile.Flag();
        }
    }

    Tile RaycastToTile()
    {
        if (MainCamera == null) return null;

        Ray ray = MainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, TileLayerMask))
        {
            return hit.collider.GetComponentInParent<Tile>();
        }

        return null;
    }

    void OnCharacterArrived()
    {
        waitingForArrival = false;

        if (currentTile != null)
        {
            currentTile.Reveal();
            currentTile = null;
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (!enabled)
        {
            waitingForArrival = false;
            currentTile = null;
        }
    }
}
