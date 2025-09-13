using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class HealthSystem : MonoBehaviour, IDamageable
{
    [SerializeField] protected int maxHealth = 100;
    [SerializeField] protected int damagePerHit = 50;
    [SerializeField] protected TMP_Text healthText;
    [SerializeField] protected Canvas healthCanvas;

    private int currentHealth;
    private RectTransform canvasTransform;
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

        Invoke(nameof(HealthCanvasSettings), 0.5f);
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

    protected virtual void Update()
    {
        if (healthCanvas != null && healthCanvas.gameObject.activeInHierarchy)
        {
            HealthFacesCamera();
        }
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
        Destroy(gameObject);
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