using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class LeaderboardUIManager : MonoBehaviour
{
    public static LeaderboardUIManager Instance {  get; private set; }
    
    public List<TMP_Text> rankTexts = new List<TMP_Text>();
    public List<TMP_Text> nameTexts = new List<TMP_Text>();
    public List<TMP_Text> scoreTexts = new List<TMP_Text>();

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return; 
        }

        Instance = this;
        //DontDestroyOnLoad(gameObject);
        LoadLeaderboard();
    }

    public void LoadLeaderboard()
    {
        var fileDir = DataManager.Instance.CustomDir;

        if (!File.Exists(fileDir))
        {
#if UNITY_EDITOR
            Debug.Log("No records found.");
#endif
            
            for (var i = 0; i < rankTexts.Count; i++)
            {
                rankTexts[i].text = "";
                nameTexts[i].text = "";
                scoreTexts[i].text = "";
            }

            return;
        }

        var json = File.ReadAllText(fileDir);
        var sortingRanks = JsonUtility.FromJson<PlayerData>(json);

        if(sortingRanks.playerNames.Count == 0)
        {
#if UNITY_EDITOR
            Debug.Log("Leaderboard is empty.");
#endif
            return;
        }

        var playerScore = new List<(string name, float score)>();

        for (var i = 0; i < sortingRanks.playerNames.Count; i++)
        {
            playerScore.Add((sortingRanks.playerNames[i], sortingRanks.playerTime[i]));
        }

        playerScore.Sort((a , b) => b.score.CompareTo(a.score));

        for (var i = 0; i < playerScore.Count && i < rankTexts.Count && i < scoreTexts.Count && i < nameTexts.Count; i++)
        {
            rankTexts[i].text = $"{i + 1}.";
            nameTexts[i].text = playerScore[i].name;
            
            var mins = Mathf.FloorToInt(playerScore[i].score / 60f);
            var secs = Mathf.FloorToInt(playerScore[i].score % 60f);
            scoreTexts[i].text = $"{mins:00}:{secs:00}";
        }
#if UNITY_EDITOR
        Debug.Log("Leaderboard loaded successfully!");
#endif
    }
    
    public void ResetRecord()
    {
        var dirToFile = DataManager.Instance.CustomDir;

        File.Delete(dirToFile);
        LoadLeaderboard();
    }
    
    public void MenuClicked()
    {
        SceneController.LoadMainMenu();
    }
}
