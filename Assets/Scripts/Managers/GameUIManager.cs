using System.Collections.Generic;
using System.IO;
using TMPro;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

public class GameUIManager : MonoBehaviour
{
    [FormerlySerializedAs("playerStats")]
    [Header("Stats References")]
    [SerializeField] private TMP_Text moveSpeedText;
    [SerializeField] private TMP_Text fireRateText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text updatePoints;
    [SerializeField] private TMP_Text healingCountdownText;
    
    [Header("Rules Panel Settings")]
    [SerializeField] private TMP_Text playerIntro;
    [SerializeField] private GameObject rulesPanel;
    [SerializeField] private GameObject menuButton;
    
    [Header("Ability Panel Settings")]
    [SerializeField] private GameObject abilityPanel;
    
    [Header("Healing Countdown Settings")]
    [SerializeField] private UpgradeData upgradeData;
    
    [Header("Timer Settings")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject cube;
    
    [Header("Game Over Settings")]
    [SerializeField] private GameObject gameOverPanel;
    
    [Header("Resume Settings")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private List<TMP_Text> rankTexts = new List<TMP_Text>();
    [SerializeField] private List<TMP_Text> nameTexts = new List<TMP_Text>();
    [SerializeField] private List<TMP_Text> scoreTexts = new List<TMP_Text>();
    
    private float _countDownTimer;
    private float _elapsedStopwatchTimer;
    private bool _isCountingDown;
    private bool _isCountingUp;
    private bool _isGameOver;
    
    private HealthSystem _cubeHealth;
    
    private readonly StringBuilder _stringBuilder = new StringBuilder(32);
    
    public bool IsGameOver => _isGameOver;
    
    private void Awake()
    {
        if (!cube)
        {
            WarningLogger("Assign Cube in inspector");
        }
        
        if (!abilityPanel)
        {
            ErrorLogger("Ability panel is not assigned!");
        }
        
        if(!rulesPanel.activeInHierarchy || menuButton.activeInHierarchy)
        {
            menuButton.SetActive(false);
            rulesPanel.SetActive(true);
        }

        if (abilityPanel.activeInHierarchy)
        {
            abilityPanel.SetActive(false);
        }
        
        if (!updatePoints)
        {
            ErrorLogger("Percentage text not found!");
        }

        if (!upgradeData)
        {
            ErrorLogger("Upgrade Data for healing countdown not found!");
        }

        if (healingCountdownText)
        {
            if (healingCountdownText.gameObject.activeInHierarchy)
            {
                healingCountdownText.gameObject.SetActive(false);
            }
        }
        
        OnMoveSpeedChange();
        OnFireRateChange();
        OnDamageChange();
        
        _isGameOver =  false;
        gameOverPanel?.SetActive(false);
        menuPanel?.SetActive(false);
        leaderboardPanel?.SetActive(false);
        _cubeHealth =  cube?.GetComponent<HealthSystem>();
        Time.timeScale = 0f;
    }
    
    private void OnEnable()
    {
        // Subscribe to stat update events
        if (StatsSystem.Instance)
        {
            StatsSystem.Instance.onStatUpdated.AddListener(OnStatUpdated);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        if (StatsSystem.Instance)
        {
            StatsSystem.Instance.onStatUpdated.RemoveListener(OnStatUpdated);
        }
    }

    private void OnDestroy()
    {
        _stringBuilder.Clear();
    }

    private void Start()
    {
        playerIntro.text = "Hi " + DataManager.Instance.PlayerName;
    }

    private void Update()
    {
        if (_isGameOver)
        {
            gameOverPanel?.SetActive(true);
            Time.timeScale = 0f;
            return;
        }
        
        if (_isCountingUp)
        {
            GameTime();
        }
        
        if (!_isCountingDown)
            return;
        
        StartCountdown();
    }

    private void OnStatUpdated(UpgradeType upgradeType, float newMultiplier)
    {
        switch (upgradeType)
        {
            case UpgradeType.MovementSpeed:
                OnMoveSpeedChange();
                break;
            
            case UpgradeType.FireRate:
                OnFireRateChange();
                break;
            
            case UpgradeType.Damage:
                OnDamageChange();
                break;
            
            case UpgradeType.Points:
                OnPointsChange();
                break;
            
            case UpgradeType.HealthRegen:
                _isCountingDown =  true;
                healingCountdownText.gameObject.SetActive(true);
                _countDownTimer = upgradeData.regenDuration;
                break;
        }
    }

    private void StartCountdown()
    {
        if(!upgradeData)
            return;
        
        _countDownTimer -=  Time.deltaTime;

        if (_countDownTimer <= 0f)
        {
            _countDownTimer = 0f;
            _isCountingDown = false;
            healingCountdownText.gameObject.SetActive(false);
        }
        
        UpdateCountdownUI();
    }

    private void GameTime()
    {
        if (_cubeHealth.CurrentHealth <= 0)
        {
            _isGameOver = true;
            _isCountingUp = false;

            if (DataManager.Instance)
            {
                DataManager.Instance.PlayerTime = _elapsedStopwatchTimer;
                DataManager.Instance.SaveData();
            }
        }
        
        if(!_isCountingUp) return;
        
        _elapsedStopwatchTimer += Time.deltaTime;
        
        var mins = Mathf.FloorToInt(_elapsedStopwatchTimer / 60);
        var secs = Mathf.FloorToInt(_elapsedStopwatchTimer % 60);
        
        _stringBuilder.Clear();
        _stringBuilder.Append($"{mins:00}:{secs:00}");
        
        timerText.text = _stringBuilder.ToString();
    }
    
    private void UpdateCountdownUI()
    {
        if(!healingCountdownText)
            return;
        
        _stringBuilder.Clear();
        _stringBuilder.Append("Healing: ");
        _stringBuilder.Append(Mathf.RoundToInt(_countDownTimer));
        
        healingCountdownText.text = _stringBuilder.ToString();
    }
    
    private void OnMoveSpeedChange()
    {
        if(!StatsSystem.Instance || !moveSpeedText)
            return;
        
        _stringBuilder.Clear();
        _stringBuilder.Append("Speed:");
        _stringBuilder.Append(Mathf.RoundToInt(StatsSystem.Instance.MovementSpeed));
        _stringBuilder.Append("/10");
        
        moveSpeedText.text = _stringBuilder.ToString();
    }
    
    private void OnFireRateChange()
    {
        if(!StatsSystem.Instance || !fireRateText)
            return;
        
        _stringBuilder.Clear();
        _stringBuilder.Append("FireRate:");
        _stringBuilder.Append(StatsSystem.Instance.FireRate);
        _stringBuilder.Append("/12");
        
        fireRateText.text = _stringBuilder.ToString();
    }

    private void OnDamageChange()
    {
        if(!StatsSystem.Instance || !damageText)
            return;
        
        _stringBuilder.Clear();
        _stringBuilder.Append("Damage:");
        _stringBuilder.Append(StatsSystem.Instance.Damage);
        _stringBuilder.Append("/25");
        
        damageText.text = _stringBuilder.ToString();
    }
    
    public void OnPointsChange()
    {
        if (!CollectableManager.Instance || !updatePoints) 
            return;

        if (CollectableManager.Instance.CurrentPoints > CollectableManager.Instance.PointsThreshold)
        {
            CollectableManager.Instance.CurrentPoints = CollectableManager.Instance.PointsThreshold;
        }
        
        _stringBuilder.Clear();
        _stringBuilder.Append("Upgrade: ");
        _stringBuilder.Append(CollectableManager.Instance.CurrentPoints);
        _stringBuilder.Append("/");
        _stringBuilder.Append(CollectableManager.Instance.PointsThreshold);
        
        updatePoints.text = _stringBuilder.ToString();
    }
    
    public void MenuClicked()
    {
        SceneController.LoadMainMenu();
    }

    public void DisableRulesPanel()
    {
        rulesPanel?.SetActive(false);
        menuButton?.SetActive(true);
        _isCountingUp = true;
        Time.timeScale = 1f;
    }
    
    public void EnableAbilityPanel()
    {
        abilityPanel.SetActive(true);
    }
    
    public void DisableAbilityPanel()
    {
        abilityPanel.SetActive(false);
    }
    
    public void ExitClicked()
    {
        SceneController.QuitGame();
    }

    public void ContinueClicked()
    {
        if (menuPanel.activeInHierarchy)
        {
            menuPanel?.SetActive(false);
            menuButton?.SetActive(true);
            Time.timeScale = 1f;
        }
    }

    public void MenuPanelClicked()
    {
        Time.timeScale = 0f;
        menuPanel?.SetActive(true);
        menuButton?.SetActive(false);
    }

    public void LeaderboardClicked()
    {
        if(!leaderboardPanel)
            return;
        
        menuPanel?.SetActive(false);
        leaderboardPanel?.SetActive(true);
        
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

    public void LeaderboardBackClicked()
    {
        leaderboardPanel?.SetActive(false);
        menuPanel?.SetActive(true);
    }
    
    private void ErrorLogger(string message)
    {
#if UNITY_EDITOR
        Debug.LogError($"[GameUIManager] {message}");
#endif
    }
    
    private void WarningLogger(string message)
    {
#if UNITY_EDITOR
        Debug.LogWarning($"[GameUIManager] {message}");
#endif
    }
}
