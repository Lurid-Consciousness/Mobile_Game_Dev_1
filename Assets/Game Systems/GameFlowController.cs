using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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
    private GameObject tutorialPanel;
    private GameObject touchControls;
    private Text failReason;
    private Image tutorialImage;
    private Text tutorialTitle;
    private Text tutorialBody;
    private Text tutorialPageText;
    private Text[] tutorialCallouts;
    private Button tutorialBackButton;
    private Button tutorialNextButton;
    private Sprite[] tutorialSprites;
    private int tutorialPage;
    private Font regularFont;
    private Font boldFont;

    private bool gameRunning;
    private bool startAfterReload;
    private bool sessionStarted;
    private InputAction pauseAction;

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
        regularFont = Resources.Load<Font>("UI/Fonts/AtkinsonHyperlegible-Regular");
        boldFont = Resources.Load<Font>("UI/Fonts/AtkinsonHyperlegible-Bold");
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
        if (sessionStarted && pauseAction != null && pauseAction.WasPressedThisFrame() && player != null && player.IsAlive && tree != null && tree.IsAlive)
        {
            if (gameRunning)
                ShowMainMenu();
            else
                StartGame();
        }
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
        sessionStarted = false;
        pauseAction = player.moveAction.action.actionMap.FindAction("Pause");
        pauseAction?.Enable();

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
        scaler.referenceResolution = new Vector2(2340f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        CreateMainPanel();
        CreateSettingsPanel();
        CreateCreditsPanel();
        CreateGameOverPanel();
        CreateTutorialPanel();
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

        CreateText(menu.transform, "TREE DEFENCE", new Vector2(0f, 245f), new Vector2(590f, 120f), 68, accentColor);
        CreateText(menu.transform, "Protect the tree. Survive the waves.", new Vector2(0f, 155f), new Vector2(590f, 65f), 30, Color.white);

        CreateButton(menu.transform, "PLAY / RESUME", new Vector2(0f, 55f), PlayFromMenu);
        CreateButton(menu.transform, "SETTINGS", new Vector2(0f, -45f), ShowSettings);
        CreateButton(menu.transform, "CREDITS", new Vector2(0f, -145f), ShowCredits);
        CreateButton(menu.transform, "QUIT", new Vector2(0f, -245f), QuitGame);
    }

    void CreateSettingsPanel()
    {
        settingsPanel = CreateFullPanel("Settings", backgroundColor);
        GameObject menu = CreateCenteredPanel(settingsPanel.transform, "Settings Menu", new Vector2(800f, 960f), panelColor);

        CreateText(menu.transform, "SETTINGS", new Vector2(0f, 410f), new Vector2(560f, 70f), 45, accentColor);
        CreateText(menu.transform, "MASTER VOLUME", new Vector2(0f, 340f), new Vector2(500f, 50f), 30, Color.white);

        Slider slider = CreateSlider(menu.transform, new Vector2(0f, 300f));
        slider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = slider.value;
        slider.onValueChanged.AddListener(SetVolume);

        AddSettingSlider(menu.transform, "Look sensitivity", "LookSensitivity", 0.2f, 3f, 1f, 225f);
        AddSettingSlider(menu.transform, "Field of view", "FieldOfView", 50f, 100f, 60f, 125f);
        AddSettingButton(menu.transform, "Invert Y", "InvertY", -100f, 0);
        AddSettingButton(menu.transform, "VSync", "VSync", -190f, 1);
        AddSettingButton(menu.transform, "Fullscreen", "Fullscreen", -280f, 1);
        CreateText(menu.transform, "VSync off: 120 FPS cap. Esc pauses / resumes.", new Vector2(0f, -350f), new Vector2(760f, 50f), 26, Color.white);
        CreateButton(menu.transform, "BACK", new Vector2(0f, -410f), ShowMainMenu);
        ApplySettings();
        settingsPanel.SetActive(false);
    }

    void CreateCreditsPanel()
    {
        creditsPanel = CreateFullPanel("Credits", backgroundColor);
        GameObject menu = CreateCenteredPanel(creditsPanel.transform, "Credits Menu", new Vector2(760f, 680f), panelColor);

        CreateText(menu.transform, "CREDITS", new Vector2(0f, 245f), new Vector2(660f, 90f), 50, accentColor);
        CreateText(menu.transform, "Game Design: Wael Al-Malki\nProgramming: Wael Al-Malki, with Codex assistance\n\nAudio / Input Glyphs: Kenney (CC0)\nImpact Sounds, UI Audio, Input Prompts\n\nNature / Raccoon + Animations: Quaternius (CC0)\nUltimate Nature, Cube World Kit\nGrass / Ground Textures: ambientCG (CC0)\nGrass004, Ground037\n\nPrototype squirrel, bear, balloon possum and\nprocedural animation: assembled with Codex\nBoomerang model: supplied by project author\nUI font: Atkinson Hyperlegible (OFL)", new Vector2(0f, 15f), new Vector2(720f, 470f), 26, Color.white);
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

    void CreateTutorialPanel()
    {
        tutorialPanel = CreateFullPanel("Tutorial", backgroundColor);
        GameObject panel = CreateCenteredPanel(tutorialPanel.transform, "Tutorial Panel", new Vector2(2180f, 940f), panelColor);

        GameObject imageObject = new GameObject("Gameplay Screenshot", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(panel.transform, false);
        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = new Vector2(-420f, 45f);
        imageRect.sizeDelta = new Vector2(1260f, 710f);
        tutorialImage = imageObject.GetComponent<Image>();
        tutorialImage.color = Color.white;
        tutorialImage.preserveAspect = true;

        tutorialTitle = CreateText(panel.transform, "", new Vector2(650f, 355f), new Vector2(720f, 100f), 52, accentColor);
        tutorialBody = CreateText(panel.transform, "", new Vector2(650f, 250f), new Vector2(720f, 120f), 30, Color.white);
        tutorialBody.alignment = TextAnchor.UpperCenter;

        tutorialCallouts = new Text[3];
        tutorialCallouts[0] = CreateTutorialCallout(panel.transform, new Vector2(650f, 100f));
        tutorialCallouts[1] = CreateTutorialCallout(panel.transform, new Vector2(650f, -35f));
        tutorialCallouts[2] = CreateTutorialCallout(panel.transform, new Vector2(650f, -170f));

        tutorialPageText = CreateText(panel.transform, "", new Vector2(0f, -405f), new Vector2(250f, 55f), 28, Color.white);
        tutorialBackButton = CreateButton(panel.transform, "BACK", new Vector2(490f, -360f), PreviousTutorialPage);
        tutorialNextButton = CreateButton(panel.transform, "NEXT", new Vector2(875f, -360f), NextTutorialPage);

        tutorialSprites = new Sprite[3];

        for (int i = 0; i < tutorialSprites.Length; i++)
        {
            Texture2D texture = Resources.Load<Texture2D>($"UI/Tutorial/tutorial_{i + 1}");

            if (texture != null)
                tutorialSprites[i] = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        tutorialPanel.SetActive(false);
    }

    void CreateTouchControls()
    {
        touchControls = new GameObject("Touch Controls", typeof(RectTransform));
        touchControls.transform.SetParent(canvasObject.transform, false);
        Stretch(touchControls.GetComponent<RectTransform>());

        VirtualButtonControl crouchControl = CreateVirtualButton("Crouch Control", "<Gamepad>/buttonEast");
        VirtualButtonControl sprintControl = CreateVirtualButton("Sprint Control", "<Gamepad>/leftStickPress");

        MobileTouchStick movementStick = CreateTouchZone(touchControls.transform, "Movement Zone", new Vector2(0f, 0f), new Vector2(0.5f, 1f), "<Gamepad>/leftStick", true, crouchControl, null, sprintControl);
        CreateAutoRunButton(touchControls.transform, movementStick);
        CreateTouchButton(touchControls.transform, "ATTACK", new Vector2(-170f, 190f), "<Gamepad>/buttonWest");
        CreateTouchButton(touchControls.transform, "AIM", new Vector2(-360f, 190f), "<Gamepad>/rightShoulder");
        CreateTouchButton(touchControls.transform, "JUMP", new Vector2(-170f, 380f), "<Gamepad>/buttonSouth");
        CreateTouchButton(touchControls.transform, "INTERACT", new Vector2(-550f, 280f), "<Gamepad>/buttonNorth");
        CreateTouchButton(touchControls.transform, "PAUSE", new Vector2(-100f, -100f), "<Gamepad>/start", true);

        touchControls.SetActive(false);
    }

    void StartGame()
    {
        PlayButtonSound();
        gameRunning = true;
        sessionStarted = true;
        Time.timeScale = 1f;

        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        tutorialPanel.SetActive(false);
        touchControls.SetActive(Application.isMobilePlatform);

        SetGameplayControl(true);
        Cursor.lockState = Application.isMobilePlatform ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = Application.isMobilePlatform;
    }

    void PlayFromMenu()
    {
        if (sessionStarted)
            StartGame();
        else
            ShowTutorial();
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
        tutorialPanel.SetActive(false);
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
        tutorialPanel.SetActive(false);
    }

    void ShowCredits()
    {
        PlayButtonSound();
        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(true);
        tutorialPanel.SetActive(false);
    }

    void ShowTutorial()
    {
        PlayButtonSound();
        gameRunning = false;
        Time.timeScale = 0f;
        tutorialPage = 0;

        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        touchControls.SetActive(false);
        tutorialPanel.SetActive(true);

        SetGameplayControl(false);
        UpdateTutorialPage();
    }

    void PreviousTutorialPage()
    {
        PlayButtonSound();

        if (tutorialPage == 0)
        {
            ShowMainMenu(false);
            return;
        }

        tutorialPage--;
        UpdateTutorialPage();
    }

    void NextTutorialPage()
    {
        PlayButtonSound();

        if (tutorialPage >= 2)
        {
            StartGame();
            return;
        }

        tutorialPage++;
        UpdateTutorialPage();
    }

    void UpdateTutorialPage()
    {
        string[] titles =
        {
            "MOVE AND SURVIVE",
            "THROW THE BOOMERANG",
            "DEFEND THE TREE"
        };

        string[] descriptions =
        {
            "The entire left half of the screen is your movement area.",
            "Use the right-side buttons to throw normally or enter precision aim.",
            "Watch your health, the tree, and the enemies remaining in each wave."
        };

        string[,] callouts =
        {
            { "MOVE STICK\nDrag anywhere on the left side to move and turn.", "AUTO RUN\nTap the visible top-left button to run forward. Tap again to stop.", "JUMP / CROUCH\nTap JUMP. Hold the stick near its centre for two seconds to crouch." },
            { "ATTACK\nThrows a fast, narrow oval in front of the player.", "AIM\nStops movement and enables precise aiming. Tilt the phone or use the stick.", "CHARGED SHOT\nHold ATTACK while aiming. Release to hit harder and ricochet back." },
            { "INTERACT\nPick up, drop, repair, or use the object under the crosshair.", "PAUSE\nOpens the main menu without ending the current run.", "SURVIVE A WAVE\nClearing every enemy completes the wave. The next wave begins automatically." }
        };

        tutorialTitle.text = titles[tutorialPage];
        tutorialBody.text = descriptions[tutorialPage];
        tutorialPageText.text = $"{tutorialPage + 1} / 3";

        for (int i = 0; i < tutorialCallouts.Length; i++)
            tutorialCallouts[i].text = callouts[tutorialPage, i];

        tutorialImage.sprite = tutorialSprites[tutorialPage];
        tutorialImage.color = tutorialSprites[tutorialPage] == null ? new Color(0.08f, 0.14f, 0.08f, 1f) : Color.white;

        tutorialBackButton.GetComponentInChildren<Text>().text = tutorialPage == 0 ? "MENU" : "BACK";
        tutorialNextButton.GetComponentInChildren<Text>().text = tutorialPage == 2 ? "START GAME" : "NEXT";
    }

    void ShowGameOver()
    {
        gameRunning = false;
        Time.timeScale = 0f;
        SetGameplayControl(false);
        touchControls.SetActive(false);
        tutorialPanel.SetActive(false);
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

    private void AddSettingSlider(Transform parent, string label, string key, float minimum, float maximum, float fallback, float y)
    {
        Text text = CreateText(parent, "", new Vector2(0f, y), new Vector2(650f, 50f), 28, Color.white);
        Slider slider = CreateSlider(parent, new Vector2(0f, y - 40f));
        slider.minValue = minimum;
        slider.maxValue = maximum;
        slider.value = PlayerPrefs.GetFloat(key, fallback);
        text.text = $"{label}: {slider.value:0.0}";
        slider.onValueChanged.AddListener(value =>
        {
            text.text = $"{label}: {value:0.0}";
            PlayerPrefs.SetFloat(key, value);
            ApplySettings();
        });
    }

    private void AddSettingButton(Transform parent, string label, string key, float y, int fallback)
    {
        Button button = CreateButton(parent, $"{label}: {(PlayerPrefs.GetInt(key, fallback) == 1 ? "ON" : "OFF")}", new Vector2(0f, y), () => { });
        button.onClick.AddListener(() =>
        {
            int value = 1 - PlayerPrefs.GetInt(key, fallback);
            PlayerPrefs.SetInt(key, value);
            button.GetComponentInChildren<Text>().text = $"{label}: {(value == 1 ? "ON" : "OFF")}";
            ApplySettings();
        });
    }

    private void ApplySettings()
    {
        if (playerCamera != null)
            playerCamera.GetComponent<Camera>().fieldOfView = PlayerPrefs.GetFloat("FieldOfView", 60f);
        QualitySettings.vSyncCount = PlayerPrefs.GetInt("VSync", 1);
        Application.targetFrameRate = Application.isMobilePlatform ? 60 : 120;
        if (!Application.isEditor && !Application.isMobilePlatform)
            Screen.fullScreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
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
        text.font = fontSize >= 36
            ? boldFont ?? regularFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            : regularFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    Text CreateTutorialCallout(Transform parent, Vector2 position)
    {
        GameObject callout = new GameObject("Tutorial Callout", typeof(RectTransform), typeof(Image));
        callout.transform.SetParent(parent, false);
        RectTransform rect = callout.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(720f, 115f);
        callout.GetComponent<Image>().color = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.9f);
        return CreateText(callout.transform, "", Vector2.zero, new Vector2(690f, 105f), 27, Color.white);
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

        CreateText(buttonObject.transform, label, Vector2.zero, new Vector2(420f, 75f), 32, Color.white);
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

    MobileTouchStick CreateTouchZone(Transform parent, string zoneName, Vector2 anchorMin, Vector2 anchorMax, string controlPath, bool movementZone, VirtualButtonControl holdAction, VirtualButtonControl tapAction, VirtualButtonControl sprintAction)
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
        return stick;
    }

    void CreateAutoRunButton(Transform parent, MobileTouchStick movementStick)
    {
        GameObject buttonObject = new GameObject("Auto Run", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(170f, -105f);
        rect.sizeDelta = new Vector2(280f, 120f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.88f);
        Button button = buttonObject.GetComponent<Button>();
        Text label = CreateText(buttonObject.transform, "AUTO RUN", Vector2.zero, new Vector2(265f, 110f), 30, Color.black);
        button.onClick.AddListener(movementStick.ToggleAutoRun);
        movementStick.AutoRunChanged += running =>
        {
            label.text = running ? "STOP RUN" : "AUTO RUN";
            image.color = running
                ? new Color(0.3f, 0.9f, 0.35f, 0.95f)
                : new Color(accentColor.r, accentColor.g, accentColor.b, 0.88f);
        };
    }

    void CreateTouchButton(Transform parent, string label, Vector2 position, string controlPath, bool topAnchored = false)
    {
        GameObject button = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(OnScreenButton));
        button.transform.SetParent(parent, false);

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = topAnchored ? new Vector2(1f, 1f) : new Vector2(1f, 0f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(140f, 140f);

        button.GetComponent<Image>().color = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.68f);
        button.GetComponent<OnScreenButton>().controlPath = controlPath;
        CreateText(button.transform, label, Vector2.zero, new Vector2(132f, 132f), 25, Color.white);
    }

    void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
