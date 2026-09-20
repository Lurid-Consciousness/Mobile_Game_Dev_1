using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameFlowController : MonoBehaviour
{
    private static GameFlowController instance;

    private PlayerController player;
    private TreeObjective tree;
    private CameraController playerCamera;
    private PlayerHUD playerHUD;

    private GameObject canvasObject;
    private GameObject mainPanel;
    private GameObject settingsPanel;
    private GameObject creditsPanel;
    private GameObject gameOverPanel;
    private GameObject touchControls;
    private Text failReason;

    private bool gameRunning;
    private bool startAfterReload;

    private readonly Color backgroundColor = new Color(0.07f, 0.12f, 0.08f, 0.96f);
    private readonly Color panelColor = new Color(0.12f, 0.22f, 0.14f, 0.98f);
    private readonly Color buttonColor = new Color(0.25f, 0.52f, 0.25f, 1f);
    private readonly Color accentColor = new Color(0.96f, 0.72f, 0.2f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        if (instance != null)
            return;

        GameObject flowObject = new GameObject("Game Flow");
        instance = flowObject.AddComponent<GameFlowController>();
        DontDestroyOnLoad(flowObject);
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        SceneManager.sceneLoaded += SceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
    }

    void Update()
    {
        if (!gameRunning || player == null || tree == null)
            return;

        if (!player.IsAlive || !tree.IsAlive)
            ShowGameOver();
    }

    void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        player = FindAnyObjectByType<PlayerController>();
        tree = FindAnyObjectByType<TreeObjective>();
        playerCamera = FindAnyObjectByType<CameraController>();
        playerHUD = FindAnyObjectByType<PlayerHUD>();

        if (canvasObject != null)
            Destroy(canvasObject);

        CreateInterface();

        if (startAfterReload)
        {
            startAfterReload = false;
            StartGame();
        }
        else
        {
            ShowMainMenu(false);
        }
    }

    void CreateInterface()
    {
        CreateEventSystem();

        canvasObject = new GameObject("Game Interface", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasObject);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        CreateMainPanel();
        CreateSettingsPanel();
        CreateCreditsPanel();
        CreateGameOverPanel();
        CreateTouchControls();
    }

    void CreateEventSystem()
    {
        EventSystem currentEventSystem = FindAnyObjectByType<EventSystem>();

        if (currentEventSystem != null)
            return;

        GameObject eventObject = new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(eventObject);
        eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    void CreateMainPanel()
    {
        mainPanel = CreateFullPanel("Main Menu", backgroundColor);
        GameObject menu = CreateCenteredPanel(mainPanel.transform, "Menu", new Vector2(640f, 760f), panelColor);

        CreateText(menu.transform, "TREE DEFENCE", new Vector2(0f, 245f), new Vector2(560f, 120f), 62, accentColor);
        CreateText(menu.transform, "Protect the tree. Survive the waves.", new Vector2(0f, 155f), new Vector2(560f, 55f), 24, Color.white);

        CreateButton(menu.transform, "PLAY", new Vector2(0f, 55f), StartGame);
        CreateButton(menu.transform, "SETTINGS", new Vector2(0f, -45f), ShowSettings);
        CreateButton(menu.transform, "CREDITS", new Vector2(0f, -145f), ShowCredits);
        CreateButton(menu.transform, "QUIT", new Vector2(0f, -245f), QuitGame);
    }

    void CreateSettingsPanel()
    {
        settingsPanel = CreateFullPanel("Settings", backgroundColor);
        GameObject menu = CreateCenteredPanel(settingsPanel.transform, "Settings Menu", new Vector2(640f, 560f), panelColor);

        CreateText(menu.transform, "SETTINGS", new Vector2(0f, 185f), new Vector2(560f, 90f), 50, accentColor);
        CreateText(menu.transform, "MASTER VOLUME", new Vector2(0f, 70f), new Vector2(500f, 50f), 26, Color.white);

        Slider slider = CreateSlider(menu.transform, new Vector2(0f, 0f));
        slider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = slider.value;
        slider.onValueChanged.AddListener(SetVolume);

        CreateButton(menu.transform, "BACK", new Vector2(0f, -155f), ShowMainMenu);
        settingsPanel.SetActive(false);
    }

    void CreateCreditsPanel()
    {
        creditsPanel = CreateFullPanel("Credits", backgroundColor);
        GameObject menu = CreateCenteredPanel(creditsPanel.transform, "Credits Menu", new Vector2(760f, 680f), panelColor);

        CreateText(menu.transform, "CREDITS", new Vector2(0f, 245f), new Vector2(660f, 90f), 50, accentColor);
        CreateText(menu.transform, "Game Design and Programming\nWael Al-Malki\n\nSound Effects\nKenney Impact Sounds\nKenney UI Audio\n\nInput Glyphs\nKenney Input Prompts\n\nNature Assets\nQuaternius Ultimate Nature", new Vector2(0f, 25f), new Vector2(650f, 390f), 24, Color.white);
        CreateButton(menu.transform, "BACK", new Vector2(0f, -245f), ShowMainMenu);
        creditsPanel.SetActive(false);
    }

    void CreateGameOverPanel()
    {
        gameOverPanel = CreateFullPanel("Game Over", new Color(0.12f, 0.02f, 0.02f, 0.96f));
        GameObject menu = CreateCenteredPanel(gameOverPanel.transform, "Game Over Menu", new Vector2(680f, 580f), new Color(0.25f, 0.06f, 0.04f, 0.98f));

        CreateText(menu.transform, "GAME OVER", new Vector2(0f, 175f), new Vector2(600f, 100f), 60, new Color(1f, 0.5f, 0.2f));
        failReason = CreateText(menu.transform, "", new Vector2(0f, 75f), new Vector2(580f, 60f), 27, Color.white);
        CreateButton(menu.transform, "RESPAWN", new Vector2(0f, -40f), Respawn);
        CreateButton(menu.transform, "MAIN MENU", new Vector2(0f, -145f), ReturnToMainMenu);
        gameOverPanel.SetActive(false);
    }

    void CreateTouchControls()
    {
        touchControls = new GameObject("Touch Controls", typeof(RectTransform));
        touchControls.transform.SetParent(canvasObject.transform, false);
        Stretch(touchControls.GetComponent<RectTransform>());

        VirtualButtonControl jumpControl = CreateVirtualButton("Jump Control", "<Gamepad>/buttonSouth");
        VirtualButtonControl crouchControl = CreateVirtualButton("Crouch Control", "<Gamepad>/buttonEast");
        VirtualButtonControl attackControl = CreateVirtualButton("Attack Control", "<Gamepad>/buttonWest");
        VirtualButtonControl sprintControl = CreateVirtualButton("Sprint Control", "<Gamepad>/leftStickPress");

        CreateTouchZone(touchControls.transform, "Movement Zone", new Vector2(0f, 0f), new Vector2(0.5f, 1f), "<Gamepad>/leftStick", true, crouchControl, jumpControl, sprintControl);
        CreateTouchZone(touchControls.transform, "Camera Zone", new Vector2(0.5f, 0f), new Vector2(1f, 1f), "<Gamepad>/rightStick", false, attackControl, null, null);
        CreateTouchButton(touchControls.transform, "INTERACT", new Vector2(-560f, 430f), "<Gamepad>/buttonNorth");

        touchControls.SetActive(false);
    }

    void StartGame()
    {
        PlayButtonSound();
        gameRunning = true;
        Time.timeScale = 1f;

        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        touchControls.SetActive(Application.isMobilePlatform);

        SetGameplayControl(true);
        Cursor.lockState = Application.isMobilePlatform ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = Application.isMobilePlatform;
    }

    void ShowMainMenu()
    {
        ShowMainMenu(true);
    }

    void ShowMainMenu(bool playSound)
    {
        if (playSound)
            PlayButtonSound();
        gameRunning = false;
        Time.timeScale = 0f;

        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        touchControls.SetActive(false);

        SetGameplayControl(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ShowSettings()
    {
        PlayButtonSound();
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
        creditsPanel.SetActive(false);
    }

    void ShowCredits()
    {
        PlayButtonSound();
        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    void ShowGameOver()
    {
        gameRunning = false;
        Time.timeScale = 0f;
        SetGameplayControl(false);
        touchControls.SetActive(false);
        gameOverPanel.SetActive(true);

        if (failReason != null)
            failReason.text = player != null && !player.IsAlive ? "The squirrel was defeated." : "The defended tree was destroyed.";

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        GameAudio.PlayFail();
    }

    void Respawn()
    {
        PlayButtonSound();
        startAfterReload = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ReturnToMainMenu()
    {
        PlayButtonSound();
        startAfterReload = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void QuitGame()
    {
        PlayButtonSound();
        Application.Quit();
    }

    void SetGameplayControl(bool value)
    {
        if (player != null)
            player.enabled = value;

        if (playerCamera != null)
            playerCamera.enabled = value;

        if (playerHUD != null)
            playerHUD.enabled = value;
    }

    void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
    }

    void PlayButtonSound()
    {
        GameAudio.PlayButton();
    }

    GameObject CreateFullPanel(string panelName, Color color)
    {
        GameObject panel = new GameObject(panelName, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    GameObject CreateCenteredPanel(Transform parent, string panelName, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(panelName, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    Text CreateText(Transform parent, string content, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    Button CreateButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(430f, 78f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = buttonColor;

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = new Color(0.35f, 0.68f, 0.32f, 1f);
        colors.pressedColor = new Color(0.18f, 0.38f, 0.18f, 1f);
        button.colors = colors;
        button.onClick.AddListener(action);

        CreateText(buttonObject.transform, label, Vector2.zero, new Vector2(420f, 75f), 28, Color.white);
        return button;
    }

    Slider CreateSlider(Transform parent, Vector2 position)
    {
        GameObject sliderObject = new GameObject("Volume Slider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = position;
        sliderRect.sizeDelta = new Vector2(440f, 50f);

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(sliderObject.transform, false);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(0f, 16f);
        background.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.05f, 1f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRect.offsetMin = new Vector2(10f, -8f);
        fillAreaRect.offsetMax = new Vector2(-10f, 8f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        Stretch(fill.GetComponent<RectTransform>());
        fill.GetComponent<Image>().color = accentColor;

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        Stretch(handleAreaRect);
        handleAreaRect.offsetMin = new Vector2(15f, 0f);
        handleAreaRect.offsetMax = new Vector2(-15f, 0f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(36f, 36f);
        handle.GetComponent<Image>().color = Color.white;

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    VirtualButtonControl CreateVirtualButton(string controlName, string controlPath)
    {
        GameObject controlObject = new GameObject(controlName, typeof(RectTransform), typeof(VirtualButtonControl));
        controlObject.transform.SetParent(touchControls.transform, false);
        VirtualButtonControl control = controlObject.GetComponent<VirtualButtonControl>();
        control.controlPath = controlPath;
        return control;
    }

    void CreateTouchZone(Transform parent, string zoneName, Vector2 anchorMin, Vector2 anchorMax, string controlPath, bool movementZone, VirtualButtonControl holdAction, VirtualButtonControl tapAction, VirtualButtonControl sprintAction)
    {
        GameObject zone = new GameObject(zoneName, typeof(RectTransform), typeof(Image), typeof(MobileTouchStick));
        zone.transform.SetParent(parent, false);

        RectTransform rect = zone.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        MobileTouchStick stick = zone.GetComponent<MobileTouchStick>();
        stick.controlPath = controlPath;
        stick.Configure(movementZone, holdAction, tapAction, sprintAction);
    }

    void CreateTouchButton(Transform parent, string label, Vector2 position, string controlPath)
    {
        GameObject button = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(OnScreenButton));
        button.transform.SetParent(parent, false);

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(140f, 140f);

        button.GetComponent<Image>().color = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.68f);
        button.GetComponent<OnScreenButton>().controlPath = controlPath;
        CreateText(button.transform, label, Vector2.zero, new Vector2(132f, 132f), 19, Color.white);
    }

    void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
