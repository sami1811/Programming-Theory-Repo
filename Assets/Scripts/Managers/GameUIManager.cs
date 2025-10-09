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
    
    private float _countDownTimer;
    private bool _isCountingDown;
    
    private readonly StringBuilder _stringBuilder = new StringBuilder(32);
    
    private void Awake()
    {
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
        //playerIntro.text = "Hi " + DataManager.Instance.playerName; 
    }

    private void Update()
    {
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
    }
    
    public void EnableAbilityPanel()
    {
        abilityPanel.SetActive(true);
    }
    
    public void DisableAbilityPanel()
    {
        abilityPanel.SetActive(false);
    }

    private void ErrorLogger(string message)
    {
#if UNITY_EDITOR
        Debug.LogError($"[GameUIManager] {message}");
#endif
    }
}
