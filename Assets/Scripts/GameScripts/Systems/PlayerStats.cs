using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class PlayerStats : MonoBehaviour
{
    [Header("Stat Configuration")]
    [Tooltip("Enable fire rate stat for this object")]
    [SerializeField] private bool useFireRate;
    [Tooltip("Enable health regeneration stat for this object")]
    [SerializeField] private bool useHealthRegen;
    [Tooltip("Enable movement speed stat for this object")]
    [SerializeField] private bool useMovementSpeed;
    [Tooltip("Enable damage stat for this object")]
    [SerializeField] private bool useDamage;

    [FormerlySerializedAs("OnStatUpdated")]
    [Header("Events")]
    [Tooltip("Invoked when any stat is updated")]
    public UnityEvent<UpgradeType, float> onStatUpdated;
    
    private float _baseFireRate;
    private float _baseHealthRegen;
    private float _baseMovementSpeed;
    private float _baseDamage;
    
    // Current multipliers for each stat
    private float _fireRateMultiplier = 1f;
    private float _healthRegenMultiplier = 1f;
    private float _movementSpeedMultiplier = 1f;
    private float _damageMultiplier = 1f;

    private PlayerController _playerController;
    private HealthSystem _healthSystem;
    
    // Public properties to get final calculated stats
    public float FireRate => _baseFireRate * _fireRateMultiplier;
    public float HealthRegen => _baseHealthRegen * _healthRegenMultiplier;
    public float MovementSpeed => _baseMovementSpeed * _movementSpeedMultiplier;
    public float Damage => _baseDamage * _damageMultiplier;

    // Properties to check which stats are active
    public bool HasFireRate => useFireRate;
    public bool HasHealthRegen => useHealthRegen;
    public bool HasMovementSpeed => useMovementSpeed;
    public bool HasDamage => useDamage;

    private void Awake()
    {
        InitializeAwake();
    }

    private void InitializeAwake()
    {
        if (useMovementSpeed || useFireRate)
        {
            _playerController = GetComponent<PlayerController>();
            if (!_playerController)
            {
                Debug.LogError("[PlayerStats] Player controller not found!]");
            }
            
            _baseFireRate = _playerController.FireRate;
            _baseMovementSpeed =  _playerController.MoveSpeed;
        }
        
        if (useHealthRegen || useDamage)
        {
            _healthSystem = GetComponent<HealthSystem>();
            if (!_healthSystem)
            {
                Debug.LogError("[PlayerStats] Health System not found!]");
            }
            
            _baseHealthRegen = _healthSystem.CurrentHealth;
            
            if (_healthSystem.gameObject.layer != LayerMask.NameToLayer("Cube"))
            {
                _baseDamage = _healthSystem.DamagePerHit;
            }
        }
    }

    private void Start()
    {
        InitializeStats();
    }

    private void OnEnable()
    {
        // Subscribe to upgrade events
        if (UpgradeManager.Instance)
        {
            UpgradeManager.Instance.onUpgradeApplied.AddListener(OnUpgradeReceived);
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[PlayerStats] UpgradeManager not found for {gameObject.name}");
#endif
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        if (UpgradeManager.Instance)
        {
            UpgradeManager.Instance.onUpgradeApplied.RemoveListener(OnUpgradeReceived);
        }
    }

    /// <summary>
    /// Initialize stats by syncing with UpgradeManager's current state
    /// </summary>
    private void InitializeStats()
    {
        if (!UpgradeManager.Instance)
            return;

        // Sync multipliers with current upgrade state
        if (useFireRate)
            _fireRateMultiplier = UpgradeManager.Instance.GetCurrentMultiplier(UpgradeType.FireRate);

        if (useHealthRegen)
            _healthRegenMultiplier = UpgradeManager.Instance.GetCurrentMultiplier(UpgradeType.HealthRegen);

        if (useMovementSpeed)
            _movementSpeedMultiplier = UpgradeManager.Instance.GetCurrentMultiplier(UpgradeType.MovementSpeed);

        if (useDamage)
            _damageMultiplier = UpgradeManager.Instance.GetCurrentMultiplier(UpgradeType.Damage);

#if UNITY_EDITOR
        LogCurrentStats();
#endif
    }

    /// <summary>
    /// Called when an upgrade is applied through UpgradeManager
    /// </summary>
    private void OnUpgradeReceived(UpgradeData upgrade, float newMultiplier)
    {
        if (upgrade == null)
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
                return _baseFireRate;
            case UpgradeType.HealthRegen:
                return _baseHealthRegen;
            case UpgradeType.MovementSpeed:
                return _baseMovementSpeed;
            case UpgradeType.Damage:
                return _baseDamage;
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
                _baseFireRate = value;
                break;
            case UpgradeType.HealthRegen:
                _baseHealthRegen = value;
                break;
            case UpgradeType.MovementSpeed:
                _baseMovementSpeed = value;
                break;
            case UpgradeType.Damage:
                _baseDamage = value;
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
            Debug.Log($"Fire Rate: {FireRate:F2} (Base: {_baseFireRate} × {_fireRateMultiplier:F2})");
        if (useHealthRegen)
            Debug.Log($"Health Regen: {HealthRegen:F2} (Base: {_baseHealthRegen} × {_healthRegenMultiplier:F2})");
        if (useMovementSpeed)
            Debug.Log($"Movement Speed: {MovementSpeed:F2} (Base: {_baseMovementSpeed} × {_movementSpeedMultiplier:F2})");
        if (useDamage)
            Debug.Log($"Damage: {Damage:F2} (Base: {_baseDamage} × {_damageMultiplier:F2})");
    }
#endif
}