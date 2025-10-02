using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class PoolingSystem : MonoBehaviour, IPoolable
{
    [Header("Pool Settings")]
    [SerializeField] protected GameObject objectPrefab;
    [SerializeField] protected int initialPoolSize;
    [SerializeField] protected int maxPoolSize;
    [SerializeField] protected int expansionBatchSize;

    [Header("Pool Shrink Settings")]
    [SerializeField] protected bool enableAutoShrink;
    [SerializeField] protected int minPoolSize;
    [SerializeField] protected float idleTimeOut;
    [SerializeField] protected float shrinkCheckInterval;

    // --- Collections ---
    private Queue<GameObject> _availableObjects = new Queue<GameObject>();
    private HashSet<GameObject> _availableSet = new HashSet<GameObject>();
    private readonly List<GameObject> _activeObjects = new List<GameObject>();
    private readonly HashSet<GameObject> _activeSet = new HashSet<GameObject>();

    // --- Memory Tracking ---
    private int _createdCount;
    private int _recycledCount;
    private int _destroyedCount;

    // --- Lazy Cleaning ---
    private int _consecutiveNulls;
    private const int ImmediateCleanThreshold = 5;

    // --- Collection Reuse for Cleaning ---
    private Queue<GameObject> _reusableCleanQueue = new Queue<GameObject>();
    private HashSet<GameObject> _reusableCleanSet = new HashSet<GameObject>();
    private readonly Dictionary<GameObject, float> _idleTimes = new Dictionary<GameObject, float>();

    // --- Periodic Active Cleaning ---
    private readonly List<GameObject> _toDestroy = new List<GameObject>();
    private float _lastActiveCleanTime;
    private const float ActiveCleanInterval = 30f;
    private bool _isAutoShrinkRunning;

    public int InitialPoolSize => initialPoolSize;
    public int MaxPoolSize => maxPoolSize;
    public GameObject ObjectPrefab => objectPrefab;

    protected virtual void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        InitializePoolOnAwake();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void InitializePoolOnAwake()
    {
        if (maxPoolSize < 1)
        {
            
#if UNITY_EDITOR
            Debug.LogWarning("Max pool size too low. Forcing to 1.");
#endif
            
            maxPoolSize = 1;
        }

        if (initialPoolSize > maxPoolSize)
        {
#if UNITY_EDITOR
            Debug.LogWarning("Initial pool size is larger than max. Clamping to max.");
#endif
            initialPoolSize = maxPoolSize;
        }

        if (ReferenceEquals(objectPrefab, null) || !objectPrefab)
        {
#if UNITY_EDITOR
            Debug.LogError("Prefab is null. Cannot initialize pool!");
#endif
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewPoolObject();
        }
#if UNITY_EDITOR
        Debug.Log("[PoolingSystem] Pool initialized successfully!");
#endif
    }

    public GameObject CreateNewPoolObject()
    {
        if (ReferenceEquals(objectPrefab, null) || !objectPrefab)
        {
#if UNITY_EDITOR
            Debug.LogError("Cannot instantiate, prefab is null or destroyed!");
#endif
            return null;
        }

        GameObject instance = Instantiate(objectPrefab, transform);
        instance.SetActive(false);

        _availableObjects.Enqueue(instance);
        _availableSet.Add(instance);

        if (enableAutoShrink)
        {
            _idleTimes[instance] = Time.time;
        }

        _createdCount++;
        return instance;
    }

    private GameObject CreateNewObjectDirect()
    {
        if (ReferenceEquals(objectPrefab, null) || !objectPrefab)
        {
#if UNITY_EDITOR
            Debug.LogError("Cannot instantiate, prefab is null or destroyed!");
#endif
            return null;
        }

        GameObject instance = Instantiate(objectPrefab, transform);
        instance.SetActive(false);
        _createdCount++;
        return instance;
    }

    private void CleanAvailableQueue()
    {
        if (_availableObjects.Count == 0) return;

        _reusableCleanQueue.Clear();
        _reusableCleanSet.Clear();
        int removedCount = 0;

        while (_availableObjects.Count > 0)
        {
            var go = _availableObjects.Dequeue();
            if (!ReferenceEquals(go, null) && go)
            {
                _reusableCleanQueue.Enqueue(go);
                _reusableCleanSet.Add(go);
            }
            else
            {
                _destroyedCount++;
                removedCount++;
                if (TotalObjectCount() - removedCount < maxPoolSize)
                {
                    var replacement = CreateNewObjectDirect();
                    if (!ReferenceEquals(replacement, null) && replacement)
                    {
                        _reusableCleanQueue.Enqueue(replacement);
                        _reusableCleanSet.Add(replacement);
                        if (enableAutoShrink)
                        {
                            _idleTimes[replacement] = Time.time;
                        }
                    }
                }
            }
        }

        (_availableObjects, _reusableCleanQueue) = (_reusableCleanQueue, _availableObjects);
        (_availableSet, _reusableCleanSet) = (_reusableCleanSet, _availableSet);

#if UNITY_EDITOR
        if (removedCount > 0)
        {
            Debug.Log("[PoolingSystem] Cleaned null/destroyed object from pool");
        }
#endif
    }

    protected void StartAutoShrink()
    {
        if (enableAutoShrink && !_isAutoShrinkRunning)
        {
            _isAutoShrinkRunning = true;
            StartCoroutine(AutoShrinkCoroutine());
        }
    }

    private IEnumerator AutoShrinkCoroutine()
    {
        yield return new WaitForSeconds(shrinkCheckInterval);

        if (_availableObjects.Count > minPoolSize)
        {
            _toDestroy.Clear();
            int destroyed = 0;

            foreach (var pair in _idleTimes)
            {
                if (Time.time - pair.Value > idleTimeOut && _availableSet.Contains(pair.Key))
                {
                    _toDestroy.Add(pair.Key);
                }
            }

            int maxToDestroy = _availableObjects.Count - minPoolSize;
            if (_toDestroy.Count > maxToDestroy)
            {
                _toDestroy.RemoveRange(maxToDestroy, _toDestroy.Count - maxToDestroy);
            }

            foreach (var obj in _toDestroy)
            {
                if (!ReferenceEquals(obj, null) && obj && _availableSet.Contains(obj))
                {
                    _availableSet.Remove(obj);
                    _idleTimes.Remove(obj);
                    Object.Destroy(obj);
                    _destroyedCount++;
                    destroyed++;
                }
            }

            if (destroyed > 0)
            {
                var tempQueue = new Queue<GameObject>();
                while (_availableObjects.Count > 0)
                {
                    var obj = _availableObjects.Dequeue();
                    if (_availableSet.Contains(obj))
                    {
                        tempQueue.Enqueue(obj);
                    }
                }
                _availableObjects = tempQueue;
            }
#if UNITY_EDITOR
            if (destroyed > 0)
            {
                Debug.Log("[PoolingSystem] Auto-shrunk pool due to idle timeout.");
            }
#endif
        }

        if (enableAutoShrink)
        {
            _isAutoShrinkRunning = true;
            yield return StartCoroutine(AutoShrinkCoroutine());
        }
        else
        {
            _isAutoShrinkRunning = false;
        }
    }

    public virtual GameObject GetObject()
    {
        if (Time.time - _lastActiveCleanTime > ActiveCleanInterval)
        {
            CleanActiveObjects();
            _lastActiveCleanTime = Time.time;
        }

        if (_consecutiveNulls >= ImmediateCleanThreshold)
        {
            CleanAvailableQueue();
            _consecutiveNulls = 0;
        }

        GameObject instance = null;
        while (_availableObjects.Count > 0)
        {
            instance = _availableObjects.Dequeue();
            bool isValidUnityObject = !ReferenceEquals(instance, null) && instance;

            if (!isValidUnityObject)
            {
                if (!ReferenceEquals(instance, null))
                {
                    _availableSet.Remove(instance);
                    _idleTimes.Remove(instance);
                }
                _destroyedCount++;
                _consecutiveNulls++;
                if (TotalObjectCount() < maxPoolSize)
                {
                    instance = CreateNewObjectDirect();
                    if (!ReferenceEquals(instance, null) && instance)
                    {
                        if (enableAutoShrink)
                        {
                            _idleTimes[instance] = Time.time;
                        }
                        _consecutiveNulls = 0;
                        break;
                    }
                }
                instance = null;
                continue;
            }

            _consecutiveNulls = 0;
            _availableSet.Remove(instance);
            _idleTimes.Remove(instance);

            if (!_activeSet.Contains(instance))
            {
                break;
            }
            instance = null;
        }

        instance = ExpandPool(instance);

        if (ReferenceEquals(instance, null) || !instance)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[PoolingSystem] Pool exhausted or failed to create object.");
#endif
            return null;
        }

        if (_activeSet.Contains(instance))
        {
#if UNITY_EDITOR
            Debug.LogError("[PoolingSystem] Pool returned already active object!");
#endif
            return null;
        }

        OnPoolRetrieve(instance);
        return instance;
    }

    private GameObject ExpandPool(GameObject currentInstance)
    {
        if ((ReferenceEquals(currentInstance, null) || !currentInstance) && TotalObjectCount() < maxPoolSize)
        {
            int toCreate = Mathf.Min(expansionBatchSize, maxPoolSize - TotalObjectCount());
            GameObject firstObject = null;

            for (int i = 0; i < toCreate; i++)
            {
                var obj = CreateNewObjectDirect();
                if (!ReferenceEquals(obj, null) && obj)
                {
                    if (firstObject == null)
                    {
                        firstObject = obj;
                    }
                    else
                    {
                        _availableObjects.Enqueue(obj);
                        _availableSet.Add(obj);
                        if (enableAutoShrink)
                        {
                            _idleTimes[obj] = Time.time;
                        }
                    }
                }
            }
#if UNITY_EDITOR
            Debug.Log("[PoolingSystem] Pool batch expanded.");
#endif

            return firstObject ?? currentInstance;
        }

        return currentInstance;
    }

    public virtual void ReturnToPool(GameObject objectToReturn)
    {
        if (ReferenceEquals(objectToReturn, null) || !objectToReturn)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[PoolingSystem] Tried to return null or destroyed object.");
#endif
            return;
        }

        if (!_activeSet.Contains(objectToReturn))
        {
#if UNITY_EDITOR
            Debug.LogWarning("[PoolingSystem] Tried to return object which is not active.");
#endif
            return;
        }

        OnPoolReturn(objectToReturn);
    }

    public virtual void OnPoolReturn(GameObject objectToReturn)
    {
        _activeObjects.Remove(objectToReturn);
        _activeSet.Remove(objectToReturn);

        if (!ReferenceEquals(objectToReturn, null) && objectToReturn && objectToReturn.activeInHierarchy)
        {
            objectToReturn.SetActive(false);
        }

        if (!_availableSet.Contains(objectToReturn))
        {
            if (!ReferenceEquals(objectToReturn, null) && objectToReturn)
            {
                objectToReturn.transform.SetParent(transform, false);
                objectToReturn.transform.localPosition = Vector3.zero;
                objectToReturn.transform.localRotation = Quaternion.identity;
            }

            _availableObjects.Enqueue(objectToReturn);
            _availableSet.Add(objectToReturn);

            if (enableAutoShrink)
            {
                if (!ReferenceEquals(objectToReturn, null) && objectToReturn)
                {
                    _idleTimes[objectToReturn] = Time.time;
                }
            }

            _recycledCount++;
        }
    }

    public virtual void OnPoolRetrieve(GameObject objectToRetrieve)
    {
        _activeObjects.Add(objectToRetrieve);
        _activeSet.Add(objectToRetrieve);

        if (!ReferenceEquals(objectToRetrieve, null) && objectToRetrieve)
        {
            objectToRetrieve.SetActive(true);
        }

        if (enableAutoShrink)
        {
            if (!ReferenceEquals(objectToRetrieve, null) && objectToRetrieve)
            {
                _idleTimes.Remove(objectToRetrieve);
            }
        }
    }

    private void CleanActiveObjects()
    {
        int removedCount = 0;
        for (int i = _activeObjects.Count - 1; i >= 0; i--)
        {
            var obj = _activeObjects[i];
            if (ReferenceEquals(obj, null) || !obj)
            {
                _activeObjects.RemoveAt(i);
                if (!ReferenceEquals(obj, null))
                {
                    _activeSet.Remove(obj);
                    _idleTimes.Remove(obj);
                }
                _destroyedCount++;
                removedCount++;
            }
        }

#if UNITY_EDITOR
        if (removedCount > 0)
        {
            Debug.Log("[PoolingSystem] Cleaned destroyed objects from active list");
        }
#endif
    }

    private void WarmPool(int targetSize)
    {
        targetSize = Mathf.Min(targetSize, maxPoolSize);
        while (TotalObjectCount() < targetSize)
        {
            var obj = CreateNewPoolObject();
            if (ReferenceEquals(obj, null) || !obj)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[PoolingSystem] Failed to create object during pool warming");
#endif
                break;
            }
        }

