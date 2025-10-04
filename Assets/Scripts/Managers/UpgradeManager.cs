using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Available Upgrades")]
    [SerializeField] private List<UpgradeData> availableUpgrades = new List<UpgradeData>();

    [Header("Max Effect Settings")]
    [Tooltip("Maximum total multiplier for each upgrade type (e.g., 3.0 = 300% max)")]
    [SerializeField] private float maxEffectMultiplier = 3f;

    [Header("Pool Behavior")]
    [Tooltip("Keep showing maxed upgrades in selection?")]
    [SerializeField] private bool showMaxedUpgrades;

    [Header("Events")]
    public UnityEvent<UpgradeData, float> onUpgradeApplied; // Passes upgrade and current total multiplier

    // Track current multiplier for each upgrade type
    private readonly Dictionary<UpgradeType, float> _currentMultipliers = new Dictionary<UpgradeType, float>();

    // Track how many times each upgrade type has been taken
    private readonly Dictionary<UpgradeType, int> _upgradeStacks = new Dictionary<UpgradeType, int>();

    // Cached lists to avoid allocations (reused every call)
    private readonly List<UpgradeData> _cachedEligibleUpgrades = new List<UpgradeData>();
    private readonly List<UpgradeData> _cachedRemainingUpgrades = new List<UpgradeData>();
    private readonly List<UpgradeData> _cachedSelectedUpgrades = new List<UpgradeData>();

    // Cached StringBuilder for string operations
    private readonly StringBuilder _stringBuilder = new StringBuilder(256);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeUpgrades();
    }

    private void OnDestroy()
    {
        // Clear all references to prevent memory leaks
        _currentMultipliers.Clear();
        _upgradeStacks.Clear();
        _cachedEligibleUpgrades.Clear();
        _cachedRemainingUpgrades.Clear();
        _cachedSelectedUpgrades.Clear();
        _stringBuilder.Clear();
    }

    /// <summary>
    /// Initialize all upgrade types with base multiplier of 1.0
    /// </summary>
    private void InitializeUpgrades()
    {
        foreach (UpgradeType type in System.Enum.GetValues(typeof(UpgradeType)))
        {
            _currentMultipliers[type] = 1f; // Base multiplier
            _upgradeStacks[type] = 0;
        }

#if UNITY_EDITOR
        if (availableUpgrades.Count == 0)
        {
            Debug.LogWarning("[UpgradeManager] No upgrades assigned! Add UpgradeData assets in inspector.");
        }
#endif
    }

    /// <summary>
    /// Get two random, different upgrades for player selection (optimized, no allocations)
    /// </summary>
    public List<UpgradeData> GetRandomUpgrades()
    {
        // Clear and reuse cached list
        _cachedSelectedUpgrades.Clear();
        
        GetEligibleUpgrades(_cachedEligibleUpgrades);

        if (_cachedEligibleUpgrades.Count == 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[UpgradeManager] No eligible upgrades available!");
#endif
            return _cachedSelectedUpgrades;
        }

        // If only one upgrade available, return it
        if (_cachedEligibleUpgrades.Count == 1)
        {
            _cachedSelectedUpgrades.Add(_cachedEligibleUpgrades[0]);
            return _cachedSelectedUpgrades;
        }

        // First upgrade
        int randomIndex1 = Random.Range(0, _cachedEligibleUpgrades.Count);
        _cachedSelectedUpgrades.Add(_cachedEligibleUpgrades[randomIndex1]);

        // Second upgrade (ensure it's different type)
        _cachedRemainingUpgrades.Clear();
        UpgradeType firstUpgradeType = _cachedSelectedUpgrades[0].upgradeType;
        
        for (int i = 0; i < _cachedEligibleUpgrades.Count; i++)
        {
            if (_cachedEligibleUpgrades[i].upgradeType != firstUpgradeType)
            {
                _cachedRemainingUpgrades.Add(_cachedEligibleUpgrades[i]);
            }
        }

        if (_cachedRemainingUpgrades.Count > 0)
        {
            int randomIndex2 = Random.Range(0, _cachedRemainingUpgrades.Count);
            _cachedSelectedUpgrades.Add(_cachedRemainingUpgrades[randomIndex2]);
        }

        return _cachedSelectedUpgrades;
    }

    /// <summary>
    /// Get list of upgrades that can still be applied (fills provided list, no allocation)
    /// </summary>
    private void GetEligibleUpgrades(List<UpgradeData> outList)
    {
        outList.Clear();

        if (showMaxedUpgrades)
        {
            // Copy all upgrades to output list
            for (int i = 0; i < availableUpgrades.Count; i++)
            {
                outList.Add(availableUpgrades[i]);
            }
            return;
        }

        // Filter out maxed upgrades
        for (int i = 0; i < availableUpgrades.Count; i++)
        {
            if (!IsUpgradeMaxed(availableUpgrades[i].upgradeType))
            {
                outList.Add(availableUpgrades[i]);
            }
        }
    }

    /// <summary>
    /// Check if an upgrade type has reached max effect
    /// </summary>
    public bool IsUpgradeMaxed(UpgradeType type)
    {
        return _currentMultipliers[type] >= maxEffectMultiplier;
    }

    /// <summary>
    /// Apply the selected upgrade to the player
    /// </summary>
    public void ApplyUpgrade(UpgradeData upgrade)
    {
        if (upgrade == null)
        {
#if UNITY_EDITOR
            Debug.LogError("[UpgradeManager] Tried to apply null upgrade!");
#endif
            return;
        }

        UpgradeType type = upgrade.upgradeType;

        // Check if already maxed
        if (IsUpgradeMaxed(type))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[UpgradeManager] {type} is already maxed out!");
#endif
            return;
        }

        // Calculate new multiplier (additive stacking)
        // Formula: New Value = Current Value + (Base Value × (multiplier - 1))
        // Base value is always 1.0
        float oldMultiplier = _currentMultipliers[type];
        float additiveBonus = 1f * (upgrade.multiplier - 1f); // Bonus from this upgrade
        float newMultiplier = oldMultiplier + additiveBonus;

        // Cap at max effect
        newMultiplier = Mathf.Min(newMultiplier, maxEffectMultiplier);

        // Update tracking
        _currentMultipliers[type] = newMultiplier;
        _upgradeStacks[type]++;

