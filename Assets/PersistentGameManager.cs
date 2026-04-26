using UnityEngine;

public class PersistentGameManager : MonoBehaviour
{
    public static PersistentGameManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    public GameSaveData SaveData { get; private set; }

    private SQLiteGameDatabase database;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        var managerObject = new GameObject("PersistentGameManager");
        managerObject.AddComponent<PersistentGameManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadPersistentData();
    }

    private void OnEnable()
    {
        GameEvents.ScoreChanged += HandleScoreChanged;
        GameEvents.PlayerHealthChanged += HandlePlayerHealthChanged;
        GameEvents.GameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEvents.ScoreChanged -= HandleScoreChanged;
        GameEvents.PlayerHealthChanged -= HandlePlayerHealthChanged;
        GameEvents.GameOver -= HandleGameOver;
    }

    private void OnApplicationQuit()
    {
        SaveCurrentData();
        database?.Dispose();
        database = null;
    }

    private void LoadPersistentData()
    {
        SaveData = GameSaveSystem.Load();

        database = new SQLiteGameDatabase();
        database.Initialize();

        GameSaveData latestDatabaseSave = database.ReadLatestSave();
        if (latestDatabaseSave != null)
        {
            SaveData.highScore = Mathf.Max(SaveData.highScore, latestDatabaseSave.highScore);
            SaveData.lastScore = latestDatabaseSave.lastScore;
            SaveData.lastHealth = latestDatabaseSave.lastHealth;
            SaveData.lastSavedUtc = latestDatabaseSave.lastSavedUtc;
        }

        SaveCurrentData();
    }

    private void HandleScoreChanged(int score, int highScore)
    {
        SaveData.lastScore = score;
        SaveData.highScore = Mathf.Max(SaveData.highScore, highScore);
        SaveCurrentData();
    }

    private void HandlePlayerHealthChanged(int currentHealth, int maxHealth)
    {
        SaveData.lastHealth = currentHealth;
        SaveCurrentData();
    }

    private void HandleGameOver(int finalScore, int highScore)
    {
        SaveData.lastScore = finalScore;
        SaveData.highScore = Mathf.Max(SaveData.highScore, highScore);
        SaveData.lastHealth = 0;
        SaveCurrentData();
    }

    private void SaveCurrentData()
    {
        if (SaveData == null)
            SaveData = new GameSaveData();

        GameSaveSystem.Save(SaveData);
        database?.WriteSave(SaveData);
    }
}
