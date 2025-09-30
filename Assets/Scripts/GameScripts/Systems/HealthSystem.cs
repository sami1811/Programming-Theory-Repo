using UnityEngine;

public class HealthSystem : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int damagePerHit = 50;
    [SerializeField] private LayerMask damageLayer;

    private int currentHealth;

    private SpawningSystem spawner;
    private PoolingSystem poolManager;

    private void OnEnable()
    {
        currentHealth = maxHealth;

        if (HealthBarManager.Instance != null)
        {
            HealthBarManager.Instance.RegisterHealthSystems(this);
            HealthBarManager.Instance.UpdateHealthText(this, currentHealth);
        }
    }

    private void OnDisable()
    {
        if (HealthBarManager.Instance != null)
        {
            HealthBarManager.Instance.UnregisterHealthSystems(this);
        }
    }

    private void OnDestroy()
    {
        if (HealthBarManager.Instance != null)
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
        if (spawner == null)
        {
            spawner = GetComponentInParent<SpawningSystem>();
            if (spawner != null)
            {
                Debug.Log($"Spawn manager found!");
            }
            else
            {
                Debug.LogError($"Spawn manager is missing!");
            }
        }

        if (poolManager == null)
        {
            poolManager = GetComponentInParent<PoolingSystem>();
            if (poolManager != null)
            {
                Debug.Log($"Pool manager found!");
            }
            else
            {
                Debug.LogError($"Pool manager is missing!");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //layer detection
        if (((1 << other.gameObject.layer) & damageLayer) != 0)
        {
            TakeDamage(damagePerHit);
        }

        /*if (other.CompareTag("Player") && IsAlive())
        {
            TakeDamage(damagePerHit);
        }*/
    }

    public virtual void TakeDamage(int damage)
    {
        if (damage < 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        HealthBarManager.Instance.UpdateHealthText(this, GetCurrentHealth());

        //Debug.Log(gameObject.name + " took " + damage + " now health is " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public virtual bool IsAlive()
    {
        return currentHealth > 0;
    }

    public void Die()
    {
        if (HealthBarManager.Instance != null)
        {
            HealthBarManager.Instance.UnregisterHealthSystems(this);
        }

        currentHealth = maxHealth;
        spawner?.OnObjectReturned(gameObject);
        poolManager?.ReturnToPool(gameObject);

        //Debug.Log(gameObject.name + " is dead!");
    }
}