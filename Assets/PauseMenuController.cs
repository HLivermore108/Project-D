using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    private const string SampleSceneName = "SampleScene";
    private const string StageTwoSceneName = "SampleScene2";
    private const string MusicVolumeParameter = "MusicVolume";
    private const string SfxVolumeParameter = "SFXVolume";
    private const string FullscreenPrefsKey = "PauseMenu.Fullscreen";
    private const string QualityPrefsKey = "PauseMenu.Quality";

    public static bool IsPaused { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;

    private GameObject pauseButtonObject;
    private GameObject menuPanel;
    private GameObject settingsPanel;
    private Button resumeButton;
    private TMP_Text qualityValueText;
    private Slider musicSlider;
    private Slider sfxSlider;
    private Toggle fullscreenToggle;
    private ScoringHealth scoringHealth;
    private float timeScaleBeforePause = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForLoadedSampleScene()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureControllerInScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IsPaused = false;
        Time.timeScale = 1f;
        EnsureControllerInScene(scene);
    }

    private static void EnsureControllerInScene(Scene scene)
    {
        if (!scene.isLoaded || !IsPlayableScene(scene.name))
            return;

#if UNITY_2023_2_OR_NEWER
        if (FindFirstObjectByType<PauseMenuController>() != null)
#else
        if (FindObjectOfType<PauseMenuController>() != null)
#endif
            return;

        var pauseControllerObject = new GameObject("PauseMenuController");
        pauseControllerObject.AddComponent<PauseMenuController>();
    }

    private static bool IsPlayableScene(string sceneName)
    {
        return sceneName == SampleSceneName || sceneName == StageTwoSceneName;
    }

    private void Awake()
    {
        IsPaused = false;
        timeScaleBeforePause = Mathf.Approximately(Time.timeScale, 0f) ? 1f : Time.timeScale;
        scoringHealth = FindScoringHealth();
        BuildUi();
        ApplySavedSettings();
        SetMenuVisible(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !IsGameOver())
        {
            TogglePause();
        }
    }

    private void OnDestroy()
    {
        if (IsPaused)
        {
            Time.timeScale = 1f;
            IsPaused = false;
        }
    }

    public void TogglePause()
    {
        if (IsPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (IsPaused || IsGameOver())
            return;

        IsPaused = true;
        timeScaleBeforePause = Mathf.Approximately(Time.timeScale, 0f) ? 1f : Time.timeScale;
        Time.timeScale = 0f;
        SetMenuVisible(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (EventSystem.current != null && resumeButton != null)
        {
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }
    }

    public void ResumeGame()
    {
        if (!IsPaused)
            return;

        IsPaused = false;
        Time.timeScale = Mathf.Approximately(timeScaleBeforePause, 0f) ? 1f : timeScaleBeforePause;
        SetMenuVisible(false);
    }

    public void ReturnToMainMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        Application.Quit();
        Debug.Log("Game Quit!");
    }

    private void SetMusicVolume(float value)
    {
        SetMixerVolume(MusicVolumeParameter, value);
    }

    private void SetSfxVolume(float value)
    {
        SetMixerVolume(SfxVolumeParameter, value);
    }

    private void SetMixerVolume(string parameterName, float sliderValue)
    {
        float dB = Mathf.Lerp(-80f, 0f, sliderValue);
        if (audioMixer != null)
        {
            audioMixer.SetFloat(parameterName, dB);
        }

        PlayerPrefs.SetFloat(parameterName, dB);
        PlayerPrefs.Save();
    }

    private void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenPrefsKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void CycleQuality()
    {
        if (QualitySettings.names.Length == 0)
            return;

        SetQuality((QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length);
    }

    private void SetQuality(int qualityIndex)
    {
        if (qualityIndex < 0 || qualityIndex >= QualitySettings.names.Length)
            return;

        QualitySettings.SetQualityLevel(qualityIndex, true);
        PlayerPrefs.SetInt(QualityPrefsKey, qualityIndex);
        PlayerPrefs.Save();

        if (qualityValueText != null)
        {
            qualityValueText.text = QualitySettings.names[qualityIndex];
        }
    }

    private void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }

    private void ApplySavedSettings()
    {
        ResolveAudioMixer();

        float savedMusicVolume = PlayerPrefs.GetFloat(MusicVolumeParameter, 0f);
        float savedSfxVolume = PlayerPrefs.GetFloat(SfxVolumeParameter, 0f);

        if (audioMixer != null)
        {
            audioMixer.SetFloat(MusicVolumeParameter, savedMusicVolume);
            audioMixer.SetFloat(SfxVolumeParameter, savedSfxVolume);
        }

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(Mathf.InverseLerp(-80f, 0f, savedMusicVolume));
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(Mathf.InverseLerp(-80f, 0f, savedSfxVolume));
        }

        bool fullscreen = PlayerPrefs.GetInt(FullscreenPrefsKey, Screen.fullScreen ? 1 : 0) == 1;
        Screen.fullScreen = fullscreen;
        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
        }

        if (QualitySettings.names.Length > 0)
        {
            int qualityIndex = Mathf.Clamp(PlayerPrefs.GetInt(QualityPrefsKey, QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(qualityIndex, true);
            if (qualityValueText != null)
            {
                qualityValueText.text = QualitySettings.names[qualityIndex];
            }
        }
    }

    private void ResolveAudioMixer()
    {
        if (audioMixer != null)
            return;

#if UNITY_2023_2_OR_NEWER
        AudioSource[] audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
#else
        AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
#endif
        foreach (AudioSource source in audioSources)
        {
            if (source.outputAudioMixerGroup != null)
            {
                audioMixer = source.outputAudioMixerGroup.audioMixer;
                return;
            }
        }
    }

    private void SetMenuVisible(bool visible)
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(visible);
        }

        if (pauseButtonObject != null)
        {
            pauseButtonObject.SetActive(!visible);
        }

        if (!visible && settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private bool IsGameOver()
    {
        if (scoringHealth == null)
        {
            scoringHealth = FindScoringHealth();
        }

        return scoringHealth != null && scoringHealth.IsGameOver();
    }

    private static ScoringHealth FindScoringHealth()
    {
#if UNITY_2023_2_OR_NEWER
        return FindFirstObjectByType<ScoringHealth>();
#else
        return FindObjectOfType<ScoringHealth>();
#endif
    }

    private void BuildUi()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("PauseMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        pauseButtonObject = CreatePauseButton(canvasObject.transform);
        menuPanel = CreatePauseOverlay(canvasObject.transform);
        settingsPanel = CreateSettingsPanel(menuPanel.transform);
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystemObject.transform.SetParent(transform, false);
    }

    private GameObject CreatePauseButton(Transform parent)
    {
        GameObject buttonObject = CreateUiObject("PauseButton", parent);
        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = new Vector2(-24f, -24f);
        rectTransform.sizeDelta = new Vector2(72f, 72f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.1f, 0.13f, 0.82f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(PauseGame);

        GameObject labelObject = CreateUiObject("Icon", buttonObject.transform);
        RectTransform labelTransform = Stretch(labelObject);
        labelTransform.offsetMin = Vector2.zero;
        labelTransform.offsetMax = Vector2.zero;

        TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "II";
        label.fontSize = 32f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;

        return buttonObject;
    }

    private GameObject CreatePauseOverlay(Transform parent)
    {
        GameObject overlay = CreateUiObject("PauseMenu", parent);
        Stretch(overlay);

        Image dim = overlay.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.62f);

        GameObject panel = CreateUiObject("Panel", overlay.transform);
        RectTransform panelTransform = panel.GetComponent<RectTransform>();
        panelTransform.anchorMin = new Vector2(0.5f, 0.5f);
        panelTransform.anchorMax = new Vector2(0.5f, 0.5f);
        panelTransform.pivot = new Vector2(0.5f, 0.5f);
        panelTransform.anchoredPosition = Vector2.zero;
        panelTransform.sizeDelta = new Vector2(560f, 840f);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.09f, 0.12f, 0.96f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(44, 44, 38, 38);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text title = CreateText("Paused", panel.transform, 52f, FontStyles.Bold);
        AddLayout(title.gameObject, 88f);

        resumeButton = CreateButton("Resume", panel.transform, ResumeGame);
        AddLayout(resumeButton.gameObject, 70f);

        Button settingsButton = CreateButton("Settings", panel.transform, ToggleSettingsPanel);
        AddLayout(settingsButton.gameObject, 70f);

        Button mainMenuButton = CreateButton("Quit To Menu", panel.transform, ReturnToMainMenu);
        AddLayout(mainMenuButton.gameObject, 70f);

        Button quitButton = CreateButton("Quit Game", panel.transform, QuitGame);
        AddLayout(quitButton.gameObject, 70f);

        return overlay;
    }

    private GameObject CreateSettingsPanel(Transform parent)
    {
        GameObject settings = CreateUiObject("Settings", parent.Find("Panel"));

        Image image = settings.AddComponent<Image>();
        image.color = new Color(0.13f, 0.15f, 0.19f, 0.95f);

        VerticalLayoutGroup layout = settings.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 20, 20);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        AddLayout(settings, 260f);

        TMP_Text settingsTitle = CreateText("Settings", settings.transform, 30f, FontStyles.Bold);
        AddLayout(settingsTitle.gameObject, 40f);

        musicSlider = CreateLabeledSlider("Music", settings.transform, SetMusicVolume);
        AddLayout(musicSlider.transform.parent.gameObject, 48f);

        sfxSlider = CreateLabeledSlider("SFX", settings.transform, SetSfxVolume);
        AddLayout(sfxSlider.transform.parent.gameObject, 48f);

        fullscreenToggle = CreateToggle("Fullscreen", settings.transform, SetFullscreen);
        AddLayout(fullscreenToggle.gameObject, 42f);

        Button qualityButton = CreateQualityButton(settings.transform);
        AddLayout(qualityButton.transform.parent.gameObject, 50f);

        settings.SetActive(false);
        return settings;
    }

    private Button CreateQualityButton(Transform parent)
    {
        GameObject row = CreateRow("QualityRow", parent);
        TMP_Text label = CreateText("Quality", row.transform, 24f, FontStyles.Normal);
        AddLayout(label.gameObject, 150f, -1f);

        GameObject buttonObject = CreateUiObject("QualityButton", row.transform);
        Image background = buttonObject.AddComponent<Image>();
        background.color = new Color(0.2f, 0.23f, 0.28f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(CycleQuality);

        qualityValueText = CreateText(QualitySettings.names.Length > 0 ? QualitySettings.names[QualitySettings.GetQualityLevel()] : "Default", buttonObject.transform, 22f, FontStyles.Normal);
        Stretch(qualityValueText.gameObject);
        qualityValueText.margin = new Vector4(14f, 0f, 14f, 0f);
        qualityValueText.alignment = TextAlignmentOptions.MidlineLeft;
        qualityValueText.raycastTarget = false;

        AddLayout(buttonObject, 230f, 44f);
        return button;
    }

    private Slider CreateLabeledSlider(string labelText, Transform parent, UnityEngine.Events.UnityAction<float> onChanged)
    {
        GameObject row = CreateRow(labelText + "Row", parent);

        TMP_Text label = CreateText(labelText, row.transform, 24f, FontStyles.Normal);
        AddLayout(label.gameObject, 150f, -1f);

        GameObject sliderObject = CreateUiObject(labelText + "Slider", row.transform);
        AddLayout(sliderObject, 230f, 36f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        GameObject backgroundObject = CreateUiObject("Background", sliderObject.transform);
        RectTransform backgroundTransform = Stretch(backgroundObject);
        backgroundTransform.offsetMin = new Vector2(0f, 12f);
        backgroundTransform.offsetMax = new Vector2(0f, -12f);
        Image background = backgroundObject.AddComponent<Image>();
        background.color = new Color(0.25f, 0.28f, 0.34f, 1f);

        GameObject fillArea = CreateUiObject("Fill Area", sliderObject.transform);
        RectTransform fillAreaTransform = Stretch(fillArea);
        fillAreaTransform.offsetMin = new Vector2(0f, 12f);
        fillAreaTransform.offsetMax = new Vector2(0f, -12f);

        GameObject fillObject = CreateUiObject("Fill", fillArea.transform);
        RectTransform fillTransform = Stretch(fillObject);
        Image fill = fillObject.AddComponent<Image>();
        fill.color = new Color(0.34f, 0.68f, 0.95f, 1f);

        GameObject handleArea = CreateUiObject("Handle Slide Area", sliderObject.transform);
        Stretch(handleArea);

        GameObject handleObject = CreateUiObject("Handle", handleArea.transform);
        RectTransform handleTransform = handleObject.GetComponent<RectTransform>();
        handleTransform.sizeDelta = new Vector2(26f, 26f);
        Image handle = handleObject.AddComponent<Image>();
        handle.color = Color.white;

        slider.fillRect = fillTransform;
        slider.handleRect = handleTransform;
        slider.targetGraphic = handle;
        slider.onValueChanged.AddListener(onChanged);

        return slider;
    }

    private Toggle CreateToggle(string labelText, Transform parent, UnityEngine.Events.UnityAction<bool> onChanged)
    {
        GameObject row = CreateRow(labelText + "Toggle", parent);
        Toggle toggle = row.AddComponent<Toggle>();

        GameObject boxObject = CreateUiObject("CheckmarkBox", row.transform);
        AddLayout(boxObject, 34f, 34f);
        Image box = boxObject.AddComponent<Image>();
        box.color = new Color(0.2f, 0.23f, 0.28f, 1f);

        GameObject checkObject = CreateUiObject("Checkmark", boxObject.transform);
        RectTransform checkTransform = Stretch(checkObject);
        checkTransform.offsetMin = new Vector2(7f, 7f);
        checkTransform.offsetMax = new Vector2(-7f, -7f);
        Image check = checkObject.AddComponent<Image>();
        check.color = new Color(0.34f, 0.68f, 0.95f, 1f);

        TMP_Text label = CreateText(labelText, row.transform, 24f, FontStyles.Normal);
        AddLayout(label.gameObject, 350f, -1f);

        toggle.targetGraphic = box;
        toggle.graphic = check;
        toggle.onValueChanged.AddListener(onChanged);
        return toggle;
    }

    private Button CreateButton(string labelText, Transform parent, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateUiObject(labelText.Replace(" ", "") + "Button", parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.17f, 0.2f, 0.25f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        TMP_Text label = CreateText(labelText, buttonObject.transform, 28f, FontStyles.Bold);
        Stretch(label.gameObject);
        label.raycastTarget = false;

        return button;
    }

    private TMP_Text CreateText(string text, Transform parent, float fontSize, FontStyles fontStyle)
    {
        GameObject textObject = CreateUiObject(text.Replace(" ", "") + "Text", parent);
        TMP_Text label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.enableAutoSizing = true;
        label.fontSizeMin = 14f;
        label.fontSizeMax = fontSize;
        return label;
    }

    private static GameObject CreateRow(string name, Transform parent)
    {
        GameObject row = CreateUiObject(name, parent);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return row;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject uiObject = new GameObject(name, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private static RectTransform Stretch(GameObject uiObject)
    {
        RectTransform rectTransform = uiObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        return rectTransform;
    }

    private static void AddLayout(GameObject uiObject, float preferredHeight)
    {
        AddLayout(uiObject, -1f, preferredHeight);
    }

    private static void AddLayout(GameObject uiObject, float preferredWidth, float preferredHeight)
    {
        LayoutElement layoutElement = uiObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = uiObject.AddComponent<LayoutElement>();
        }

        if (preferredWidth >= 0f)
        {
            layoutElement.preferredWidth = preferredWidth;
        }

        if (preferredHeight >= 0f)
        {
            layoutElement.preferredHeight = preferredHeight;
        }
    }
}
