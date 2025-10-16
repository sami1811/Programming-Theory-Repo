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
    
    [Header("Data Settings")]
    [SerializeField] private int maxDataCount;
    
    private string _playerName;
    private float _playerTime;
    private string _customDir = $"E:/Unity/Unity Projects/Unity Version Control/Programming-Theory-Repo/Saved Records/Leaderboard.json";
    
    public string PlayerName
    {
        get => _playerName;
        set => _playerName = value;
    }

    public float PlayerTime
    {
        get => _playerTime;
        set => _playerTime = value;
    }
    
    public string CustomDir => _customDir;
    
    private void Awake()
    {
        if(Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (string.IsNullOrEmpty(_customDir))
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

        if (existingPlayer >= 0)
        {
            if (_playerTime > saveData.playerTime[existingPlayer])
            {
                saveData.playerTime[existingPlayer] = _playerTime;
                
                var mins = Mathf.FloorToInt(_playerTime / 60f);
                var secs = Mathf.FloorToInt(_playerTime % 60f);
#if UNITY_EDITOR
                Debug.LogWarning($"[Data Manager] {_playerName} got new record of {mins:00}:{secs:00}");
#endif
            }
        }
        else
        {
            if (saveData.playerTime.Count < maxDataCount)
            {
                saveData.playerNames.Add(_playerName);
                saveData.playerTime.Add(_playerTime);
                
                var mins = Mathf.FloorToInt(_playerTime / 60f);
                var secs = Mathf.FloorToInt(_playerTime % 60f);
                
#if UNITY_EDITOR
                Debug.Log($"[Data Manager] {_playerName} is saved with the score of {mins:00}:{secs:00}");
#endif
            }
            else
            {
                var minScore = saveData.playerTime[0];
                var minIndex = 0;

                for(var i = 1; i < saveData.playerTime.Count; i++)
                {
                    if (saveData.playerTime[i] < minScore)
                    {
                        minScore = saveData.playerTime[i];
                        minIndex = i;
                    }
                }

                if(_playerTime <= minScore)
                {
#if UNITY_EDITOR
                    Debug.Log("[Data Manager] Score is too low to be updated");
#endif
                    return;
                }
                else
                {
                    var removedPlayerName = saveData.playerNames[minIndex];

                    saveData.playerTime.RemoveAt(minIndex);
                    saveData.playerNames.RemoveAt(minIndex);

                    saveData.playerNames.Add(_playerName);
                    saveData.playerTime.Add(_playerTime);
#if UNITY_EDITOR
                    Debug.Log($"[Data Manager] {_playerName} took {removedPlayerName}'s place leaderboard is updated.");
#endif
                }
            }
        }
        
        var json = JsonUtility.ToJson(saveData);
        File.WriteAllText(_customDir, json);
#if UNITY_EDITOR
        Debug.Log($"[Data Manager] Leaderboard is updated.");
#endif
    }
}
