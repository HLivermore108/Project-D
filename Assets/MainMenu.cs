using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuPanel;   // Main buttons panel
    public GameObject optionsPanel;
    public GameObject creditsPanel;

    private bool showPlayPanel;
    private bool showMultiplayerPanel;
    private string multiplayerAddress = "127.0.0.1";
    private string multiplayerPort = "7777";

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

        GUILayout.BeginArea(new Rect(Screen.width * 0.5f - 170f, Screen.height * 0.5f - 150f, 340f, 330f), GUI.skin.box);
        GUILayout.Label("Choose Mode");

        if (!showMultiplayerPanel)
        {
            if (GUILayout.Button("Single Player", GUILayout.Height(44f)))
            {
                if (TwoPlayerNetcodeBootstrap.Instance != null)
                {
                    TwoPlayerNetcodeBootstrap.Instance.StartSinglePlayerGame();
                }
                else
                {
                    SceneManager.LoadScene("SampleScene");
                }
            }

            if (GUILayout.Button("Multiplayer", GUILayout.Height(44f)))
            {
                showMultiplayerPanel = true;
            }

            if (GUILayout.Button("Back", GUILayout.Height(36f)))
            {
                BackToMain();
            }
        }
        else
        {
            GUILayout.Label(TwoPlayerNetcodeBootstrap.Instance != null ? TwoPlayerNetcodeBootstrap.Instance.Status : "Initializing multiplayer...");
            GUILayout.Label("Address");
            multiplayerAddress = GUILayout.TextField(multiplayerAddress);
            GUILayout.Label("Port");
            multiplayerPort = GUILayout.TextField(multiplayerPort);

            if (!ushort.TryParse(multiplayerPort, out ushort port))
            {
                port = 7777;
            }

            GUI.enabled = TwoPlayerNetcodeBootstrap.Instance != null && !TwoPlayerNetcodeBootstrap.Instance.IsConnected;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Host", GUILayout.Height(40f)))
            {
                TwoPlayerNetcodeBootstrap.Instance.StartHostSession(multiplayerAddress, port);
            }

            if (GUILayout.Button("Connect", GUILayout.Height(40f)))
            {
                TwoPlayerNetcodeBootstrap.Instance.StartClientSession(multiplayerAddress, port);
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            GUI.enabled = TwoPlayerNetcodeBootstrap.Instance != null && TwoPlayerNetcodeBootstrap.Instance.CanHostStartGame;
            if (GUILayout.Button("Start Game", GUILayout.Height(44f)))
            {
                TwoPlayerNetcodeBootstrap.Instance.HostStartGame();
            }
            GUI.enabled = true;

            if (TwoPlayerNetcodeBootstrap.Instance != null && TwoPlayerNetcodeBootstrap.Instance.IsConnected && GUILayout.Button("Disconnect", GUILayout.Height(36f)))
            {
                TwoPlayerNetcodeBootstrap.Instance.Shutdown();
            }

            if (GUILayout.Button("Back", GUILayout.Height(36f)))
            {
                showMultiplayerPanel = false;
            }
        }

        GUILayout.EndArea();
    }
}
