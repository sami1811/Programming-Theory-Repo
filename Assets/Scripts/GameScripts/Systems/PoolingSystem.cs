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

    // --- Periodic Active Cleaning ---
    private float lastActiveCleanTime;
    private const float ACTIVE_CLEAN_INTERVAL = 30f; // Clean every 30 seconds

    protected virtual void Awake()
    {
        InitializePool();
    }

    public int InitialPoolSize => initialPoolSize;
    public int MaxPoolSize => maxPoolSize;
    public GameObject ObjectPrefab => objectPrefab;

    /// <summary>
    /// Initialize the pool and pre-create objects.
    /// </summary>
    protected void InitializePool()
    {
        if (maxPoolSize < 1)
        {
            Debug.LogWarning("Max pool size too low. Forcing to 1.");
            maxPoolSize = 1;
        }

        if (initialPoolSize > maxPoolSize)
        {
            Debug.LogWarning("Initial pool size larger than max. Clamping to max.");
            initialPoolSize = maxPoolSize;
        }

        // Unity Object null check - more reliable than == null
        if (ReferenceEquals(objectPrefab, null) || !objectPrefab)
        {
            Debug.LogError("Object prefab is null. Cannot initialize pool!");
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewPoolObject();
        }

        Debug.Log("[PoolingSystem] Pool has been initialized successfully!");
    }

    /// <summary>
    /// Creates a new object for the pool and enqueues it.
    /// </summary>
    public GameObject CreateNewPoolObject()
    {
        // Unity Object null check
        if (ReferenceEquals(objectPrefab, null) || !objectPrefab)
        {
            Debug.LogError("Cannot instantiate: ObjectPrefab is null or destroyed.");
            return null;
        }

        GameObject instance = Instantiate(objectPrefab, transform);
        instance.SetActive(false);

        availableObjects.Enqueue(instance);
        availableSet.Add(instance);

        createdCount++;

#if UNITY_EDITOR
        Debug.Log($"[PoolingSystem] Created new object. Total created: {createdCount}, Pool size: {TotalObjectCount()}");
#endif
        return instance;
    }

    /// <summary>
    /// Creates a new object without enqueuing (used when pool expands).
    /// </summary>
    private GameObject CreateNewObjectDirect()
    {
        // Unity Object null check
        if (ReferenceEquals(objectPrefab, null) || !objectPrefab)
        {
            Debug.LogError("Cannot instantiate: ObjectPrefab is null or destroyed.");
            return null;
        }

        GameObject instance = Instantiate(objectPrefab, transform);
        instance.SetActive(false);

        createdCount++;
        return instance;
    }

    /// <summary>
    /// Comprehensive cleaning that handles Unity's "fake null" objects properly.
    /// Uses reusable collections to minimize memory allocations.
    /// </summary>
    protected void CleanAvailableQueue()
    {
        if (availableObjects.Count == 0) return;

        // Reuse collections to avoid allocations
        reusableCleanQueue.Clear();
        reusableCleanSet.Clear();
        int removedCount = 0;

        while (availableObjects.Count > 0)
        {
            var go = availableObjects.Dequeue();

            // Unity Object null check - handles both true null and "fake null" destroyed objects
            if (!ReferenceEquals(go, null) && go)
            {
                reusableCleanQueue.Enqueue(go);
                reusableCleanSet.Add(go);
            }
            else
            {
                destroyedCount++;
                removedCount++;

                // Auto-refill destroyed slot
                if (TotalObjectCount() - removedCount < maxPoolSize)
                {
                    var replacement = CreateNewObjectDirect();
                    if (!ReferenceEquals(replacement, null) && replacement)
                    {
                        reusableCleanQueue.Enqueue(replacement);
                        reusableCleanSet.Add(replacement);
                    }
                }
            }
        }

        // Swap the cleaned collections with the original ones
        (availableObjects, reusableCleanQueue) = (reusableCleanQueue, availableObjects);
        (availableSet, reusableCleanSet) = (reusableCleanSet, availableSet);

#if UNITY_EDITOR
        if (removedCount > 0)
        {
            Debug.Log($"[PoolingSystem] Cleaned {removedCount} null/destroyed objects from pool");
        }
#endif
    }

    /// <summary>
    /// Retrieve an object from the pool with lazy cleaning and Unity null handling.
    /// Includes periodic active object cleaning.
    /// </summary>
    public virtual GameObject GetObject()
    {
        // Periodic active object cleaning
        if (Time.time - lastActiveCleanTime > ACTIVE_CLEAN_INTERVAL)
        {
            CleanActiveObjects();
            lastActiveCleanTime = Time.time;
        }

        // Immediate cleaning if we hit too many consecutive nulls
        if (consecutiveNulls >= IMMEDIATE_CLEAN_THRESHOLD)
        {
            CleanAvailableQueue();
            consecutiveNulls = 0;
        }

        GameObject instance = null;

        // Try to get from available objects, handling Unity nulls properly
        while (availableObjects.Count > 0)
        {
            instance = availableObjects.Dequeue();

            // Unity Object null check - this catches both true nulls and destroyed objects
            bool isValidUnityObject = !ReferenceEquals(instance, null) && instance;

            if (!isValidUnityObject)
            {
                // Remove from set if it was there
                if (!ReferenceEquals(instance, null))
                {
                    availableSet.Remove(instance);
                }

                destroyedCount++;
                consecutiveNulls++;

                // Auto-refill destroyed slot
                if (TotalObjectCount() < maxPoolSize)
                {
                    instance = CreateNewObjectDirect();
                    if (!ReferenceEquals(instance, null) && instance)
                    {
                        consecutiveNulls = 0; // Reset since we got a valid object
                        break;
                    }
                }

                instance = null;
                continue;
            }

            consecutiveNulls = 0; // Reset counter on valid object
            availableSet.Remove(instance);

            if (!activeSet.Contains(instance))
            {
                break; // Found valid object
            }

            instance = null;
        }

        // If no available object found, expand in batches
        instance = ExpandPool(instance);

        // Final validation before returning
        if (ReferenceEquals(instance, null) || !instance)
        {
            Debug.LogWarning("[PoolingSystem] Pool exhausted or failed to create object.");
            return null;
        }

        if (activeSet.Contains(instance))
        {
            Debug.LogError($"[PoolingSystem] Pool returned already active object: {instance.name}");
            return null;
        }

        OnPoolRetrieve(instance);

        return instance;
    }

    /// <summary>
    /// Expand the pool with batch creation, returning the first object for immediate use.
    /// </summary>
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
                        firstObject = obj; // Use the first valid one immediately
                    }
                    else
                    {
                        availableObjects.Enqueue(obj);
                        availableSet.Add(obj);
                    }
                }
            }

