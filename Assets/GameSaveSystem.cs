using System;
using System.IO;
using UnityEngine;

public static class GameSaveSystem
{
    private const string SaveFileName = "save_data.json";

    public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static GameSaveData Load()
    {
        if (!File.Exists(SavePath))
            return new GameSaveData();

        try
        {
            string json = File.ReadAllText(SavePath);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            return data ?? new GameSaveData();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Could not load save data from {SavePath}: {ex.Message}");
            return new GameSaveData();
        }
    }

    public static void Save(GameSaveData data)
    {
        if (data == null)
            return;

        try
        {
            data.lastSavedUtc = DateTime.UtcNow.ToString("O");
            Directory.CreateDirectory(Application.persistentDataPath);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Could not save data to {SavePath}: {ex.Message}");
        }
    }
}
