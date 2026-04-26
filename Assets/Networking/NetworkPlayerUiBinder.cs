using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkPlayerUiBinder : MonoBehaviour
{
    private ScoringHealth scoringHealth;
    private NetworkPlayerController2D localPlayer;
    private bool showedFinalScores;
    private bool configuredUi;

    private void Update()
    {
        if (TwoPlayerNetcodeBootstrap.CurrentSessionMode != TwoPlayerNetcodeBootstrap.SessionMode.Multiplayer)
            return;

        if (scoringHealth == null)
            scoringHealth = FindFirstObjectByType<ScoringHealth>();

        if (scoringHealth == null)
            return;

        if (localPlayer == null)
            localPlayer = NetworkMultiplayerHud.FindLocalPlayer();

        if (localPlayer == null)
            return;

        ConfigureUiOnce();
        UpdateLiveUi();
        UpdateGameOverUi();
    }

    private void UpdateLiveUi()
    {
        TMP_Text scoreText = scoringHealth.scoreText;
        Slider healthBar = scoringHealth.healthBar;

        if (scoreText != null)
            scoreText.text = "Score: " + localPlayer.Score.Value;

        if (healthBar != null)
        {
            healthBar.minValue = 0;
            healthBar.maxValue = NetworkPlayerController2D.MaxHealth;
            healthBar.value = localPlayer.Health.Value;
        }
    }

    private void UpdateGameOverUi()
    {
        bool allPlayersEliminated = NetworkPlayerController2D.AreAllPlayersEliminated();
        if (!allPlayersEliminated)
        {
            showedFinalScores = false;
            if (scoringHealth.gameOverPanel != null)
                scoringHealth.gameOverPanel.SetActive(false);
            return;
        }

        if (showedFinalScores)
            return;

        showedFinalScores = true;
        if (scoringHealth.gameOverPanel != null)
            scoringHealth.gameOverPanel.SetActive(true);

        if (scoringHealth.finalScoreText != null)
            scoringHealth.finalScoreText.text = "Final Scores\n" + NetworkPlayerController2D.BuildFinalScoreText();

        if (scoringHealth.finalHighScoreText != null)
            scoringHealth.finalHighScoreText.text = string.Empty;
    }

    private void ConfigureUiOnce()
    {
        if (configuredUi)
            return;

        configuredUi = true;
        if (scoringHealth.highScoreText != null)
        {
            scoringHealth.highScoreText.text = string.Empty;
            scoringHealth.highScoreText.gameObject.SetActive(false);
        }

        if (scoringHealth.gameOverPanel != null)
            scoringHealth.gameOverPanel.SetActive(false);
    }
}