#if UNITY_EDITOR
        Debug.Log($"[UpgradeManager] Applied {upgrade.upgradeName}");
        Debug.Log($"[UpgradeManager] {type}: {oldMultiplier:F2}x → {newMultiplier:F2}x (+{additiveBonus:F2}) [Stack: {_upgradeStacks[type]}]");
#endif

        // Invoke event for PlayerStats to listen
        onUpgradeApplied?.Invoke(upgrade, newMultiplier);
    }

    /// <summary>
    /// Get current total multiplier for a specific upgrade type
    /// </summary>
    public float GetCurrentMultiplier(UpgradeType type)
    {
        return _currentMultipliers.ContainsKey(type) ? _currentMultipliers[type] : 1f;
    }

    /// <summary>
    /// Get number of times an upgrade type has been taken
    /// </summary>
    public int GetStackCount(UpgradeType type)
    {
        return _upgradeStacks.ContainsKey(type) ? _upgradeStacks[type] : 0;
    }

    /// <summary>
    /// Reset all upgrades (for new game)
    /// </summary>
    public void ResetUpgrades()
    {
        InitializeUpgrades();
#if UNITY_EDITOR
        Debug.Log("[UpgradeManager] All upgrades reset");
#endif
    }

    /// <summary>
    /// Get upgrade statistics for debugging/UI (optimized with StringBuilder)
    /// </summary>
    public string GetUpgradeStats()
    {
        _stringBuilder.Clear();
        _stringBuilder.AppendLine("=== Current Upgrades ===");
        
        foreach (UpgradeType type in System.Enum.GetValues(typeof(UpgradeType)))
        {
            float multiplier = _currentMultipliers[type];
            int stacks = _upgradeStacks[type];
            
            _stringBuilder.Append(type.ToString());
            _stringBuilder.Append(": ");
            _stringBuilder.Append(multiplier.ToString("F2"));
            _stringBuilder.Append("x (x");
            _stringBuilder.Append(stacks);
            _stringBuilder.Append(")");
            
            if (IsUpgradeMaxed(type))
            {
                _stringBuilder.Append(" [MAXED]");
            }
            
            _stringBuilder.AppendLine();
        }
        
        return _stringBuilder.ToString();
    }
}