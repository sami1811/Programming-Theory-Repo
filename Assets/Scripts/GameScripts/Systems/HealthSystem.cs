using UnityEngine;

public class HealthSystem : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth;
    [SerializeField] private int damagePerHit;
    [SerializeField] private LayerMask damageLayer;
    [SerializeField] private bool spawningRequired;

    private int _currentHealth;

    private SpawningSystem _spawner;

    private void OnEnable()
    {
        _currentHealth = maxHealth;

        if (HealthBarManager.Instance)
        {
            HealthBarManager.Instance.RegisterHealthSystems(this);
            HealthBarManager.Instance.UpdateHealthText(this, _currentHealth);
        }
    }

    private void OnDisable()
    {
        if (HealthBarManager.Instance)
        {
            HealthBarManager.Instance.UnregisterHealthSystems(this);
        }
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

    private void InitializeStart()
    {
        if(!spawningRequired) return;
        
        if (!_spawner)
        {
            _spawner = GetComponentInParent<SpawningSystem>();
            if (_spawner)
            {
#if UNITY_EDITOR
                Debug.Log($"Spawn manager found!");
#endif
            }
            else
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
            TakeDamage(damagePerHit);
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (damage < 0)
            return;

        _currentHealth = Mathf.Max(0, _currentHealth - damage);

        HealthBarManager.Instance.UpdateHealthText(this, GetCurrentHealth());
        
        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    public int GetCurrentHealth()
    {
        return _currentHealth;
    }

    public bool IsAlive()
    {
        return _currentHealth > 0;
    }

    private void Die()
    {
        if (HealthBarManager.Instance)
        {
            HealthBarManager.Instance.UnregisterHealthSystems(this);
        }

        _currentHealth = maxHealth;

        if (spawningRequired)
        {
            _spawner?.ReturnObjectToPool(gameObject);
        }
    }
}