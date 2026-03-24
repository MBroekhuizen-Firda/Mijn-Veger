using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject SetupPanel;
    public GameObject HUDPanel;
    public GameObject GameOverPanel;
    public GameObject WinPanel;

    [Header("Setup Screen")]
    public Slider WidthSlider;
    public Slider HeightSlider;
    public TextMeshProUGUI WidthLabel;
    public TextMeshProUGUI HeightLabel;
    public Button StartButton;

    [Header("HUD")]
    public TextMeshProUGUI MineCountText;
    public TextMeshProUGUI FlagCountText;

    [Header("Game Over")]
    public Button RetryButton;

    [Header("Win")]
    public Button PlayAgainButton;

    void Start()
    {
        // Setup slider listeners
        if (WidthSlider != null)
        {
            WidthSlider.minValue = 10;
            WidthSlider.maxValue = 100;
            WidthSlider.wholeNumbers = true;
            WidthSlider.value = 10;
            WidthSlider.onValueChanged.AddListener(OnWidthChanged);
            OnWidthChanged(WidthSlider.value);
        }

        if (HeightSlider != null)
        {
            HeightSlider.minValue = 10;
            HeightSlider.maxValue = 100;
            HeightSlider.wholeNumbers = true;
            HeightSlider.value = 10;
            HeightSlider.onValueChanged.AddListener(OnHeightChanged);
            OnHeightChanged(HeightSlider.value);
        }

        // Setup button listeners
        if (StartButton != null)
            StartButton.onClick.AddListener(OnStartClicked);

        if (RetryButton != null)
            RetryButton.onClick.AddListener(OnRetryClicked);

        if (PlayAgainButton != null)
            PlayAgainButton.onClick.AddListener(OnRetryClicked);
    }

    void OnWidthChanged(float value)
    {
        if (WidthLabel != null)
            WidthLabel.text = $"Width: {(int)value}";
        if (GameManager.Instance != null)
            GameManager.Instance.GridWidth = (int)value;
    }

    void OnHeightChanged(float value)
    {
        if (HeightLabel != null)
            HeightLabel.text = $"Height: {(int)value}";
        if (GameManager.Instance != null)
            GameManager.Instance.GridHeight = (int)value;
    }

    void OnStartClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();
    }

    void OnRetryClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RestartGame();
    }

    public void ShowSetupScreen()
    {
        SetAllPanelsInactive();
        if (SetupPanel != null) SetupPanel.SetActive(true);
    }

    public void ShowHUD()
    {
        SetAllPanelsInactive();
        if (HUDPanel != null) HUDPanel.SetActive(true);
        UpdateHUD();
    }

    public void ShowGameOverScreen()
    {
        if (HUDPanel != null) HUDPanel.SetActive(false);
        if (GameOverPanel != null) GameOverPanel.SetActive(true);
    }

    public void ShowWinScreen()
    {
        if (HUDPanel != null) HUDPanel.SetActive(false);
        if (WinPanel != null) WinPanel.SetActive(true);
    }

    public void UpdateHUD()
    {
        if (GameManager.Instance == null) return;

        if (MineCountText != null)
            MineCountText.text = $"Mines: {GameManager.Instance.TotalMines}";

        if (FlagCountText != null)
            FlagCountText.text = $"Flags: {GameManager.Instance.FlagsPlaced}";
    }

    void SetAllPanelsInactive()
    {
        if (SetupPanel != null) SetupPanel.SetActive(false);
        if (HUDPanel != null) HUDPanel.SetActive(false);
        if (GameOverPanel != null) GameOverPanel.SetActive(false);
        if (WinPanel != null) WinPanel.SetActive(false);
    }
}
