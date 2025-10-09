using UnityEngine;

/// <summary>
/// Enum defining all available upgrade types
/// </summary>
public enum UpgradeType
{
    FireRate,
    HealthRegen,
    MovementSpeed,
    Damage,
    Points
}

/// <summary>
/// ScriptableObject that stores data for a single upgrade
/// </summary>
[CreateAssetMenu(fileName = "New Upgrade", menuName = "Game/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("Upgrade Information")]
    [Tooltip("Display name shown in UI")]
    public string upgradeName;
    
    [Tooltip("Type of upgrade - determines which stat to modify")]
    public UpgradeType upgradeType;
    
    [Header("Effect Settings")]
    [Tooltip("Multiplier applied to the base stat (e.g., 1.2 = 20% increase)")]
    [Range(1f, 3f)]
    public float multiplier = 1.2f;
    
    [Header("Optional Description")]
    [Tooltip("Description shown in UI (optional)")]
    [TextArea(2, 4)]
    public string description;
    
    [Header("Health Regenerate Settings")]
    public float regenDuration;

    /// <summary>
    /// Validates the upgrade data in the editor
    /// </summary>
    private void OnValidate()
    {
        // Ensure multiplier is at least 1 (no negative effects)
        if (multiplier < 1f)
        {
            multiplier = 1f;
            Debug.LogWarning($"[{upgradeName}] Multiplier cannot be less than 1. Reset to 1.");
        }

        // Auto-generate description if empty
        if (string.IsNullOrEmpty(description))
        {
            int percentIncrease = Mathf.RoundToInt((multiplier - 1f) * 100f);
            description = $"Increases {upgradeType} by {percentIncrease}%";
        }
    }
}