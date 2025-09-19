using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class HealthSystem : MonoBehaviour, IDamageable
{
    [Header("Settings")]
    [SerializeField] protected int maxHealth = 100;
    [SerializeField] protected int damagePerHit = 50;
    [SerializeField] protected TMP_Text healthText;
    [SerializeField] protected Canvas healthCanvas;

    protected int currentHealth;

    private RectTransform canvasTransform;
    private PoolingSystem parentPool;
    private Camera mainCamera;
    private Vector3 healthBarOffset = new Vector3(0, 4, 0);
    private Vector3 healthBarScale = new Vector3(0.02f, 0.02f, 0.02f);
    
    protected virtual void Awake()
    {
        InitializeAwake();
    }

    protected virtual void Start()
    {
        InitializeStart();
    }

    protected virtual void LateUpdate()
    {
        if (healthCanvas != null && healthCanvas.gameObject.activeInHierarchy)
        {
            HealthFacesCamera();
        }
    }

    private void InitializeStart()
    {
        if (healthText == null)
        {
            Debug.LogWarning("Health UI text is not found. Assign UI text in inspector.");
        }

        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning("No Collider is attached to " + gameObject.name);
        }
    }

    private void InitializeAwake()
    {
        if (healthCanvas == null)
        {
            Debug.LogWarning("Assign health canvas in inspector on " + gameObject.name + " script");
        }
        else
        {
            canvasTransform = healthCanvas.GetComponent<RectTransform>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        currentHealth = maxHealth;

        UpdateHealthUI();
    }

    private void OnEnable()
    {
        HealthCanvasSettings();
    }

    public void InitializeForPool(PoolingSystem pool)
    {
        parentPool = pool;
        Debug.Log($"{gameObject.name} is initialized for pooling.");
    }

    protected virtual void OnPoolReturn()
    {
        if (parentPool != null)
        {
            parentPool.ReturnObject(this.gameObject);
            gameObject.SetActive(false);
            Debug.Log($"{gameObject.name} is returned to pool sucessully.");
        }
    }

    public void OnPoolRetrieve()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        Debug.Log($"{gameObject.name} was retrieved sucessfully with full health.");
    }

    public virtual void TakeDamage(int damage)
    {
        if(damage < 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        UpdateHealthUI();

        Debug.Log(gameObject.name + " took " + damage + " now health is "  + currentHealth);

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

    private void Die()
    {
        Debug.Log(gameObject.name + " is dead!");
        OnPoolReturn();
    }

    protected void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}";
        }
    }

    private void HealthCanvasSettings()
    {
        if (healthCanvas != null)
        {
            healthCanvas.renderMode = RenderMode.WorldSpace;
            canvasTransform.position = transform.position + healthBarOffset;   
            canvasTransform.localScale = healthBarScale;
        }
    }

    private void HealthFacesCamera()
    {
        if (mainCamera != null)
        {
            canvasTransform.rotation = quaternion.LookRotation(mainCamera.transform.forward, mainCamera.transform.up);
        }
    }
}