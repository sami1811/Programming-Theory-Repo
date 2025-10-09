using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(-100)]
public class StatsSystem : MonoBehaviour
{
    public static StatsSystem Instance {  get; private set; }
        
    [Header("Stat Configuration")]
    [Tooltip("Enable fire rate stat for this object")]
    [SerializeField] private bool useFireRate;
    [Tooltip("Enable health regeneration stat for this object")]
    [SerializeField] private bool useHealthRegen;
    [Tooltip("Enable movement speed stat for this object")]
    [SerializeField] private bool useMovementSpeed;
    [Tooltip("Enable damage stat for this object")]
    [SerializeField] private bool useDamage;
    [Tooltip("Enable points multiplier stat for this object")]
    [SerializeField] private bool usePoints;

    [Header("Base Stat Values")]
    [SerializeField] private float baseFireRate;
    [SerializeField] private float baseHealthRegen;
    [SerializeField] private float baseMovementSpeed;
    [SerializeField] private float baseDamage;
    [SerializeField] private float basePoints;

    [Header("Events")]
    [Tooltip("Invoked when any stat is updated")]
    public UnityEvent<UpgradeType, float> onStatUpdated;

    // Current multipliers for each stat
    private float _fireRateMultiplier = 1f;
    private float _healthRegenMultiplier = 1f;
    private float _movementSpeedMultiplier = 1f;
    private float _damageMultiplier = 1f;
    private float _pointsMultiplier = 1f;

    // Public properties to get final calculated stats
    public float FireRate => baseFireRate * _fireRateMultiplier;
    public float HealthRegen => baseHealthRegen * _healthRegenMultiplier;
    public float MovementSpeed => baseMovementSpeed * _movementSpeedMultiplier;
    public float Damage => baseDamage * _damageMultiplier;
    public float Points => basePoints * _pointsMultiplier;
    
