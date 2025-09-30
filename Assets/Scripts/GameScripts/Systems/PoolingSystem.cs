using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class PoolingSystem : MonoBehaviour, IPoolable
{
    [Header("Pool Settings")]
    [SerializeField] protected GameObject objectPrefab;
    [SerializeField] protected int initialPoolSize = 5;
    [SerializeField] protected int maxPoolSize = 20;
    [SerializeField] protected int expansionBatchSize = 5;

    [Header("Pool Shrink Settings")]
    [SerializeField] protected bool enableAutoShrink;
    [SerializeField] protected int minPoolSize = 7;
    [SerializeField] protected float idleTimeOut = 60f;
    [SerializeField] protected float shrinkCheckInterval = 30f;

    // --- Collections ---
    protected Queue<GameObject> availableObjects = new Queue<GameObject>();
    protected HashSet<GameObject> availableSet = new HashSet<GameObject>();
    protected List<GameObject> activeObjects = new List<GameObject>();
    protected HashSet<GameObject> activeSet = new HashSet<GameObject>();

    // --- Memory Tracking ---
    private int createdCount = 0;
    private int recycledCount = 0;
    private int destroyedCount = 0;

    // --- Lazy Cleaning ---
    private int consecutiveNulls = 0;
    private const int IMMEDIATE_CLEAN_THRESHOLD = 3;

    // --- Collection Reuse for Cleaning ---
    private Queue<GameObject> reusableCleanQueue = new Queue<GameObject>();
    private HashSet<GameObject> reusableCleanSet = new HashSet<GameObject>();
    private Dictionary<GameObject, float> idleTimes = new Dictionary<GameObject, float>();

    // --- Periodic Active Cleaning ---
    private List<GameObject> toDestroy = new List<GameObject>();
    private float lastActiveCleanTime;
    private const float ACTIVE_CLEAN_INTERVAL = 30f;
    private bool isAutoShrinkRunning = false;

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

    protected void InitializePoolOnAwake()
    {
        if (maxPoolSize < 1)
        {
            Debug.LogWarning("Max pool size too low. Forcing to 1.");
            maxPoolSize = 1;
        }

        if (initialPoolSize > maxPoolSize)
        {
            Debug.LogWarning("Initial pool size is larger than max. Clamping to max.");
            initialPoolSize = maxPoolSize;
        }

        if (ReferenceEquals(objectPrefab, null) || !objectPrefab)
        {
            Debug.LogError("Prefab is null. Cannot initialize pool!");
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewPoolObject();
        }

        Debug.Log("[PoolingSystem] Pool initialized successfully!");
    }

    public GameObject CreateNewPoolObject()
    {
        if (ReferenceEquals(objectPrefab, null) || !objectPrefab)
        {
            Debug.LogError("Cannot instantiate, prefab is null or destroyed!");
            return null;
        }

        GameObject instance = Instantiate(objectPrefab, transform);
        instance.SetActive(false);

        availableObjects.Enqueue(instance);
        availableSet.Add(instance);

        if (enableAutoShrink)
        {
            idleTimes[instance] = Time.time;
        }

        createdCount++;
        return instance;
    }

    private GameObject CreateNewObjectDirect()
    {
        if (ReferenceEquals(objectPrefab, null) || !objectPrefab)
        {
            Debug.LogError("Cannot instantiate, prefab is null or destroyed!");
            return null;
        }

        GameObject instance = Instantiate(objectPrefab, transform);
        instance.SetActive(false);
        createdCount++;
        return instance;
    }

    protected void CleanAvailableQueue()
    {
        if (availableObjects.Count == 0) return;

        reusableCleanQueue.Clear();
        reusableCleanSet.Clear();
        int removedCount = 0;

        while (availableObjects.Count > 0)
        {
            var go = availableObjects.Dequeue();
            if (!ReferenceEquals(go, null) && go)
            {
                reusableCleanQueue.Enqueue(go);
                reusableCleanSet.Add(go);
            }
            else
            {
                destroyedCount++;
                removedCount++;
                if (TotalObjectCount() - removedCount < maxPoolSize)
                {
                    var replacement = CreateNewObjectDirect();
                    if (!ReferenceEquals(replacement, null) && replacement)
                    {
                        reusableCleanQueue.Enqueue(replacement);
                        reusableCleanSet.Add(replacement);
                        if (enableAutoShrink)
                        {
                            idleTimes[replacement] = Time.time;
                        }
                    }
                }
            }
        }

        (availableObjects, reusableCleanQueue) = (reusableCleanQueue, availableObjects);
        (availableSet, reusableCleanSet) = (reusableCleanSet, availableSet);

#if UNITY_EDITOR
        if (removedCount > 0)
        {
            Debug.Log("[PoolingSystem] Cleaned null/destroyed object from pool");
        }
#endif
    }

    protected void StartAutoShrink()
    {
        if (enableAutoShrink && !isAutoShrinkRunning)
        {
            isAutoShrinkRunning = true;
            StartCoroutine(AutoShrinkCoroutine());
        }
    }

    private IEnumerator AutoShrinkCoroutine()
    {
        yield return new WaitForSeconds(shrinkCheckInterval);

        if (availableObjects.Count > minPoolSize)
        {
            toDestroy.Clear();
            int destroyed = 0;

            foreach (var pair in idleTimes)
            {
                if (Time.time - pair.Value > idleTimeOut && availableSet.Contains(pair.Key))
                {
                    toDestroy.Add(pair.Key);
                }
            }

            int maxToDestroy = availableObjects.Count - minPoolSize;
            if (toDestroy.Count > maxToDestroy)
            {
                toDestroy.RemoveRange(maxToDestroy, toDestroy.Count - maxToDestroy);
            }

            foreach (var obj in toDestroy)
            {
                if (!ReferenceEquals(obj, null) && obj && availableSet.Contains(obj))
                {
                    availableSet.Remove(obj);
                    idleTimes.Remove(obj);
                    Object.Destroy(obj);
                    destroyedCount++;
                    destroyed++;
                }
            }

            if (destroyed > 0)
            {
                var tempQueue = new Queue<GameObject>();
                while (availableObjects.Count > 0)
                {
                    var obj = availableObjects.Dequeue();
                    if (availableSet.Contains(obj))
                    {
                        tempQueue.Enqueue(obj);
                    }
                }
                availableObjects = tempQueue;
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
            isAutoShrinkRunning = true;
            yield return StartCoroutine(AutoShrinkCoroutine());
        }
        else
        {
            isAutoShrinkRunning = false;
        }
    }

    public virtual GameObject GetObject()
    {
        if (Time.time - lastActiveCleanTime > ACTIVE_CLEAN_INTERVAL)
        {
            CleanActiveObjects();
            lastActiveCleanTime = Time.time;
        }

        if (consecutiveNulls >= IMMEDIATE_CLEAN_THRESHOLD)
        {
            CleanAvailableQueue();
            consecutiveNulls = 0;
        }

        GameObject instance = null;
        while (availableObjects.Count > 0)
        {
            instance = availableObjects.Dequeue();
            bool isValidUnityObject = !ReferenceEquals(instance, null) && instance;

            if (!isValidUnityObject)
            {
                if (!ReferenceEquals(instance, null))
                {
                    availableSet.Remove(instance);
                    idleTimes.Remove(instance);
                }
                destroyedCount++;
                consecutiveNulls++;
                if (TotalObjectCount() < maxPoolSize)
                {
                    instance = CreateNewObjectDirect();
                    if (!ReferenceEquals(instance, null) && instance)
                    {
                        if (enableAutoShrink)
                        {
                            idleTimes[instance] = Time.time;
                        }
                        consecutiveNulls = 0;
                        break;
                    }
                }
                instance = null;
                continue;
            }

            consecutiveNulls = 0;
            availableSet.Remove(instance);
            idleTimes.Remove(instance);

            if (!activeSet.Contains(instance))
            {
                break;
            }
            instance = null;
        }

        instance = ExpandPool(instance);

        if (ReferenceEquals(instance, null) || !instance)
        {
            Debug.LogWarning("[PoolingSystem] Pool exhausted or failed to create object.");
            return null;
        }

        if (activeSet.Contains(instance))
        {
            Debug.LogError("[PoolingSystem] Pool returned already active object!");
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
                        availableObjects.Enqueue(obj);
                        availableSet.Add(obj);
                        if (enableAutoShrink)
                        {
                            idleTimes[obj] = Time.time;
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
            Debug.LogWarning("[PoolingSystem] Tried to return null or destroyed object.");
            return;
        }

        if (!activeSet.Contains(objectToReturn))
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
        activeObjects.Remove(objectToReturn);
        activeSet.Remove(objectToReturn);

        if (!ReferenceEquals(objectToReturn, null) && objectToReturn && objectToReturn.activeInHierarchy)
        {
            objectToReturn.SetActive(false);
        }

        if (!availableSet.Contains(objectToReturn))
        {
            if (!ReferenceEquals(objectToReturn, null) && objectToReturn)
            {
                objectToReturn.transform.SetParent(transform, false);
                objectToReturn.transform.localPosition = Vector3.zero;
                objectToReturn.transform.localRotation = Quaternion.identity;
            }

            availableObjects.Enqueue(objectToReturn);
            availableSet.Add(objectToReturn);

            if (enableAutoShrink)
            {
                idleTimes[objectToReturn] = Time.time;
            }

            recycledCount++;
        }
    }

    public virtual void OnPoolRetrieve(GameObject objectToRetrieve)
    {
        activeObjects.Add(objectToRetrieve);
        activeSet.Add(objectToRetrieve);

        if (!ReferenceEquals(objectToRetrieve, null) && objectToRetrieve)
        {
            objectToRetrieve.SetActive(true);
        }

        if (enableAutoShrink)
        {
            idleTimes.Remove(objectToRetrieve);
        }
    }

    public void CleanActiveObjects()
    {
        int removedCount = 0;
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            var obj = activeObjects[i];
            if (ReferenceEquals(obj, null) || !obj)
            {
                activeObjects.RemoveAt(i);
                if (!ReferenceEquals(obj, null))
                {
                    activeSet.Remove(obj);
                    idleTimes.Remove(obj);
                }
                destroyedCount++;
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

    public void WarmPool(int targetSize)
    {
        targetSize = Mathf.Min(targetSize, maxPoolSize);
        while (TotalObjectCount() < targetSize)
        {
            var obj = CreateNewPoolObject();
            if (ReferenceEquals(obj, null) || !obj)
            {
                Debug.LogWarning("[PoolingSystem] Failed to create object during pool warming");
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
        consecutiveNulls = 0;
        lastActiveCleanTime = Time.time;
    }

    public int TotalObjectCount() => availableObjects.Count + activeObjects.Count;
    public int AvailableObjectCount() => availableObjects.Count;
    public int ActiveObjectCount() => activeObjects.Count;

    public int CreatedCount => createdCount;
    public int RecycledCount => recycledCount;
    public int DestroyedCount => destroyedCount;

    public float HitRatio => createdCount > 0 ? (float)recycledCount / (createdCount + recycledCount) : 0f;
    public float PoolUtilization => maxPoolSize > 0 ? (float)activeObjects.Count / maxPoolSize : 0f;
    public float PoolHealth => TotalObjectCount() > 0 ? 1f - ((float)destroyedCount / (createdCount + destroyedCount)) : 1f;

#if UNITY_EDITOR
    [ContextMenu("Validate Pool Integrity")]
    public void ValidatePoolIntegrity()
    {
        ForceCleanAll();
        Debug.Log($"[PoolingSystem] Pool Validation Complete:\n" +
                  $"Available: {availableObjects.Count}, Active: {activeObjects.Count}\n" +
                  $"Created: {createdCount}, Recycled: {recycledCount}, Destroyed: {destroyedCount}\n" +
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