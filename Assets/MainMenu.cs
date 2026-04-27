using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private const float MenuReferenceWidth = 1280f;
    private const float MenuReferenceHeight = 720f;

    [Header("Menu Panels")]
    public GameObject mainMenuPanel;   // Main buttons panel
    public GameObject optionsPanel;
    public GameObject creditsPanel;

    private bool showPlayPanel;
    private bool showMultiplayerPanel;
    private string multiplayerAddress = "127.0.0.1";
    private string multiplayerPort = "7777";
    private int selectedStageIndex;

    void Start()
    {
        // Ensure only main menu shows when starting
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void PlayGame()
    {
        showPlayPanel = true;
        showMultiplayerPanel = false;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    }

    public void OpenOptions()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void OpenCredits()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    public void BackToMain()
    {
        showPlayPanel = false;
        showMultiplayerPanel = false;
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Quit!");
    }

    private void OnGUI()
    {
        if (!showPlayPanel)
            return;

        Matrix4x4 originalMatrix = GUI.matrix;
        float menuScale = GetMenuScale();
        Vector2 virtualScreen = new Vector2(Screen.width / menuScale, Screen.height / menuScale);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(menuScale, menuScale, 1f));

        float panelWidth = Mathf.Min(720f, virtualScreen.x - 48f);
        float panelHeight = showMultiplayerPanel ? 620f : 430f;
        panelHeight = Mathf.Min(panelHeight, virtualScreen.y - 48f);
        Rect panelRect = new Rect(
            virtualScreen.x * 0.5f - panelWidth * 0.5f,
            virtualScreen.y * 0.5f - panelHeight * 0.5f,
            panelWidth,
            panelHeight);

        GUIStyle panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.padding = new RectOffset(26, 26, 22, 22);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontSize = 28;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 18;
        labelStyle.normal.textColor = Color.white;

        GUIStyle centeredLabelStyle = new GUIStyle(labelStyle);
        centeredLabelStyle.alignment = TextAnchor.MiddleCenter;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 20;
        buttonStyle.fontStyle = FontStyle.Bold;

        GUIStyle smallButtonStyle = new GUIStyle(buttonStyle);
        smallButtonStyle.fontSize = 18;

        GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField);
        textFieldStyle.fontSize = 20;

        GUILayout.BeginArea(panelRect, panelStyle);
        GUILayout.Label("Choose Mode", titleStyle, GUILayout.Height(40f));
        DrawStagePicker(labelStyle, centeredLabelStyle, buttonStyle);

        if (!showMultiplayerPanel)
        {
            GUILayout.Space(12f);
            if (GUILayout.Button("Single Player", buttonStyle, GUILayout.Height(58f)))
            {
                if (TwoPlayerNetcodeBootstrap.Instance != null)
                {
                    TwoPlayerNetcodeBootstrap.Instance.SelectStage(selectedStageIndex);
                    TwoPlayerNetcodeBootstrap.Instance.StartSinglePlayerGame();
                }
                else
                {
                    SceneManager.LoadScene(TwoPlayerNetcodeBootstrap.GetStageSceneName(selectedStageIndex));
                }
            }

            if (GUILayout.Button("Multiplayer", buttonStyle, GUILayout.Height(58f)))
            {
                if (TwoPlayerNetcodeBootstrap.Instance != null)
                    TwoPlayerNetcodeBootstrap.Instance.SelectStage(selectedStageIndex);

                showMultiplayerPanel = true;
            }

            GUILayout.Space(8f);
            if (GUILayout.Button("Back", smallButtonStyle, GUILayout.Height(46f)))
            {
                BackToMain();
            }
        }
        else
        {
            GUILayout.Space(8f);
            GUILayout.Label(TwoPlayerNetcodeBootstrap.Instance != null ? TwoPlayerNetcodeBootstrap.Instance.Status : "Initializing multiplayer...", centeredLabelStyle, GUILayout.Height(32f));
            if (TwoPlayerNetcodeBootstrap.Instance != null)
                TwoPlayerNetcodeBootstrap.Instance.SelectStage(selectedStageIndex);

            GUILayout.Label("Address", labelStyle);
            multiplayerAddress = GUILayout.TextField(multiplayerAddress, textFieldStyle, GUILayout.Height(42f));
            GUILayout.Label("Port", labelStyle);
            multiplayerPort = GUILayout.TextField(multiplayerPort, textFieldStyle, GUILayout.Height(42f));

            if (!ushort.TryParse(multiplayerPort, out ushort port))
            {
                port = 7777;
            }

            GUI.enabled = TwoPlayerNetcodeBootstrap.Instance != null && !TwoPlayerNetcodeBootstrap.Instance.IsConnected;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Host", buttonStyle, GUILayout.Height(52f)))
            {
                TwoPlayerNetcodeBootstrap.Instance.StartHostSession(multiplayerAddress, port);
            }

            if (GUILayout.Button("Connect", buttonStyle, GUILayout.Height(52f)))
            {
                TwoPlayerNetcodeBootstrap.Instance.StartClientSession(multiplayerAddress, port);
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            GUI.enabled = TwoPlayerNetcodeBootstrap.Instance != null && TwoPlayerNetcodeBootstrap.Instance.CanHostStartGame;
            if (GUILayout.Button("Start Game", buttonStyle, GUILayout.Height(56f)))
            {
                TwoPlayerNetcodeBootstrap.Instance.HostStartGame();
            }
            GUI.enabled = true;

            if (TwoPlayerNetcodeBootstrap.Instance != null && TwoPlayerNetcodeBootstrap.Instance.IsConnected && GUILayout.Button("Disconnect", smallButtonStyle, GUILayout.Height(44f)))
            {
                TwoPlayerNetcodeBootstrap.Instance.Shutdown();
            }

            if (GUILayout.Button("Back", smallButtonStyle, GUILayout.Height(44f)))
            {
                showMultiplayerPanel = false;
            }
        }

        GUILayout.EndArea();
        GUI.matrix = originalMatrix;
    }

    private float GetMenuScale()
    {
        float widthScale = Screen.width / MenuReferenceWidth;
        float heightScale = Screen.height / MenuReferenceHeight;
        return Mathf.Clamp(Mathf.Min(widthScale, heightScale), 0.9f, 1.6f);
    }

    private void DrawStagePicker(GUIStyle labelStyle, GUIStyle centeredLabelStyle, GUIStyle buttonStyle)
    {
        GUILayout.Label("Stage", labelStyle, GUILayout.Height(26f));
        GUILayout.BeginHorizontal();
        for (int i = 0; i < TwoPlayerNetcodeBootstrap.StageCount; i++)
        {
            bool wasSelected = selectedStageIndex == i;
            GUI.enabled = !wasSelected;
            if (GUILayout.Button(TwoPlayerNetcodeBootstrap.GetStageLabel(i), buttonStyle, GUILayout.Height(48f)))
            {
                selectedStageIndex = i;
                if (TwoPlayerNetcodeBootstrap.Instance != null)
                    TwoPlayerNetcodeBootstrap.Instance.SelectStage(selectedStageIndex);
            }
            GUI.enabled = true;
        }
        GUILayout.EndHorizontal();
        GUILayout.Label("Selected: " + TwoPlayerNetcodeBootstrap.GetStageLabel(selectedStageIndex), centeredLabelStyle, GUILayout.Height(30f));
    }
}