    // Properties to check which stats are active
    public bool HasFireRate => useFireRate;
    public bool HasHealthRegen => useHealthRegen;
    public bool HasMovementSpeed => useMovementSpeed;
    public bool HasDamage => useDamage;
    public bool HasPoints => usePoints;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        InitializeStats();
    }

    private void OnEnable()
    {
        // Subscribe to upgrade events
        if (AbilityManager.Instance)
        {
            AbilityManager.Instance.onUpgradeApplied.AddListener(OnUpgradeReceived);
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[PlayerStats] Ability manager not found for {gameObject.name}");
#endif
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        if (AbilityManager.Instance)
        {
            AbilityManager.Instance.onUpgradeApplied.RemoveListener(OnUpgradeReceived);
        }
    }

    /// <summary>
    /// Initialize stats by syncing with UpgradeManager's current state
    /// </summary>
    private void InitializeStats()
    {
        if (!AbilityManager.Instance)
            return;

        // Sync multipliers with current upgrade state
        if (useFireRate)
            _fireRateMultiplier = AbilityManager.Instance.GetCurrentMultiplier(UpgradeType.FireRate);

        if (useHealthRegen)
            _healthRegenMultiplier = AbilityManager.Instance.GetCurrentMultiplier(UpgradeType.HealthRegen);

        if (useMovementSpeed)
            _movementSpeedMultiplier = AbilityManager.Instance.GetCurrentMultiplier(UpgradeType.MovementSpeed);

        if (useDamage)
            _damageMultiplier = AbilityManager.Instance.GetCurrentMultiplier(UpgradeType.Damage);
        
        if (usePoints)
            _pointsMultiplier = AbilityManager.Instance.GetCurrentMultiplier(UpgradeType.Points);

#if UNITY_EDITOR
        LogCurrentStats();
#endif
    }

    /// <summary>
    /// Called when an upgrade is applied through UpgradeManager
    /// </summary>
    private void OnUpgradeReceived(UpgradeData upgrade, float newMultiplier)
    {
        if (!upgrade)
            return;

        // Only update stats that this object uses
        bool statUpdated = false;

        switch (upgrade.upgradeType)
        {
            case UpgradeType.FireRate:
                if (useFireRate)
                {
                    _fireRateMultiplier = newMultiplier;
                    statUpdated = true;
#if UNITY_EDITOR
                    Debug.Log($"[PlayerStats] {gameObject.name} Fire Rate updated: {FireRate:F2}");
#endif
                }
                break;

            case UpgradeType.HealthRegen:
                if (useHealthRegen)
                {
                    _healthRegenMultiplier = newMultiplier;
                    statUpdated = true;
#if UNITY_EDITOR
                    Debug.Log($"[PlayerStats] {gameObject.name} Health Regen updated: {HealthRegen:F2}");
#endif
                }
                break;

            case UpgradeType.MovementSpeed:
                if (useMovementSpeed)
                {
                    _movementSpeedMultiplier = newMultiplier;
                    
                    statUpdated = true;
#if UNITY_EDITOR
                    Debug.Log($"[PlayerStats] {gameObject.name} Movement Speed updated: {MovementSpeed:F2}");
#endif
                }
                break;

            case UpgradeType.Damage:
                if (useDamage)
                {
                    _damageMultiplier = newMultiplier;
                    statUpdated = true;
#if UNITY_EDITOR
                    Debug.Log($"[PlayerStats] {gameObject.name} Damage updated: {Damage:F2}");
#endif
                }
                break;
            
            case UpgradeType.Points:
                if (usePoints)
                {
                    _pointsMultiplier = newMultiplier;
                    statUpdated = true;
#if UNITY_EDITOR
                    Debug.Log($"[PlayerStats] {gameObject.name} Points updated: {Points:F2}");
#endif
                }
                break;
        }

        // Invoke event if a stat was updated
        if (statUpdated)
        {
            onStatUpdated?.Invoke(upgrade.upgradeType, newMultiplier);
        }
    }

    /// <summary>
    /// Get the current multiplier for a specific stat type
    /// </summary>
    public float GetMultiplier(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.FireRate:
                return _fireRateMultiplier;
            case UpgradeType.HealthRegen:
                return _healthRegenMultiplier;
            case UpgradeType.MovementSpeed:
                return _movementSpeedMultiplier;
            case UpgradeType.Damage:
                return _damageMultiplier;
            case UpgradeType.Points:
                return _pointsMultiplier;
            default:
                return 1f;
        }
    }

    /// <summary>
    /// Get base value for a specific stat type
    /// </summary>
    public float GetBaseValue(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.FireRate:
                return baseFireRate;
            case UpgradeType.HealthRegen:
                return baseHealthRegen;
            case UpgradeType.MovementSpeed:
                return baseMovementSpeed;
            case UpgradeType.Damage:
                return baseDamage;
            case UpgradeType.Points:
                return basePoints;
            default:
                return 0f;
        }
    }

    /// <summary>
    /// Manually set a base stat value (useful for runtime adjustments)
    /// </summary>
    public void SetBaseValue(UpgradeType type, float value)
    {
        switch (type)
        {
            case UpgradeType.FireRate:
                baseFireRate = value;
                break;
            case UpgradeType.HealthRegen:
                baseHealthRegen = value;
                break;
            case UpgradeType.MovementSpeed:
                baseMovementSpeed = value;
                break;
            case UpgradeType.Damage:
                baseDamage = value;
                break;
            case UpgradeType.Points:
                basePoints = value;
                break;
        }
    }

    /// <summary>
    /// Reset all multipliers to 1.0 (base values)
    /// </summary>
    public void ResetMultipliers()
    {
        _fireRateMultiplier = 1f;
        _healthRegenMultiplier = 1f;
        _movementSpeedMultiplier = 1f;
        _damageMultiplier = 1f;
        _pointsMultiplier = 1f;

#if UNITY_EDITOR
        Debug.Log($"[PlayerStats] {gameObject.name} multipliers reset");
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// Debug logging for current stats
    /// </summary>
    private void LogCurrentStats()
    {
        Debug.Log($"=== {gameObject.name} Stats ===");
        if (useFireRate)
            Debug.Log($"Fire Rate: {FireRate:F2} (Base: {baseFireRate} × {_fireRateMultiplier:F2})");
        if (useHealthRegen)
            Debug.Log($"Health Regen: {HealthRegen:F2} (Base: {baseHealthRegen} × {_healthRegenMultiplier:F2})");
        if (useMovementSpeed)
            Debug.Log($"Movement Speed: {MovementSpeed:F2} (Base: {baseMovementSpeed} × {_movementSpeedMultiplier:F2})");
        if (useDamage)
            Debug.Log($"Damage: {Damage:F2} (Base: {baseDamage} × {_damageMultiplier:F2})");
        if (usePoints)
            Debug.Log($"Points: {Points:F2} (Base: {basePoints} × {_pointsMultiplier:F2})");
    }
#endif
}