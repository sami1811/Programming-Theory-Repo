using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public List<string> playerNames =  new List<string>();
    public List<float> playerTime = new List<float>();
}

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    
    [Header("Game Data Settings")]
    [SerializeField] private int playerListLength;
    
    private string _playerName;
    private float _playerTime;
    private string _customDir = $"E:/Unity/Unity Projects/Unity Version Control/Programming-Theory-Repo/Saved Records/Leaderboard.json";
    
    public string PlayerName { get; set; }
    
    private void Awake()
    {
        if(Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (string.IsNullOrEmpty(_playerName))
        {
            _customDir = Path.Combine(Application.persistentDataPath, "Leaderboard.json");
        }
        
        Directory.CreateDirectory(Path.GetDirectoryName(_customDir) ?? string.Empty);
        
        QualitySettings.vSyncCount = 0; // disable VSync so targetFrameRate applies

#if UNITY_EDITOR
        Application.targetFrameRate = -1; // uncapped in editor
#elif UNITY_STANDALONE
        Application.targetFrameRate = 60; // cap for PC
#elif UNITY_ANDROID || UNITY_IOS
        Application.targetFrameRate = 60; // mobile default
#else
        Application.targetFrameRate = 60; // fallback
#endif
    }
    
    public void SaveData()
    {
        var saveData = new PlayerData();

        if (File.Exists(_customDir))
        {
            var existingData = File.ReadAllText(_customDir);
            var data = JsonUtility.FromJson<PlayerData>(existingData);

            if (data.playerNames != null && data.playerTime != null)
            {
                saveData.playerNames = data.playerNames;
                saveData.playerTime = data.playerTime;
            }
        }
        
        var existingPlayer = saveData.playerNames.IndexOf(_playerName);
        
        
    }
}