#if UNITY_EDITOR
        Debug.Log($"[PoolingSystem] {gameObject.name} object pool warmed to {TotalObjectCount()} objects");
#endif
    }

    public void ForceCleanAll()
    {
        CleanAvailableQueue();
        CleanActiveObjects();
        _consecutiveNulls = 0;
        _lastActiveCleanTime = Time.time;
    }

    public int TotalObjectCount() => _availableObjects.Count + _activeObjects.Count;
    public int AvailableObjectCount() => _availableObjects.Count;
    public int ActiveObjectCount() => _activeObjects.Count;

    public int CreatedCount => _createdCount;
    public int RecycledCount => _recycledCount;
    public int DestroyedCount => _destroyedCount;

    public float HitRatio => _createdCount > 0 ? (float)_recycledCount / (_createdCount + _recycledCount) : 0f;
    public float PoolUtilization => maxPoolSize > 0 ? (float)_activeObjects.Count / maxPoolSize : 0f;
    public float PoolHealth => TotalObjectCount() > 0 ? 1f - ((float)_destroyedCount / (_createdCount + _destroyedCount)) : 1f;

#if UNITY_EDITOR
    [ContextMenu("Validate Pool Integrity")]
    public void ValidatePoolIntegrity()
    {
        ForceCleanAll();
        Debug.Log($"[PoolingSystem] Pool Validation Complete:\n" +
                  $"Available: {_availableObjects.Count}, Active: {_activeObjects.Count}\n" +
                  $"Created: {_createdCount}, Recycled: {_recycledCount}, Destroyed: {_destroyedCount}\n" +
                  $"Hit Ratio: {HitRatio:P1}, Utilization: {PoolUtilization:P1}, Health: {PoolHealth:P1}");
    }

    [ContextMenu("Warm Pool to Max")]
    public void WarmPoolToMax()
    {
        WarmPool(maxPoolSize);
    }

    [ContextMenu("Show Pool Stats")]
    public void ShowPoolStats()
    {
        Debug.Log($"=== POOL STATISTICS ===\n" +
                  $"Total Objects: {TotalObjectCount()}\n" +
                  $"Available: {AvailableObjectCount()}\n" +
                  $"Active: {ActiveObjectCount()}\n" +
                  $"Created: {CreatedCount}\n" +
                  $"Recycled: {RecycledCount}\n" +
                  $"Destroyed: {DestroyedCount}\n" +
                  $"Hit Ratio: {HitRatio:P2}\n" +
                  $"Utilization: {PoolUtilization:P2}\n" +
                  $"Health: {PoolHealth:P2}");
    }
#endif
}