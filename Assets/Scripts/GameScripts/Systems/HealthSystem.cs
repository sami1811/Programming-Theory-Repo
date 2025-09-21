using UnityEngine;

public class HealthSystem : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] protected int maxHealth = 100;
    [SerializeField] protected int damagePerHit = 50;

    protected int currentHealth;

    private PoolingSystem poolManager;

    private void OnEnable()
    {
        RestoreOnRetrieve();
        
    }

    private void Start()
    {
        AssignOnStart();
    }

    private void AssignOnStart()
    {
        if(poolManager == null)
        {
            poolManager = GetComponentInParent<PoolingSystem>();
            Debug.Log("Pool Manager found.");
        }
        else
        {
            Debug.LogError("Object pool manager is missing!");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && IsAlive())
        {
            TakeDamage(damagePerHit);
        }
    }

    public void RestoreOnRetrieve()
    {
        currentHealth = maxHealth;
    }

    public virtual void TakeDamage(int damage)
    {
        if (damage < 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

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
        poolManager.ReturnObject(gameObject);
    }
}