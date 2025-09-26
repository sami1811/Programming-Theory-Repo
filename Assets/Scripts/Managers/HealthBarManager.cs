using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HealthBarManager : PoolingSystem
{
    public static HealthBarManager Instance { get; private set; }

    [Header("Canvas Settings")]
    [SerializeField] private Vector3 healthBarOffset;
    [SerializeField] private Vector3 healthBarScale;
    [SerializeField] private float showAtDistance = 5f;

    [Header("Player Settings")]
    [SerializeField] private Transform playerTransform;

    private Dictionary<HealthSystem, GameObject> activeCanvases = new Dictionary<HealthSystem, GameObject>();
    private Dictionary<HealthSystem, TMP_Text> healthText = new Dictionary<HealthSystem, TMP_Text>();
    private HashSet<HealthSystem> registeredHealthSystems = new HashSet<HealthSystem>();

    private Camera mainCamera;
    private float updateInterval = 0.1f;
    private float nextUpdateTime = 0f;

    protected override void Awake()
    {
        InitializeAwake();
    }

    private void InitializeAwake()
    {
        base.Awake();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        mainCamera = Camera.main;

        if (playerTransform == null)
        {
            playerTransform = mainCamera?.transform;
        }
    }

    private void Update()
    {
        StartAutoShrink();

        if(playerTransform == null) return;

        if (Time.time >= nextUpdateTime)
        {
            UpdateCanvasStates();
            nextUpdateTime += updateInterval;
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void UpdateCanvasStates()
    {
        float showDistanceSqr = showAtDistance * showAtDistance;

        // Create a temp list to avoid modifying collection during iteration
        List<HealthSystem> toUnregister = new List<HealthSystem>();

        foreach (HealthSystem hs in registeredHealthSystems)
        {
            if (hs == null)
            {
                toUnregister.Add(hs);
                continue;
            }

            float distanceSqr = (playerTransform.position - hs.transform.position).sqrMagnitude;
            bool withinRange = distanceSqr <= showDistanceSqr;

            if (withinRange)
            {
                // Ensure canvas exists
                if (!activeCanvases.TryGetValue(hs, out GameObject canvasObj) || canvasObj == null)
                {
                    AssignCanvas(hs);
                    canvasObj = activeCanvases[hs];
                }

                // Update position + rotation
                if (canvasObj != null)
                {
                    canvasObj.transform.position = hs.transform.position + healthBarOffset;

                    if (mainCamera != null)
                    {
                        Vector3 lookDir = canvasObj.transform.position - mainCamera.transform.position;
                        canvasObj.transform.rotation = Quaternion.LookRotation(lookDir);
                    }
                }
            }
            else
            {
                // Hide if active but too far
                if (activeCanvases.ContainsKey(hs))
                {
                    ReturnCanvas(hs);
                }
            }
        }

        // Cleanup null health systems
        foreach (var dead in toUnregister)
        {
            registeredHealthSystems.Remove(dead);
        }
    }

    private void AssignCanvas(HealthSystem hs)
    {
        GameObject canvasObj = GetObject();

        if(canvasObj == null)
        {
            Debug.LogWarning($"No canvas found!");
            return;
        }

        canvasObj.transform.position = hs.transform.position + healthBarOffset;

        TMP_Text txt = canvasObj.GetComponentInChildren<TMP_Text>();

        if(txt != null)
        {
            txt.text = hs.GetCurrentHealth().ToString();
            healthText[hs] = txt;
        }

        activeCanvases[hs] = canvasObj;
    }

    public void RegisterHealthSystems(HealthSystem hs)
    {
        if (hs != null && !registeredHealthSystems.Contains(hs))
        {
            registeredHealthSystems.Add(hs);
            Debug.Log($"[HealthBarManager] Registered health system {hs.gameObject.name}");
        }
    }

    public void UnregisterHealthSystems(HealthSystem hs)
    {
        if (hs == null) return;

        registeredHealthSystems.Remove(hs);

        if(activeCanvases.ContainsKey(hs))
        {
            ReturnCanvas(hs);
        }
    }

    private void ReturnCanvas(HealthSystem hs)
    {
        if (activeCanvases.TryGetValue(hs, out GameObject canvasObj))
        {
            healthText.Remove(hs);
            activeCanvases.Remove(hs);
            OnPoolReturn(canvasObj);
        }
    }

    public void UpdateHealthText(HealthSystem hs, int currentHealth)
    {
        if (hs != null && healthText.TryGetValue(hs, out TMP_Text txt) && txt != null)
        {
            txt.text = currentHealth.ToString();
        }
    }

    public override void OnPoolRetrieve(GameObject objectToRetrieve)
    {
        base.OnPoolRetrieve(objectToRetrieve);

        Canvas canvas = objectToRetrieve.GetComponent<Canvas>();

        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCamera;
            objectToRetrieve.transform.localScale = healthBarScale;
        }
    }
}
