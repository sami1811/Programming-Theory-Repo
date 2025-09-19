using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GlobalSpawner : PoolingSystem
{
    [Header("Spawn Area Settings")]
    public List<GameObject> spawnAreas = new List<GameObject>();

    [Header("Spawner Settings")]
    public float minDistance = 5f;
    public float yOffset = 0.99f;

    [Header("Spawn Loop Settings")]
    public int maxSpawnAttempts = 2;

    // Internal dictionary of spawned objects and their positions
    private readonly Dictionary<GameObject, Vector3> spawnedMap = new Dictionary<GameObject, Vector3>();

    // Public read-only view for safe external access
    public IReadOnlyDictionary<GameObject, Vector3> SpawnedMap => spawnedMap;

    private Coroutine respawnRoutine;
    private bool isRespawning;

    private void Awake()
    {
        InitializeOnAwake();
    }

    protected void InitializeOnAwake()
    {
        InitializePool();
        Debug.Log("Pool has been initialized successfully!");
    }

    protected void SpawnInitialObject()
    {
        int successfulSpawns = 0;
        int attempts = 0;
        int maxAttempts = initialPoolSize * maxSpawnAttempts;

        while (successfulSpawns < initialPoolSize && attempts < maxAttempts)
        {
            if (SpawnWithoutOverlap())
                successfulSpawns++;

            attempts++;
        }

        Debug.Log($"Successfully spawned {successfulSpawns} out of {initialPoolSize} {objectPrefab.name}(s)");

        if (successfulSpawns < initialPoolSize)
        {
            Debug.LogWarning(
                $"Could only spawn {successfulSpawns} out of {initialPoolSize} " +
                $"due to space constraints or max attempts reached."
            );
        }
    }

    protected virtual bool SpawnWithoutOverlap()
    {
        if (spawnAreas.Count < 1)
        {
            Debug.LogWarning("No spawn areas are assigned!");
            return false;
        }

        Vector3 spawnPosition = Vector3.zero;
        bool validPositionFound = false;

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
            GameObject objectToSpawn = GetObject();

            if (objectToSpawn != null)
            {
                // Optional: warn if this object was already in the map
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

    protected virtual void SpawnAtCustomPosition(Vector3 position)
    {
        if (IsValidPosition(position))
        {
            position = ApplyOffsetY(position);

            GameObject objectToSpawn = GetObject();

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

    protected IEnumerator RespawnAtRandomAfterDelay(float delay, int numberOfTries)
    {
        isRespawning = true;

        while (true)
        {
            yield return new WaitForSeconds(delay);

            if (ActiveObjectCount() < maxPoolSize)
            {
                if (DelayedSpawn(numberOfTries))
                {
                    // Successfully spawned → break out
                    break;
                }
                else
                {
                    Debug.Log("Respawn failed, retrying after delay...");
                    // loop will retry
                }
            }
            else
            {
                Debug.Log("Pool full, waiting before retry...");
                // loop will retry automatically after delay
            }
        }

        isRespawning = false;
    }

    private bool DelayedSpawn(int maxRetries)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            Vector3 spawnPos = GetRandomPosition();

            if (!IsValidPosition(spawnPos))
                continue; // Try again with different position

            spawnPos = ApplyOffsetY(spawnPos);
            GameObject objectToSpawn = GetObject();

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

    public void StartRespawn(float delay, int tries)
    {
        if (isRespawning) return; // already running
        respawnRoutine = StartCoroutine(RespawnAtRandomAfterDelay(delay, tries));
    }

    protected virtual void SpawnManualAtRandom()
    {
        Vector3 randomPos = GetRandomPosition();

        if (IsValidPosition(randomPos))
        {
            randomPos = ApplyOffsetY(randomPos);
            GameObject objectToSpawn = GetObject();

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
            bounds.max.y + yOffset,
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }

    private bool IsValidPosition(Vector3 position)
    {
        float minDistSq = minDistance * minDistance;
        foreach (Vector3 existingPos in spawnedMap.Values)
        {
            if ((position - existingPos).sqrMagnitude < minDistSq)
                return false;
        }
        return true;
    }

    public override void ReturnObject(GameObject objectToReturn)
    {
        if (objectToReturn == null)
            return;

        if (spawnedMap.ContainsKey(objectToReturn))
            spawnedMap.Remove(objectToReturn);

        base.ReturnObject(objectToReturn);
    }

    /// <summary>
    /// Force the Y-position to a fixed offset, ignoring the original Y.
    /// </summary>
    protected Vector3 ApplyOffsetY(Vector3 newPosWithYOffset)
    {
        return new Vector3(newPosWithYOffset.x, yOffset, newPosWithYOffset.z);
    }
}
