using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ScoringHealth : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text scoreText;
    public TMP_Text highScoreText; // <-- new UI reference for high score
    public Slider healthBar;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TMP_Text finalScoreText;
    public TMP_Text finalHighScoreText; // <-- optional: show high score on game over screen
    public Button retryButton;
    public Button mainMenuButton;

    private int score = 0;
    private int health = 100;
    private bool isGameOver = false;
    private int highScore = 0;

    private void Start()
    {
        // Load saved high score
        highScore = PlayerPrefs.GetInt("HighScore", 0);

        UpdateUI();
        gameOverPanel.SetActive(false);

        // Hook up button events
        retryButton.onClick.AddListener(RestartGame);
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    public void AddScore(int amount)
    {
        if (isGameOver) return;
        score += amount;

        // Update high score in real-time
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", highScore);
        }

        UpdateUI();
    }

    public void TakeDamage(int amount)
    {
        if (isGameOver) return;
        health -= amount;
        if (health <= 0)
        {
            health = 0;
            GameOver();
        }
        UpdateUI();
    }

    private void UpdateUI()
    {
        scoreText.text = "Score: " + score;
        healthBar.value = health;
        if (highScoreText != null)
            highScoreText.text = "High Score: " + highScore;
    }

    private void GameOver()
    {
        isGameOver = true;
        gameOverPanel.SetActive(true);
        finalScoreText.text = "Final Score: " + score;

        // Save new high score if beaten
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", highScore);
        }

        if (finalHighScoreText != null)
            finalHighScoreText.text = "High Score: " + highScore;

        Time.timeScale = 0f; // pause the game
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); // make sure a scene named "MainMenu" exists
    }
}
