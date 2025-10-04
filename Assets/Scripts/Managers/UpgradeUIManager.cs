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

    // Placeholder upgrade data (will be replaced with actual UpgradeData later)
    private string _selectedOption1Name;
    private string _selectedOption2Name;
    
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
            Debug.LogError("[Collectable Manager]Assign upgrade percentage text!");
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
    }
    
    public void ShowUpgradeOptions()
    {
        GeneratePlaceholderUpgrades();
        UpdateUpgradeUI();
        
        gameManager?.EnableUpgradeSelection();

#if UNITY_EDITOR
        Debug.Log("[UpgradeUIManager] Showing upgrade options");
#endif
    }

    public void OnPointsChange()
    {
        if (CollectableManager.Instance)
        {
            upgradePercentage.text = $"Upgrade {CollectableManager.Instance.GetProgressPercentage()}/100";
        }
    }
    
    private void GeneratePlaceholderUpgrades()
    {
        // Placeholder upgrade names (will be replaced with actual UpgradeData)
        string[] placeholderUpgrades = new string[]
        {
            "Increase Fire Rate",
            "Health Regeneration",
            "Movement Speed Boost",
            "Damage Increase",
            "Shield Protection",
            "Critical Hit Chance"
        };

        // Select two random upgrades (ensuring they are different)
        int index1 = Random.Range(0, placeholderUpgrades.Length);
        int index2 = Random.Range(0, placeholderUpgrades.Length);
        
        // Ensure different options
        while (index2 == index1)
        {
            index2 = Random.Range(0, placeholderUpgrades.Length);
        }

        _selectedOption1Name = placeholderUpgrades[index1];
        _selectedOption2Name = placeholderUpgrades[index2];
    }
    
    private void UpdateUpgradeUI()
    {
        // Update Option 1
        if (option1NameText)
            option1NameText.text = _selectedOption1Name;

        // Update Option 2
        if (option2NameText)
            option2NameText.text = _selectedOption2Name;
    }
    
    private void OnOptionOneSelected()
    {
        Debug.Log($"[UpgradeUIManager] Player selected: {_selectedOption1Name}");
        
        // TODO: Apply upgrade to player via UpgradeSystem
        // UpgradeSystem.Instance.ApplyUpgrade(selectedUpgrade1);

        CollectableManager.Instance?.ResetPoints();
        CloseUpgradePanel();
    }
    
    private void OnOptionTwoSelected()
    {
        Debug.Log($"[UpgradeUIManager] Player selected: {_selectedOption2Name}");
        
        // TODO: Apply upgrade to player via UpgradeSystem
        // UpgradeSystem.Instance.ApplyUpgrade(selectedUpgrade2);

        CollectableManager.Instance?.ResetPoints();
        CloseUpgradePanel();
    }
    
    private void CloseUpgradePanel()
    {
        gameManager?.DisableUpgradeSelection();
    }
}
