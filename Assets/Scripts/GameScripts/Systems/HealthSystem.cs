using UnityEngine;

public class HealthSystem : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int damagePerHit = 50;
    [SerializeField] private LayerMask damageLayer;

    private int currentHealth;

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
        if (poolManager == null)
        {
            poolManager = GetComponentInParent<PoolingSystem>();
            if (poolManager != null)
                Debug.Log("Pool Manager found.");
            else
                Debug.LogError("Object pool manager is missing!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & damageLayer) != 0)
        {
            TakeDamage(damagePerHit);
        }
    }

    public virtual void TakeDamage(int damage)
    {
        if (damage < 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        HealthBarManager.Instance.UpdateHealthText(this, GetCurrentHealth());

        Debug.Log(gameObject.name + " took " + damage + " now health is " + currentHealth);

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
        Debug.Log(gameObject.name + " is dead!");

        if (HealthBarManager.Instance != null)
        {
            HealthBarManager.Instance.UnregisterHealthSystems(this);
        }

        currentHealth = maxHealth;
        poolManager?.ReturnToPool(gameObject);
    }
}