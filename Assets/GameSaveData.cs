using System;

[Serializable]
public class GameSaveData
{
    public int highScore;
    public int lastScore;
    public int lastHealth = 100;
    public string lastSavedUtc;
}
