using UnityEngine;
using System.Text;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class AbilityUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button upgradeOption1Button;
    [SerializeField] private Button upgradeOption2Button;
    [SerializeField] private TMP_Text updatePoints;
    
    [Header("Option 1 UI Elements")]
    [SerializeField] private TMP_Text option1NameText;
    
    [Header("Option 2 UI Elements")]
    [SerializeField] private TMP_Text option2NameText;

    [Header("Game Manager Settings")]
    [SerializeField] private GameUIManager gameUIManager;

    // Current upgrade options (using actual UpgradeData)
    private UpgradeData _selectedOption1;
    private UpgradeData _selectedOption2;
    
    // Cached StringBuilder for text formatting (zero allocation)
    private StringBuilder _stringBuilder = new StringBuilder(32);
    
    private void Awake()
    {
        InitializeAwake();
    }

    private void InitializeAwake()
    {
        if (!gameUIManager)
        {
            ErrorLogger("GameUIManager not found!");
        }

        if (upgradeOption1Button)
        {
            upgradeOption1Button.onClick.AddListener(OnOptionOneSelected);
        }
        else
        {
            ErrorLogger("Option 1 button not found!");
        }
        
        if (upgradeOption2Button)
        {
            upgradeOption2Button.onClick.AddListener(OnOptionTwoSelected);
        }
        else
        {
            ErrorLogger("Option 2 button not found!");
        }
        
        if (!updatePoints)
        {
            ErrorLogger("Percentage text not found!");
        }
    }

    private void Start()
    {
        InitializeStart();
    }

    private void InitializeStart()
    {
        if (CollectableManager.Instance)
        {
            CollectableManager.Instance.onThresholdReached.AddListener(ShowUpgradeOptions);
        }
        else
        {
            ErrorLogger("CollectableManager instance not found!");
        }
        
        if(!AbilityManager.Instance)
        {
            ErrorLogger("AbilityManager not found!");
        }
    }

    private void OnDestroy()
    {
        if (CollectableManager.Instance)
        {
            CollectableManager.Instance.onThresholdReached.RemoveListener(ShowUpgradeOptions);
        }

        // Remove button listeners
        if (upgradeOption1Button)
            upgradeOption1Button.onClick.RemoveListener(OnOptionOneSelected);
        
        if (upgradeOption2Button)
            upgradeOption2Button.onClick.RemoveListener(OnOptionTwoSelected);
        
        // Clear cached data
        _selectedOption1 = null;
        _selectedOption2 = null;
        _stringBuilder.Clear();
    }
    
    public void ShowUpgradeOptions()
    {
        if (!AbilityManager.Instance)
        {
            ErrorLogger("AbilityManager Instance not found!");
            return;
        }
        
        // Get random upgrades from UpgradeManager (uses cached list, no allocation)
        List<UpgradeData> upgrades = AbilityManager.Instance.GetRandomUpgrades();

        if (upgrades.Count == 0)
        {
            WarningLogger("No upgrades are available!");
            return;
        }
        
        // Store selected upgrades
        _selectedOption1 = upgrades[0];
        _selectedOption2 = upgrades.Count > 1 ? upgrades[1] : null;
        
        UpdateUpgradeUI();
        gameUIManager?.EnableAbilityPanel();
        
        Logger("Showing upgrade options");
    }

    public void OnPointsChange()
    {
        if (!CollectableManager.Instance || !updatePoints) 
            return;

        _stringBuilder.Clear();
        _stringBuilder.Append("Upgrade ");
        _stringBuilder.Append(CollectableManager.Instance.GetProgressPercentage().ToString("F0"));
        _stringBuilder.Append("/100");
        
        updatePoints.text = _stringBuilder.ToString();
    }
    
    private void UpdateUpgradeUI()
    {
        EventSystem.current.SetSelectedGameObject(null);
        
        // Update Option 1
        if (option1NameText && _selectedOption1 != null)
        {
            option1NameText.text = _selectedOption1.upgradeName;
        }

        // Update Option 2
        if (option2NameText && _selectedOption2 != null)
        {
            option2NameText.text = _selectedOption2.upgradeName;
        }
        else if (option2NameText)
        {
            // Hide second option if not available
            option2NameText.text = "N/A";
            if (upgradeOption2Button)
                upgradeOption2Button.interactable = false;
        }
    }
    
    private void OnOptionOneSelected()
    {
        if (!_selectedOption1)
        {
            ErrorLogger("Option 1 is null!");
            return;
        }
        
        Logger($"Player selected: {_selectedOption1.upgradeName}");
        
        // Apply upgrade through UpgradeManager
        AbilityManager.Instance?.ApplyUpgrade(_selectedOption1);

        CollectableManager.Instance?.ResetPoints();
        CloseUpgradePanel();
    }
    
    private void OnOptionTwoSelected()
    {
        if (!_selectedOption2)
        {
            ErrorLogger("Option 2 is null!");
            return;
        }
        
        Logger($"Player selected: {_selectedOption2.upgradeName}");
        
        // Apply upgrade through UpgradeManager
        AbilityManager.Instance?.ApplyUpgrade(_selectedOption2);

        CollectableManager.Instance?.ResetPoints();
        CloseUpgradePanel();
    }
    
    private void CloseUpgradePanel()
    {
        if(upgradeOption1Button)
            upgradeOption1Button.interactable = true;
        
        if (upgradeOption2Button)
            upgradeOption2Button.interactable = true;

        gameUIManager?.DisableAbilityPanel();
    }
    
    private void ErrorLogger(string message)
    {
#if UNITY_EDITOR
        Debug.LogError($"[AbilityUIManager] {message}");
#endif
    }
    
    private void Logger(string message)
    {
#if UNITY_EDITOR
        Debug.Log($"[AbilityUIManager] {message}");
#endif
    }
    
    private void WarningLogger(string message)
    {
#if UNITY_EDITOR
        Debug.LogWarning($"[AbilityUIManager] {message}");
#endif
    }
}