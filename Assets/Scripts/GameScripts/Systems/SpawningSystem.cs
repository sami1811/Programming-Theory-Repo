using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PoolingSystem))]
public class SpawningSystem : MonoBehaviour
{
    public static SpawningSystem instance {  get; private set; }

    [Header("Spawn Area Settings")]
    [Tooltip("List of game objects that define spawnable areas. Each must have a Renderer (e.g., plane).")]
    public List<GameObject> spawnAreas = new List<GameObject>();

    [Header("Spawner Settings")]
    [Tooltip("Minimum allowed distance between spawned objects.")]
    public float minDistance = 5f;

    [Tooltip("Vertical offset applied to all spawned objects (to place them above the surface).")]
    public float yOffset = 0.99f;

    [Header("Spawn Loop Settings")]
    [Tooltip("Maximum number of attempts to find a valid spawn position per object.")]
    public int maxSpawnAttempts = 2;

    // Reference to the pooling system that provides/recycles objects
    private PoolingSystem poolingSystem;

    // Internal dictionary mapping active objects to their spawn positions
    private readonly Dictionary<GameObject, Vector3> spawnedMap = new Dictionary<GameObject, Vector3>();

    /// <summary>
    /// Read-only access to the current spawned object-position map.
    /// </summary>
    public IReadOnlyDictionary<GameObject, Vector3> SpawnedMap => spawnedMap;

    // Respawn coroutine state
    private Coroutine respawnRoutine;
    private bool isRespawning;

    private void Awake()
    {
        InitializeAwake();
    }

