using UnityEngine;
using TMPro;

public class Tile : MonoBehaviour
{
    [Header("State")]
    public bool IsMine;
    public bool IsRevealed;
    public bool IsFlagged;
    public bool IsGoal;
    public int AdjacentMineCount;
    public Vector2Int GridPosition;

    [Header("Prefabs")]
    public GameObject FlagPrefab;

    [Header("Visual References")]
    public GameObject FlagVisual;
    public GameObject GoalVisual;
    public GameObject MineSlot;
    public TextMeshPro NumberText;

    [Header("Materials")]
    public Material HiddenMaterial;
    public Material RevealedMaterial;
    public Material FlaggedMaterial;
    public Material GoalMaterial;

    private MeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        ApplyVisuals();
    }

    void ApplyVisuals()
    {
        // The tile quad is always visible — swap its material based on state
        if (meshRenderer != null)
        {
            if (IsFlagged && FlaggedMaterial != null)
                meshRenderer.material = FlaggedMaterial;
            else if (IsRevealed && RevealedMaterial != null)
                meshRenderer.material = RevealedMaterial;
            else if (IsGoal && GoalMaterial != null)
                meshRenderer.material = GoalMaterial;
            else if (HiddenMaterial != null)
                meshRenderer.material = HiddenMaterial;
        }

        // Flag indicator (optional child object, e.g. a small 3D flag)
        if (FlagVisual != null) FlagVisual.SetActive(IsFlagged);

        // Goal indicator
        if (GoalVisual != null) GoalVisual.SetActive(IsGoal && !IsRevealed);

        // Number text — only shown on revealed non-mine tiles with count > 0
        if (NumberText != null) NumberText.gameObject.SetActive(IsRevealed && !IsMine && AdjacentMineCount > 0);

        // Mine prefab — hidden until the player steps on it
        if (MineSlot != null) MineSlot.SetActive(IsRevealed && IsMine);
    }

    public void Reveal()
    {
        if (IsRevealed || IsFlagged) return;
        IsRevealed = true;

        if (IsMine)
        {
            ApplyVisuals();
            TriggerMine();
            return;
        }

        ApplyVisuals();

        if (AdjacentMineCount > 0)
        {
            ShowNumber();
        }
        else
        {
            // Flood-fill: reveal adjacent tiles with 0 adjacent mines
            GridGenerator generator = GameManager.Instance.GridGenerator;
            if (generator != null)
            {
                generator.FloodFillReveal(GridPosition);
            }
        }

        if (IsGoal)
        {
            GameManager.Instance.OnPlayerReachedGoal();
        }
    }

    void ShowNumber()
    {
        if (NumberText == null) return;
        NumberText.gameObject.SetActive(true);
        NumberText.text = AdjacentMineCount.ToString();
        NumberText.color = GetNumberColor(AdjacentMineCount);
    }

    Color GetNumberColor(int count)
    {
        switch (count)
        {
            case 1: return Color.blue;
            case 2: return new Color(0f, 0.5f, 0f);
            case 3: return Color.red;
            case 4: return new Color(0.5f, 0f, 0.5f);
            case 5: return new Color(0.5f, 0f, 0f);
            case 6: return Color.cyan;
            case 7: return Color.black;
            case 8: return Color.gray;
            default: return Color.white;
        }
    }

    public void Flag()
    {
        if (IsRevealed) return;

        IsFlagged = !IsFlagged;

        // Instantiate / destroy the flag visual dynamically
        if (IsFlagged && FlagVisual == null && FlagPrefab != null)
        {
            FlagVisual = Instantiate(FlagPrefab, transform);
            FlagVisual.transform.localPosition = new Vector3(0f, 0.01f, 0f);
        }
        else if (!IsFlagged && FlagVisual != null)
        {
            Destroy(FlagVisual);
            FlagVisual = null;
        }

        ApplyVisuals();
        GameManager.Instance.OnFlagChanged(IsFlagged ? 1 : -1);
    }

    void TriggerMine()
    {
        MineExplosion explosion = GetComponentInChildren<MineExplosion>();
        if (explosion != null)
        {
            explosion.Explode();
        }
        else
        {
            GameManager.Instance.OnPlayerDied();
        }
    }

    // Called by GridGenerator.FloodFillReveal via SendMessage to update visuals
    // without triggering another recursive flood-fill
    void OnFloodRevealed()
    {
        ApplyVisuals();
        if (AdjacentMineCount > 0)
        {
            ShowNumber();
        }

        if (IsGoal)
        {
            GameManager.Instance.OnPlayerReachedGoal();
        }
    }

    // Called by GridGenerator after all materials and state are fully initialized
    void RefreshVisuals()
    {
        ApplyVisuals();
    }

    public void ResetTile()
    {
        IsMine = false;
        IsRevealed = false;
        IsFlagged = false;
        IsGoal = false;
        AdjacentMineCount = 0;

        if (FlagVisual != null) { Destroy(FlagVisual); FlagVisual = null; }
        if (GoalVisual != null) { Destroy(GoalVisual); GoalVisual = null; }

        ApplyVisuals();
    }
}
