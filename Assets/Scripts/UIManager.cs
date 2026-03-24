using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    [Header("UI Document")]
    public UIDocument UIDocument;

    VisualElement root;

    // Panels
    VisualElement setupPanel;
    VisualElement hudPanel;
    VisualElement gameOverPanel;
    VisualElement winPanel;

    // Setup elements
    SliderInt widthSlider;
    SliderInt heightSlider;
    Label widthLabel;
    Label heightLabel;
    Button startButton;

    // HUD elements
    Label mineCountText;
    Label flagCountText;

    // Overlay buttons
    Button retryButton;
    Button playAgainButton;

    void OnEnable()
    {
        if (UIDocument == null)
            UIDocument = GetComponent<UIDocument>();

        root = UIDocument.rootVisualElement;

        // Query panels
        setupPanel = root.Q<VisualElement>("setup-panel");
        hudPanel = root.Q<VisualElement>("hud-panel");
        gameOverPanel = root.Q<VisualElement>("gameover-panel");
        winPanel = root.Q<VisualElement>("win-panel");

        // Query setup elements
        widthSlider = root.Q<SliderInt>("width-slider");
        heightSlider = root.Q<SliderInt>("height-slider");
        widthLabel = root.Q<Label>("width-label");
        heightLabel = root.Q<Label>("height-label");
        startButton = root.Q<Button>("start-button");

        // Query HUD elements
        mineCountText = root.Q<Label>("mine-count");
        flagCountText = root.Q<Label>("flag-count");

        // Query overlay buttons
        retryButton = root.Q<Button>("retry-button");
        playAgainButton = root.Q<Button>("playagain-button");

        // Register callbacks
        widthSlider.RegisterValueChangedCallback(OnWidthChanged);
        heightSlider.RegisterValueChangedCallback(OnHeightChanged);
        startButton.clicked += OnStartClicked;
        retryButton.clicked += OnRetryClicked;
        playAgainButton.clicked += OnRetryClicked;

        // Initialize labels
        OnWidthChanged(null);
        OnHeightChanged(null);
    }

    void OnDisable()
    {
        if (widthSlider != null)
            widthSlider.UnregisterValueChangedCallback(OnWidthChanged);
        if (heightSlider != null)
            heightSlider.UnregisterValueChangedCallback(OnHeightChanged);
        if (startButton != null)
            startButton.clicked -= OnStartClicked;
        if (retryButton != null)
            retryButton.clicked -= OnRetryClicked;
        if (playAgainButton != null)
            playAgainButton.clicked -= OnRetryClicked;
    }

    void OnWidthChanged(ChangeEvent<int> evt)
    {
        int value = widthSlider.value;
        widthLabel.text = $"Width: {value}";
        if (GameManager.Instance != null)
            GameManager.Instance.GridWidth = value;
    }

    void OnHeightChanged(ChangeEvent<int> evt)
    {
        int value = heightSlider.value;
        heightLabel.text = $"Height: {value}";
        if (GameManager.Instance != null)
            GameManager.Instance.GridHeight = value;
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
        HideAllPanels();
        Show(setupPanel);
    }

    public void ShowHUD()
    {
        HideAllPanels();
        Show(hudPanel);
        UpdateHUD();
    }

    public void ShowGameOverScreen()
    {
        Hide(hudPanel);
        Show(gameOverPanel);
    }

    public void ShowWinScreen()
    {
        Hide(hudPanel);
        Show(winPanel);
    }

    public void UpdateHUD()
    {
        if (GameManager.Instance == null) return;

        mineCountText.text = $"Mines: {GameManager.Instance.TotalMines}";
        flagCountText.text = $"Flags: {GameManager.Instance.FlagsPlaced}";
    }

    void HideAllPanels()
    {
        Hide(setupPanel);
        Hide(hudPanel);
        Hide(gameOverPanel);
        Hide(winPanel);
    }

    void Show(VisualElement element)
    {
        if (element != null)
            element.style.display = DisplayStyle.Flex;
    }

    void Hide(VisualElement element)
    {
        if (element != null)
            element.style.display = DisplayStyle.None;
    }
}
