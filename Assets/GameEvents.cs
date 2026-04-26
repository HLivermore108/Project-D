public static class GameEvents
{
    public delegate void ScoreChangedHandler(int score, int highScore);
    public delegate void PlayerHealthChangedHandler(int currentHealth, int maxHealth);
    public delegate void GameOverHandler(int finalScore, int highScore);

    public static event ScoreChangedHandler ScoreChanged;
    public static event PlayerHealthChangedHandler PlayerHealthChanged;
    public static event GameOverHandler GameOver;

    public static void RaiseScoreChanged(int score, int highScore)
    {
        ScoreChanged?.Invoke(score, highScore);
    }

    public static void RaisePlayerHealthChanged(int currentHealth, int maxHealth)
    {
        PlayerHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public static void RaiseGameOver(int finalScore, int highScore)
    {
        GameOver?.Invoke(finalScore, highScore);
    }
}
