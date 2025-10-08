using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HealthBarManager : PoolingSystem
{
    public static HealthBarManager Instance { get; private set; }

    [Header("Canvas Settings")]
    [SerializeField] private Vector3 healthBarScale;

    [Header("Player Settings")]
    [SerializeField] private Transform playerTransform;

    private readonly Dictionary<HealthSystem, GameObject> _activeCanvases = new Dictionary<HealthSystem, GameObject>();
    private readonly Dictionary<HealthSystem, TMP_Text> _healthText = new Dictionary<HealthSystem, TMP_Text>();
    private readonly Dictionary<HealthSystem, Vector3> _healthBarOffset = new Dictionary<HealthSystem, Vector3>();
    private readonly Dictionary<HealthSystem, float> _showAtDistance = new Dictionary<HealthSystem, float>();
    private readonly HashSet<HealthSystem> _registeredHealthSystems = new HashSet<HealthSystem>();

    private Camera _mainCamera;
    private const float UpdateInterval = 0.1f;
    private float _nextUpdateTime;

    private SpawningSystem _spawner;

    protected override void Awake()
    {
        InitializeAwake();
    }

    private void InitializeAwake()
    {
        base.Awake();

        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _mainCamera = Camera.main;

        if (!playerTransform)
        {
            playerTransform = _mainCamera?.transform;
        }
    }

    private void Update()
    {
        StartAutoShrink();

        if(!playerTransform) return;

        if (Time.time >= _nextUpdateTime)
        {
            UpdateCanvasStates();
            _nextUpdateTime += UpdateInterval;
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void UpdateCanvasStates()
    {
        // Create a temp list to avoid modifying collection during iteration
        List<HealthSystem> toUnregister = new List<HealthSystem>();

        foreach (HealthSystem hs in _registeredHealthSystems)
        {
            if (!hs)
            {
                toUnregister.Add(hs);
                continue;
            }

            float showDistance = _showAtDistance.TryGetValue(hs, out float distance) ? distance : 10f;
            float showDistanceSqr = showDistance * showDistance;
            float distanceSqr = (playerTransform.position - hs.transform.position).sqrMagnitude;
            bool withinRange = distanceSqr <= showDistanceSqr;

            if (withinRange)
            {
                // Ensure canvas exists
                if (!_activeCanvases.TryGetValue(hs, out GameObject canvasObj) || !canvasObj)
                {
                    AssignCanvas(hs);
                    canvasObj = _activeCanvases[hs];
                }

                // Update position + rotation
                if (canvasObj)
                {
                    Vector3 offset = _healthBarOffset.TryGetValue(hs, out Vector3 healthBarOffset ) ? healthBarOffset : Vector3.up;
                    canvasObj.transform.position = hs.transform.position + healthBarOffset;

                    if (_mainCamera)
                    {
                        Vector3 lookDir = canvasObj.transform.position - _mainCamera.transform.position;
                        canvasObj.transform.rotation = Quaternion.LookRotation(lookDir);
                    }
                }
            }
            else
            {
                // Hide if active but too far
                if (_activeCanvases.ContainsKey(hs))
                {
                    ReturnCanvas(hs);
                }
            }
        }

        // Cleanup null health systems
        foreach (var dead in toUnregister)
        {
            _registeredHealthSystems.Remove(dead);
            _healthBarOffset.Remove(dead);
            _showAtDistance.Remove(dead);
            _healthText.Remove(dead);
            _activeCanvases.Remove(dead); 
        }
    }

    private void AssignCanvas(HealthSystem hs)
    {
        GameObject canvasObj = GetObject();

        if(!canvasObj)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"No canvas found!");
#endif
            return;
        }

        Vector3 offset = _healthBarOffset.TryGetValue(hs, out Vector3 healthBarOffset ) ? healthBarOffset : Vector3.up;
        UpdateCanvasPosition(canvasObj, hs.transform.position, healthBarOffset);

        TMP_Text txt = canvasObj.GetComponentInChildren<TMP_Text>();

        if(txt)
        {
            txt.text = hs.GetCurrentHealth().ToString();
            _healthText[hs] = txt;
        }

        _activeCanvases[hs] = canvasObj;
    }

    private void UpdateCanvasPosition(GameObject gameObj, Vector3 setPosition, Vector3 yOffset)
    {
        if(gameObj)
        {
            gameObj.transform.position = setPosition + yOffset;
        }
    }

    public void RegisterHealthSystems(HealthSystem hs, Vector3 offset, float showAtDistance)
    {
        if (hs)
        {
            _registeredHealthSystems.Add(hs);
            _healthBarOffset[hs]  = offset;
            _showAtDistance[hs] = showAtDistance;
            //Debug.Log($"[HealthBarManager] Registered health system {hs.gameObject.name}");
        }
    }

    public void UnregisterHealthSystems(HealthSystem hs)
    {
        if (!hs) return;

        _registeredHealthSystems.Remove(hs);
        _healthBarOffset.Remove(hs);
        _showAtDistance.Remove(hs);

        if(_activeCanvases.ContainsKey(hs))
        {
            ReturnCanvas(hs);
        }
    }

    private void ReturnCanvas(HealthSystem hs)
    {
        if (_activeCanvases.TryGetValue(hs, out GameObject canvasObj))
        {
            _healthText.Remove(hs);
            _activeCanvases.Remove(hs);
            OnPoolReturn(canvasObj);
        }
    }

    public void UpdateHealthText(HealthSystem hs, float currentHealth)
    {
        if (hs && _healthText.TryGetValue(hs, out TMP_Text txt) && txt)
        {
            txt.text = $"{currentHealth}";
        }
    }

    public override void OnPoolRetrieve(GameObject objectToRetrieve)
    {
        base.OnPoolRetrieve(objectToRetrieve);

        Canvas canvas = objectToRetrieve.GetComponent<Canvas>();

        if (canvas)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = _mainCamera;
            objectToRetrieve.transform.localScale = healthBarScale;
        }
    }
}
