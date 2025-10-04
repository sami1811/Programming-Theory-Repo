using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button upgradeOption1Button;
    [SerializeField] private Button upgradeOption2Button;
    public TMP_Text upgradePercentage;
    
    [Header("Option 1 UI Elements")]
    [SerializeField] private TMP_Text option1NameText;
    
    [Header("Option 2 UI Elements")]
    [SerializeField] private TMP_Text option2NameText;

    [Header("Game Manager Object")]
    [SerializeField] private GameManager gameManager;

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
        if (!gameManager)
        {
#if UNITY_EDITOR
            Debug.LogError("[UpgradeUIManager] GameManager not found!");
#endif
        }

        if (upgradeOption1Button)
        {
            upgradeOption1Button.onClick.AddListener(OnOptionOneSelected);
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogError("[UpgradeUIManager] Option 1 button not found!");
#endif
        }
        
        if (upgradeOption2Button)
        {
            upgradeOption2Button.onClick.AddListener(OnOptionTwoSelected);
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogError("[UpgradeUIManager] Option 2 button not found!");
#endif
        }
        
        if (!upgradePercentage)
        {
#if UNITY_EDITOR
            Debug.LogError("[UpgradeUIManager] Assign upgrade percentage text!");
#endif
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
#if UNITY_EDITOR
            Debug.LogError("[UpgradeUIManager] CollectableManager instance not found!");
#endif
        }
        
        if (!UpgradeManager.Instance)
        {
#if UNITY_EDITOR
            Debug.LogError("[UpgradeUIManager] UpgradeManager instance not found!");
#endif
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
    
    /// <summary>
    /// Called when threshold is reached - gets real upgrade options from UpgradeManager
    /// </summary>
    public void ShowUpgradeOptions()
    {
        if (!UpgradeManager.Instance)
        {
#if UNITY_EDITOR
            Debug.LogError("[UpgradeUIManager] UpgradeManager not available!");
#endif
            return;
        }

        // Get random upgrades from UpgradeManager (uses cached list, no allocation)
        List<UpgradeData> upgrades = UpgradeManager.Instance.GetRandomUpgrades();
        
        if (upgrades.Count == 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[UpgradeUIManager] No upgrades available!");
#endif
            return;
        }

        // Store selected upgrades
        _selectedOption1 = upgrades[0];
        _selectedOption2 = upgrades.Count > 1 ? upgrades[1] : null;

        UpdateUpgradeUI();
        gameManager?.EnableUpgradeSelection();

#if UNITY_EDITOR
        Debug.Log("[UpgradeUIManager] Showing upgrade options");
#endif
    }

    /// <summary>
    /// Updates UI progress text (optimized with StringBuilder)
    /// </summary>
    public void OnPointsChange()
    {
        if (!CollectableManager.Instance || !upgradePercentage) 
            return;

        _stringBuilder.Clear();
        _stringBuilder.Append("Upgrade ");
        _stringBuilder.Append(CollectableManager.Instance.GetProgressPercentage().ToString("F0"));
        _stringBuilder.Append("/100");
        
        upgradePercentage.text = _stringBuilder.ToString();
    }
    
    /// <summary>
    /// Updates the UI with current upgrade options
    /// </summary>
    private void UpdateUpgradeUI()
    {
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
    
    /// <summary>
    /// Called when player selects the first upgrade option
    /// </summary>
    private void OnOptionOneSelected()
    {
        if (_selectedOption1 == null)
        {
#if UNITY_EDITOR
            Debug.LogError("[UpgradeUIManager] Option 1 is null!");
#endif
            return;
        }

#if UNITY_EDITOR
        Debug.Log($"[UpgradeUIManager] Player selected: {_selectedOption1.upgradeName}");
#endif
        
        // Apply upgrade through UpgradeManager
        UpgradeManager.Instance?.ApplyUpgrade(_selectedOption1);

        CollectableManager.Instance?.ResetPoints();
        CloseUpgradePanel();
    }
    
    /// <summary>
    /// Called when player selects the second upgrade option
    /// </summary>
    private void OnOptionTwoSelected()
    {
        if (_selectedOption2 == null)
        {
#if UNITY_EDITOR
            Debug.LogError("[UpgradeUIManager] Option 2 is null!");
#endif
            return;
        }

#if UNITY_EDITOR
        Debug.Log($"[UpgradeUIManager] Player selected: {_selectedOption2.upgradeName}");
#endif
        
        // Apply upgrade through UpgradeManager
        UpgradeManager.Instance?.ApplyUpgrade(_selectedOption2);

        CollectableManager.Instance?.ResetPoints();
        CloseUpgradePanel();
    }
    
    /// <summary>
    /// Closes the upgrade panel
    /// </summary>
    private void CloseUpgradePanel()
    {
        // Re-enable button 2 in case it was disabled
        if (upgradeOption2Button)
            upgradeOption2Button.interactable = true;

        gameManager?.DisableUpgradeSelection();
    }
}