    private void Start()
    {
        InitializeStart();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    /// <summary>
    /// Spawns the initial set of pooled objects at random valid positions.
    /// </summary>
    private void InitializeStart()
    {
        SpawnInitialObject();
    }

    /// <summary>
    /// Finds the PoolingSystem component on this GameObject.
    /// </summary>
    private void InitializeAwake()
    {
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        poolingSystem = GetComponent<PoolingSystem>();

        if (poolingSystem == null)
        {
            Debug.LogError("[Spawning system] PoolingSystem component missing on this GameObject.");
            return;
        }
    }

    /// <summary>
    /// Attempts to spawn as many objects as the initial pool size allows.
    /// Respects minimum distance and maximum attempts.
    /// </summary>
    public void SpawnInitialObject()
    {
        int successfulSpawns = 0;
        int attempts = 0;
        int maxAttempts = poolingSystem.InitialPoolSize * maxSpawnAttempts;

        while (successfulSpawns < poolingSystem.InitialPoolSize && attempts < maxAttempts)
        {
            if (SpawnWithoutOverlap())
                successfulSpawns++;

            attempts++;
        }

        if (poolingSystem.ObjectPrefab == null)
        {
            Debug.LogWarning("Object prefab is null!");
            return;
        }

        Debug.Log($"Successfully spawned {successfulSpawns} out of {poolingSystem.InitialPoolSize} {poolingSystem.ObjectPrefab.name}(s)");

        if (successfulSpawns < poolingSystem.InitialPoolSize)
        {
            Debug.LogWarning(
                $"Could only spawn {successfulSpawns} out of {poolingSystem.InitialPoolSize} " +
                $"due to space constraints or max attempts reached."
            );
        }
    }

    /// <summary>
    /// Attempts to spawn a pooled object at a random position that does not overlap with existing objects.
    /// </summary>
    /// <returns>True if successful, false otherwise.</returns>
    protected virtual bool SpawnWithoutOverlap()
    {
        if (spawnAreas.Count < 1)
        {
            Debug.LogWarning("No spawn areas are assigned!");
            return false;
        }

        Vector3 spawnPosition = Vector3.zero;
        bool validPositionFound = false;

        // Try multiple times to find a valid position
        for (int i = 0; i < maxSpawnAttempts; ++i)
        {
            Vector3 randomPos = GetRandomPosition();

            if (IsValidPosition(randomPos))
            {
                randomPos = ApplyOffsetY(randomPos);
                spawnPosition = randomPos;
                validPositionFound = true;
                break;
            }
        }

        if (validPositionFound)
        {
            GameObject objectToSpawn = poolingSystem.GetObject();

            if (objectToSpawn != null)
            {
                // Prevent double assignment
                if (spawnedMap.ContainsKey(objectToSpawn))
                {
                    Debug.LogWarning($"{objectToSpawn.name} is being respawned without being returned. Retry spawning!");
                    ReturnObject(objectToSpawn);
                    return false;
                }

                objectToSpawn.transform.position = spawnPosition;
                spawnedMap[objectToSpawn] = spawnPosition;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Spawns an object at a specific custom position if valid.
    /// </summary>
    protected virtual void SpawnAtCustomPosition(Vector3 position)
    {
        if (IsValidPosition(position))
        {
            position = ApplyOffsetY(position);

            GameObject objectToSpawn = poolingSystem.GetObject();

            if (objectToSpawn != null)
            {
                if (spawnedMap.ContainsKey(objectToSpawn))
                {
                    Debug.LogWarning($"{objectToSpawn.name} is being respawned without being returned. Retry spawning!");
                    ReturnObject(objectToSpawn);
                    return;
                }

                objectToSpawn.transform.position = position;
                spawnedMap[objectToSpawn] = position;
            }
        }
        else
        {
            Debug.LogWarning($"Position {position} is too close to existing objects.");
        }
    }

    /// <summary>
    /// Coroutine that retries spawning an object after a delay until successful.
    /// </summary>
    private IEnumerator RespawnAtRandomAfterDelay(float delay, int numberOfTries)
    {
        isRespawning = true;

        while (true)
        {
            yield return new WaitForSeconds(delay);

            if (poolingSystem.ActiveObjectCount() < poolingSystem.MaxPoolSize)
            {
                if (DelayedSpawn(numberOfTries))
                { // Successfully spawned → break out
                    break;
                }
                else
                {
                    Debug.Log("Respawn failed, retrying after delay...");
                }
            }
            else
            {
                Debug.Log("Pool full, waiting before retry...");
            }
        }

        isRespawning = false;
    }

    /// <summary>
    /// Attempts to spawn an object with limited retries.
    /// </summary>
    private bool DelayedSpawn(int maxRetries)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            Vector3 spawnPos = GetRandomPosition();

            if (!IsValidPosition(spawnPos)) continue;

            spawnPos = ApplyOffsetY(spawnPos);
            GameObject objectToSpawn = poolingSystem.GetObject();

            if (objectToSpawn == null)
            {
                Debug.LogWarning($"Pool exhausted on delayed spawn attempt {attempt + 1}");
                continue;
            }

            if (spawnedMap.ContainsKey(objectToSpawn))
            {
                Debug.LogWarning($"Pool returned active object. Retry {attempt + 1}/{maxRetries}");
                ReturnObject(objectToSpawn);
                continue;
            }

            // Success!
            objectToSpawn.transform.position = spawnPos;
            spawnedMap[objectToSpawn] = spawnPos;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Starts a respawn coroutine that retries until an object is spawned.
    /// </summary>
    public void StartRespawn(float delay, int tries)
    {
        if (isRespawning) return;

        respawnRoutine = StartCoroutine(RespawnAtRandomAfterDelay(delay, tries));
    }

    /// <summary>
    /// Spawns an object manually at a random valid position.
    /// </summary>
    protected virtual void SpawnManualAtRandom()
    {
        Vector3 randomPos = GetRandomPosition();

        if (IsValidPosition(randomPos))
        {
            randomPos = ApplyOffsetY(randomPos);
            GameObject objectToSpawn = poolingSystem.GetObject();

            if (objectToSpawn != null)
            {
                if (spawnedMap.ContainsKey(objectToSpawn))
                {
                    Debug.LogWarning($"{objectToSpawn.name} is being respawned without being returned. Retry spawning!");
                    ReturnObject(objectToSpawn);
                    return;
                }

                objectToSpawn.transform.position = randomPos;
                spawnedMap[objectToSpawn] = randomPos;
            }
        }
    }

    /// <summary>
    /// Picks a random position inside a randomly chosen spawn area's renderer bounds.
    /// </summary>
    private Vector3 GetRandomPosition()
    {
        if (spawnAreas.Count == 0)
        {
            Debug.LogError("No spawn areas available!");
            return Vector3.zero;
        }

        GameObject randomArea = spawnAreas[Random.Range(0, spawnAreas.Count)];
        Renderer planeRenderer = randomArea.GetComponent<Renderer>();

        if (planeRenderer == null)
        {
            Debug.LogError($"No renderer found on spawn area: {randomArea.name}");
            return Vector3.zero;
        }

        Bounds bounds = planeRenderer.bounds;

        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            bounds.max.y,
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }

    /// <summary>
    /// Checks whether the given position is far enough from all currently spawned objects.
    /// </summary>
    private bool IsValidPosition(Vector3 position)
    {
        float minDistSq = minDistance * minDistance;

        foreach (Vector3 existingPos in spawnedMap.Values)
        {
            if ((position - existingPos).sqrMagnitude < minDistSq) return false;
        }

        return true;
    }

    /// <summary>
    /// Returns an object back to the pool and removes it from the spawned map.
    /// </summary>
    private void ReturnObject(GameObject objectToReturn)
    {
        if (objectToReturn == null) return;

        if (spawnedMap.ContainsKey(objectToReturn))
        {
            spawnedMap.Remove(objectToReturn);
        }

        poolingSystem.ReturnToPool(objectToReturn);
    }

    /// <summary>
    /// Force the Y-position to a fixed offset, ignoring the original Y.
    /// </summary>
    protected Vector3 ApplyOffsetY(Vector3 pos)
    {
        return new Vector3(pos.x, pos.y + yOffset, pos.z);
    }
}