#if UNITY_EDITOR
            Debug.Log($"[PoolingSystem] Batch expanded by {toCreate}. Total pool size: {TotalObjectCount()}");
#endif

            return firstObject ?? currentInstance;
        }

        return currentInstance;
    }

    /// <summary>
    /// Return an object back to the pool with proper Unity null validation.
    /// </summary>
    public virtual void ReturnObject(GameObject objectToReturn)
    {
        // Unity Object null check
        if (ReferenceEquals(objectToReturn, null) || !objectToReturn)
        {
            Debug.LogWarning("[PoolingSystem] Tried to return null or destroyed object.");
            return;
        }

        if (!activeSet.Contains(objectToReturn))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[PoolingSystem] Tried to return object {objectToReturn.name} which is not active.");
#endif
            return;
        }

        OnPoolReturn(objectToReturn);
    }

    public virtual void OnPoolReturn(GameObject objectToReturn)
    {
        activeObjects.Remove(objectToReturn);
        activeSet.Remove(objectToReturn);

        // Additional safety check before deactivating
        if (!ReferenceEquals(objectToReturn, null) && objectToReturn && objectToReturn.activeInHierarchy)
        {
            objectToReturn.SetActive(false);
        }

        if (!availableSet.Contains(objectToReturn))
        {
            // Additional safety checks before manipulating transform
            if (!ReferenceEquals(objectToReturn, null) && objectToReturn)
            {
                objectToReturn.transform.SetParent(transform, false);
                objectToReturn.transform.localPosition = Vector3.zero;
                objectToReturn.transform.localRotation = Quaternion.identity;
            }

            availableObjects.Enqueue(objectToReturn);
            availableSet.Add(objectToReturn);

            recycledCount++;
        }
    }

    public virtual void OnPoolRetrieve(GameObject objectToRetrieve)
    {
        activeObjects.Add(objectToRetrieve);
        activeSet.Add(objectToRetrieve);

        // Additional safety check before activating
        if (!ReferenceEquals(objectToRetrieve, null) && objectToRetrieve)
        {
            objectToRetrieve.SetActive(true);
        }
    }

    /// <summary>
    /// Clean up active objects list, removing any that have been destroyed externally.
    /// Now called periodically to maintain pool health.
    /// </summary>
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
                }
                destroyedCount++;
                removedCount++;
            }
        }

