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

    [Header("Visual References")]
    public GameObject HiddenVisual;
    public GameObject RevealedVisual;
    public GameObject FlagVisual;
    public GameObject GoalVisual;
    public GameObject MineSlot;
    public TextMeshPro NumberText;

    [Header("Materials")]
    public Material HiddenMaterial;
    public Material RevealedMaterial;

    private MeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        SetVisualState(TileVisualState.Hidden);
    }

    public enum TileVisualState { Hidden, Revealed, Flagged, Mine }

    void SetVisualState(TileVisualState state)
    {
        if (HiddenVisual != null) HiddenVisual.SetActive(state == TileVisualState.Hidden);
        if (RevealedVisual != null) RevealedVisual.SetActive(state == TileVisualState.Revealed || state == TileVisualState.Mine);
        if (FlagVisual != null) FlagVisual.SetActive(state == TileVisualState.Flagged);
        if (GoalVisual != null) GoalVisual.SetActive(IsGoal && state != TileVisualState.Mine);
        if (NumberText != null) NumberText.gameObject.SetActive(false);
        if (MineSlot != null) MineSlot.SetActive(state == TileVisualState.Mine);
    }

    public void Reveal()
    {
        if (IsRevealed || IsFlagged) return;
        IsRevealed = true;

        if (IsMine)
        {
            SetVisualState(TileVisualState.Mine);
            TriggerMine();
            return;
        }

        SetVisualState(TileVisualState.Revealed);

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

        // Check win condition if this is the goal tile
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
            case 2: return new Color(0f, 0.5f, 0f); // dark green
            case 3: return Color.red;
            case 4: return new Color(0.5f, 0f, 0.5f); // purple
            case 5: return new Color(0.5f, 0f, 0f); // maroon
            case 6: return Color.cyan;
            case 7: return Color.black;
            case 8: return Color.gray;
            default: return Color.white;
        }
    }

    public void Flag()
    {
        if (IsRevealed) return;

        if (IsFlagged)
        {
            IsFlagged = false;
            SetVisualState(TileVisualState.Hidden);
            GameManager.Instance.OnFlagChanged(-1);
        }
        else
        {
            IsFlagged = true;
            SetVisualState(TileVisualState.Flagged);
            GameManager.Instance.OnFlagChanged(1);
        }
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
            // Fallback: notify game manager directly
            GameManager.Instance.OnPlayerDied();
        }
    }

    // Called by GridGenerator.FloodFillReveal via SendMessage to update visuals
    // without triggering another recursive flood-fill
    void OnFloodRevealed()
    {
        SetVisualState(TileVisualState.Revealed);
        if (AdjacentMineCount > 0)
        {
            ShowNumber();
        }

        if (IsGoal)
        {
            GameManager.Instance.OnPlayerReachedGoal();
        }
    }

    public void ResetTile()
    {
        IsMine = false;
        IsRevealed = false;
        IsFlagged = false;
        IsGoal = false;
        AdjacentMineCount = 0;
        SetVisualState(TileVisualState.Hidden);
        if (NumberText != null) NumberText.gameObject.SetActive(false);
    }
}
