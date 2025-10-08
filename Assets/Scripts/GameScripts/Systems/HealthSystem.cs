using UnityEngine;
using UnityEngine.Serialization;

public class HealthSystem : MonoBehaviour, IDamageable
{
    [FormerlySerializedAs("playerStatsSystem")]
    [Header("Other Settings")]
    [SerializeField] private LayerMask damageLayer;
    [SerializeField] private bool spawningRequired;
    
    [Header("Regeneration Settings")]
    [SerializeField] private bool shouldRegenHealth;
    [SerializeField] private float regenInterval; // Regen every 1 second
    [SerializeField] private int regenAmountPerTick ; // Base regen amount
    
    [Header("Enemy Health Settings")]
    [SerializeField] private bool isEnemy;
    [SerializeField] private bool increaseHealthOverTime;
    [SerializeField] private float baseEnemyHealth;
    
    [Header("Tree Health Settings")]
    [SerializeField] private bool isTree;
    [SerializeField] private float treeHealth;
    
    [Header("Health Bar Settings")]
    [SerializeField] private float showAtDistance;
    [SerializeField] private Vector3 healthBarOffset;
    
    
    private const float MaxHealth = 2500f;
    private const float HealthMultiplier = 0.02f;
    
    private float _treeHealth;
    private float _currentHealth;
    private float _damagePerHit;
    private float _regenTimer;
    private float _regenEndTime;
    
    private bool _isRegenActive;
    
    public float CurrentHealth => _currentHealth;
    
    private SpawningSystem _spawner;
    
    private void OnEnable()
    {
        if (isEnemy)
        {
            _currentHealth = baseEnemyHealth;
        }
        else if (isTree)
        {
            _currentHealth = treeHealth;
        }
        else
        {
            AbilityManager.Instance?.onUpgradeApplied.AddListener(OnRegenUpgrade);
            _currentHealth = MaxHealth;
        }

        if (HealthBarManager.Instance)
        {
            HealthBarManager.Instance.RegisterHealthSystems(this, healthBarOffset, showAtDistance);
            HealthBarManager.Instance.UpdateHealthText(this, _currentHealth);
        }
    }

    private void OnDisable()
    {
        if (HealthBarManager.Instance)
        {
            HealthBarManager.Instance.UnregisterHealthSystems(this);
        }
        
        AbilityManager.Instance.onUpgradeApplied.RemoveListener(OnRegenUpgrade);
    }

    private void OnDestroy()
    {
        if (HealthBarManager.Instance)
        {
            HealthBarManager.Instance.UnregisterHealthSystems(this);
        }
    }

    private void Start()
    {
        InitializeStart();
    }

    private void Update()
    {
        if (StatsSystem.Instance)
        {
            _damagePerHit = Mathf.RoundToInt( StatsSystem.Instance.Damage);
        }

        if (shouldRegenHealth && _isRegenActive && Time.time < _regenEndTime)
        {
            _regenTimer += Time.deltaTime;
            if (_regenTimer >= regenInterval)
            {
                RegenerateHealth();
                _regenTimer = 0f;
            }
        }
        else if (_isRegenActive && Time.time >= _regenEndTime)
        {
            _isRegenActive = false;
            shouldRegenHealth = false;
        }
    }

    private void RegenerateHealth()
    {
        if (_currentHealth >= MaxHealth)
            return;
        
        // Apply regen multiplier
        //float regenMultiplier = StatsSystem.Instance.HealthRegen;
        int regenAmount = Mathf.RoundToInt(StatsSystem.Instance.HealthRegen);
        
        _currentHealth = Mathf.Min(_currentHealth + regenAmount, MaxHealth);
        
        HealthBarManager.Instance?.UpdateHealthText(this, _currentHealth);
    }
    
    private void OnRegenUpgrade(UpgradeData upgrade, float multiplier)
    {
        if (upgrade.upgradeType == UpgradeType.HealthRegen && multiplier > 1f)
        {
            _isRegenActive = true;
            _regenEndTime = Time.time + upgrade.regenDuration;
            shouldRegenHealth = true;
        }
    }
    
    private void InitializeStart()
    {
        if (isEnemy || isTree)
        {
            if(!spawningRequired)
                spawningRequired = true;
        
            _spawner = GetComponentInParent<SpawningSystem>();
        
            if (!_spawner)
            {
#if UNITY_EDITOR
                Debug.LogError($"Spawn manager is missing!");
#endif
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Cube"))
        {
            _currentHealth = 0;
            Die();
        }
        
        if (((1 << other.gameObject.layer) & damageLayer) != 0)
        {
            TakeDamage(_damagePerHit);
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (damage < 0)
            return;

        _currentHealth = Mathf.Max(0, _currentHealth - damage);

        HealthBarManager.Instance?.UpdateHealthText(this, GetCurrentHealth());
        
        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    public float GetCurrentHealth()
    {
        return _currentHealth;
    }

    public bool IsAlive()
    {
        return _currentHealth > 0;
    }

    private void Die()
    {
        if (isEnemy || isTree)
        {
            if (spawningRequired)
            {
                _spawner?.ReturnObjectToPool(gameObject);
            }
        }
        
        if (increaseHealthOverTime)
        {
            const float temp = HealthMultiplier * 100f;
            baseEnemyHealth += (int) temp;
        }
    }
}