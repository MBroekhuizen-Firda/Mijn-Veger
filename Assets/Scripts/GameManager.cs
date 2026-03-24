using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Setup, Playing, GameOver, Win }
    public GameState CurrentState { get; private set; } = GameState.Setup;

    [Header("Grid Settings")]
    public int GridWidth = 10;
    public int GridHeight = 10;
    [Range(0.10f, 0.25f)]
    public float MineDensity = 0.15f;

    [Header("References")]
    public GridGenerator GridGenerator;
    public UIManager UIManager;
    public PlayerController PlayerController;
    public CameraController CameraController;

    public int TotalMines { get; private set; }
    public int FlagsPlaced { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        SetState(GameState.Setup);
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.Setup:
                if (UIManager != null) UIManager.ShowSetupScreen();
                break;
            case GameState.Playing:
                if (UIManager != null) UIManager.ShowHUD();
                break;
            case GameState.GameOver:
                if (PlayerController != null) PlayerController.SetInputEnabled(false);
                if (UIManager != null) UIManager.ShowGameOverScreen();
                break;
            case GameState.Win:
                if (PlayerController != null) PlayerController.SetInputEnabled(false);
                if (UIManager != null) UIManager.ShowWinScreen();
                break;
        }
    }

    public void StartGame()
    {
        FlagsPlaced = 0;
        TotalMines = Mathf.RoundToInt(GridWidth * GridHeight * MineDensity);

        GridGenerator.GenerateGrid(GridWidth, GridHeight, TotalMines);

        if (PlayerController != null) PlayerController.SetInputEnabled(true);
        if (CameraController != null) CameraController.SetTopDownView(GridWidth, GridHeight);

        SetState(GameState.Playing);
    }

    public void OnPlayerDied()
    {
        SetState(GameState.GameOver);
    }

    public void OnPlayerReachedGoal()
    {
        SetState(GameState.Win);
    }

    public void OnFlagChanged(int delta)
    {
        FlagsPlaced += delta;
        if (UIManager != null) UIManager.UpdateHUD();
    }

    public void RestartGame()
    {
        GridGenerator.ClearGrid();
        SetState(GameState.Setup);
    }
}
