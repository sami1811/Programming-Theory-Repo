using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class HealthBarManager : PoolingSystem
{
    public static HealthBarManager Instance { get; private set; }

    [Header("Canvas Scale and Position")]
    [SerializeField] private Vector3 healthBarOffset;
    [SerializeField] private Vector3 healthBarScale;

    [Header("Canvas Visibality Settings")]
    [SerializeField] private float showAtDistance;

    // Player tracking for distance calculations
    [Header("Player Settings")]
    [SerializeField] private Transform playerTransform;

    // Registration and tracking
    private Dictionary<HealthSystem, GameObject> activeCanvases = new Dictionary<HealthSystem, GameObject>();
    private HashSet<HealthSystem> registeredHealthSystems = new HashSet<HealthSystem>();

    private Canvas healthCanvas;
    private Camera mainCamera;
    private RectTransform canvasTransform;

    protected override void Awake()
    {
        InitializeAwake();
    }

    private void Update()
    {
        if(playerTransform == null)
        {
            return;
        }

        UpdateCanvasVisibility();
        UpdateActiveCanvasPositions();
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

        if(mainCamera == null)
        {
            mainCamera = Camera.main;

            if(mainCamera == null)
            {
                Debug.LogError("$Main camera is not found!");
            }
        }

        if (playerTransform == null)
        {
            playerTransform = Camera.main?.transform;

            if (playerTransform == null)
            {
                Debug.LogError($"Player transform is not assigned in inspector!");
            }
        }

        if (healthCanvas == null)
        {
            healthCanvas = objectPrefab?.GetComponentInChildren<Canvas>();

            if (healthCanvas == null)
            {
                Debug.LogError($"Object prefab is missing canvas component!");
            }
        }

        if (canvasTransform == null)
        {
            canvasTransform = objectPrefab?.GetComponentInChildren<RectTransform>();

            if (canvasTransform == null)
            {
                Debug.LogError($"Object prefab is missing rect transform component!");
            }
        }
    }

    private void UpdateCanvasVisibility()
    {
        List<HealthSystem> hsToProcess = new List<HealthSystem>(registeredHealthSystems);

        foreach(HealthSystem hs in hsToProcess)
        {
            if(hs == null)
            {
                registeredHealthSystems.Remove(hs);
                continue;
            }

            bool shouldShowCanvas = ShouldShowCanvas(hs);
            bool hasActiveCanvas = activeCanvases.ContainsKey(hs);

            if (shouldShowCanvas && !hasActiveCanvas)
            {
                AssignCanvas(hs);
            }
            else if (!shouldShowCanvas && hasActiveCanvas)
            {
                ReturnCanvas(hs);
            }
        }
    }

    private bool ShouldShowCanvas(HealthSystem hs)
    {
        if(playerTransform != null)
        {
            float distanceSqr = (playerTransform.position - hs.transform.position).sqrMagnitude;
            float showDistanceSqr = showAtDistance * showAtDistance;

            return distanceSqr <= showDistanceSqr;
        }

        return false;
    }

    private void AssignCanvas(HealthSystem hs)
    {
        GameObject canvas = GetObject();

        if(canvas == null)
        {
            Debug.LogWarning($"No canvas found!");
            return;
        }

        canvas.transform.position = hs.transform.position + healthBarOffset;

        activeCanvases[hs] = canvas;

        Debug.Log($"Canvas is assigned to {hs.gameObject.name}");
    }

    private void UpdateActiveCanvasPositions()
    {
        foreach(var hsc in activeCanvases)
        {
            HealthSystem healthSystem = hsc.Key;
            GameObject canvas = hsc.Value;

            if(healthSystem != null && canvas != null)
            {
                canvas.transform.position = healthSystem.transform.position + healthBarOffset;

                if(mainCamera != null)
                {
                    canvas.transform.rotation = quaternion.LookRotation(mainCamera.transform.forward, mainCamera.transform.up);
                }
            }
        }
    }

    public void RegisterHealthSystems(HealthSystem hs)
    {
        if (hs != null && !registeredHealthSystems.Contains(hs))
        {
            registeredHealthSystems.Add(hs);
            Debug.Log($"Registered health system {hs.gameObject.name}");
        }
    }

    public void UnregisterHealthSystems(HealthSystem hs)
    {
        if (hs != null && registeredHealthSystems.Contains(hs))
        {
            registeredHealthSystems.Remove(hs);

            if (activeCanvases.ContainsKey(hs))
            {
                ReturnCanvas(hs);
            }

            Debug.Log($"Unregistered health system {hs.gameObject.name}");
        }
    }

    public void UpdateHealthText(HealthSystem hs, int currentHealth)
    {
        if(hs != null && registeredHealthSystems.Contains(hs))
        {
            if(activeCanvases.ContainsKey(hs))
            {
                activeCanvases[hs].GetComponentInChildren<TMP_Text>().text = currentHealth.ToString();
            }
        }
    }

    private void ReturnCanvas(HealthSystem hs)
    {
        if (activeCanvases.TryGetValue(hs, out GameObject canvas))
        {
            OnPoolReturn(canvas);
            activeCanvases.Remove(hs);
        }
    }

    public override void OnPoolRetrieve(GameObject objectToRetrieve)
    {
        Canvas canvas = objectToRetrieve.GetComponent<Canvas>();
        RectTransform rectTransform = objectToRetrieve.GetComponent<RectTransform>();

        if (canvas != null && rectTransform != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            rectTransform.localScale = healthBarScale;
            base.OnPoolRetrieve(objectToRetrieve);
        }
        else
        {
            Debug.LogError("Canvas or RectTransform missing on retrieved object!");
        }
    }

    public override void OnPoolReturn(GameObject objectToReturn)
    {
        base.OnPoolReturn(objectToReturn);
    }
}