#if UNITY_EDITOR
        if (removedCount > 0)
        {
            Debug.Log($"[PoolingSystem] Cleaned {removedCount} destroyed objects from active list");
        }
#endif
    }

    /// <summary>
    /// Utility method to check if a Unity Object is truly valid
    /// </summary>
    private bool IsValidUnityObject(Object obj)
    {
        return !ReferenceEquals(obj, null) && obj;
    }

    /// <summary>
    /// Warm up the pool by pre-creating objects (useful during loading screens)
    /// </summary>
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
        Debug.Log($"[PoolingSystem] Pool warmed to {TotalObjectCount()} objects");
#endif
    }

    /// <summary>
    /// Force immediate cleaning of both available and active objects
    /// </summary>
    public void ForceCleanAll()
    {
        CleanAvailableQueue();
        CleanActiveObjects();
        consecutiveNulls = 0;
        lastActiveCleanTime = Time.time;
    }

    // --- Utility counts ---
    public int TotalObjectCount() => availableObjects.Count + activeObjects.Count;
    public int AvailableObjectCount() => availableObjects.Count;
    public int ActiveObjectCount() => activeObjects.Count;

    // --- Memory stats ---
    public int CreatedCount => createdCount;
    public int RecycledCount => recycledCount;
    public int DestroyedCount => destroyedCount;

    // --- Advanced Statistics ---
    public float HitRatio => createdCount > 0 ? (float)recycledCount / (createdCount + recycledCount) : 0f;
    public float PoolUtilization => maxPoolSize > 0 ? (float)activeObjects.Count / maxPoolSize : 0f;
    public float PoolHealth => TotalObjectCount() > 0 ? 1f - ((float)destroyedCount / (createdCount + destroyedCount)) : 1f;

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only method to validate pool integrity
    /// </summary>
    [ContextMenu("Validate Pool Integrity")]
    public void ValidatePoolIntegrity()
    {
        ForceCleanAll();

        Debug.Log($"[PoolingSystem] Pool Validation Complete:\n" +
                  $"Available: {availableObjects.Count}, Active: {activeObjects.Count}\n" +
                  $"Created: {createdCount}, Recycled: {recycledCount}, Destroyed: {destroyedCount}\n" +
                  $"Hit Ratio: {HitRatio:P1}, Utilization: {PoolUtilization:P1}, Health: {PoolHealth:P1}");
    }

    /// <summary>
    /// Force warm the pool from the inspector
    /// </summary>
    [ContextMenu("Warm Pool to Max")]
    public void WarmPoolToMax()
    {
        WarmPool(maxPoolSize);
    }

    /// <summary>
    /// Show current pool statistics
    /// </summary>
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