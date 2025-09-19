using System.Collections.Generic;
using UnityEngine;

public class PoolingSystem : MonoBehaviour, IPoolable
{
    [Header("Pool Settings")]
    [SerializeField] protected GameObject objectPrefab;
    [SerializeField] protected int initialPoolSize = 5;
    [SerializeField] protected int maxPoolSize = 20;

    protected Queue<GameObject> availableObjects = new Queue<GameObject>();
    protected List<GameObject> activeObjects = new List<GameObject>();

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

        if (objectPrefab == null)
        {
            Debug.LogError("Object prefab is null. Cannot initialize pool!");
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewPoolObject();
        }
    }

    /// <summary>
    /// Creates a new object for the pool and enqueues it.
    /// </summary>
    public GameObject CreateNewPoolObject()
    {
        if (objectPrefab == null)
        {
            Debug.LogError("No object prefab assigned. Cannot create pooled object.");
            return null;
        }

        GameObject instance = Instantiate(objectPrefab, transform);
        HealthSystem hs = instance.GetComponent<HealthSystem>();

        if (hs == null)
        {
            Debug.LogWarning($"{objectPrefab.name} is missing HealthSystem. Destroying instance.");
            Destroy(instance);
            return null;
        }

        hs.InitializeForPool(this);
        instance.SetActive(false);
        availableObjects.Enqueue(instance);

        return instance;
    }

    /// <summary>
    /// Creates a new object without enqueuing (used when pool expands).
    /// </summary>
    private GameObject CreateNewObjectDirect()
    {
        if (objectPrefab == null)
        {
            Debug.LogError("Object prefab is null. Cannot create object.");
            return null;
        }

        GameObject instance = Instantiate(objectPrefab, transform);
        HealthSystem hs = instance.GetComponent<HealthSystem>();

        if (hs == null)
        {
            Debug.LogWarning($"{objectPrefab.name} is missing HealthSystem. Destroying instance.");
            Destroy(instance);
            return null;
        }

        hs.InitializeForPool(this);
        instance.SetActive(false);
        return instance;
    }

    /// <summary>
    /// Removes null/destroyed objects from the available queue.
    /// </summary>
    protected void CleanAvailableQueue()
    {
        if (availableObjects.Count == 0) return;

        Queue<GameObject> cleanQueue = new Queue<GameObject>(availableObjects.Count);
        while (availableObjects.Count > 0)
        {
            var go = availableObjects.Dequeue();
            if (go != null)
                cleanQueue.Enqueue(go);
        }
        availableObjects = cleanQueue;
    }

    /// <summary>
    /// Retrieve an object from the pool, expanding if needed.
    /// </summary>
    public GameObject GetObject()
    {
        CleanAvailableQueue();

        GameObject instance = null;

        // Try to get from available objects first
        while (availableObjects.Count > 0)
        {
            instance = availableObjects.Dequeue();
            if (instance != null && !activeObjects.Contains(instance))
            {
                break; // Found a valid object
            }
            instance = null; // Object was invalid, try next
        }

        // If no available object found, create new one if possible
        if (instance == null && TotalObjectCount() < maxPoolSize)
        {
            instance = CreateNewObjectDirect();
        }

        if (instance == null)
        {
            Debug.LogWarning("Pool exhausted or failed to create object.");
            return null;
        }

        // Ensure object isn't already active
        if (activeObjects.Contains(instance))
        {
            Debug.LogError($"Pool returned already active object: {instance.name}");
            return null;
        }

        activeObjects.Add(instance);

        var hs = instance.GetComponent<HealthSystem>();
        hs?.OnPoolRetrieve();

        instance.SetActive(true);
        return instance;
    }

    /// <summary>
    /// Return an object back to the pool.
    /// </summary>
    public virtual void ReturnObject(GameObject objectToReturn)
    {
        if (objectToReturn == null)
            return;

        if (!activeObjects.Contains(objectToReturn))
            return;

        activeObjects.Remove(objectToReturn);

        if (objectToReturn.activeInHierarchy)
            objectToReturn.SetActive(false);

        if (!availableObjects.Contains(objectToReturn))
            availableObjects.Enqueue(objectToReturn);
    }

    // --- Utility counts ---
    protected int TotalObjectCount() => availableObjects.Count + activeObjects.Count;
    protected int AvailableObjectCount() => availableObjects.Count;
    protected int ActiveObjectCount() => activeObjects.Count;
}
