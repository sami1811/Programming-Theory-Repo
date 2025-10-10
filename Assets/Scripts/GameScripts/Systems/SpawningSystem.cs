using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(PoolingSystem))]
public class SpawningSystem : MonoBehaviour
{
    [Header("Spawn Area Settings")]
    [Tooltip("List of game objects that define spawnable areas. Each must have a Renderer (e.g., plane).")]
    public List<GameObject> spawnAreas = new List<GameObject>();

    [Header("Spawner Settings")]
    [Tooltip("Minimum allowed distance between spawned objects.")]
    public float minDistance;

    [Tooltip("Vertical offset applied to all spawned objects (to place them above the surface).")]
    public float yOffset;

    [Tooltip("Maximum attempts to find a valid position per object.")]
    public int maxSpawnAttempts;

    [FormerlySerializedAs("startingEnemyCount")]
    [Header("Progressive Spawn Settings")]
    [SerializeField] private bool shouldAvoidObstacles;
    [SerializeField] private int startingObjectCount;
    [SerializeField] private float initialSpawnDelay;
    [SerializeField] private float minSpawnDelay;
    [SerializeField] private float spawnAcceleration;
    
    private int _maxSimultaneousEnemies; // Auto-assigned from pool's maxPoolSize

    private PoolingSystem _poolingSystem;

    // Track all spawned objects and their positions
    private readonly HashSet<GameObject> _spawnedObjects = new HashSet<GameObject>();
    private readonly List<Vector3> _activePositions = new List<Vector3>();

    // Spawning control
    private Coroutine _spawnCoroutine;
    private float _currentSpawnDelay;
    private int _currentMaxEnemies;

    public IReadOnlyCollection<GameObject> SpawnedObjects => _spawnedObjects;

    private void Awake()
    {
        _poolingSystem = GetComponent<PoolingSystem>();
        
#if UNITY_EDITOR
        if (!_poolingSystem)
        {
            Debug.LogError("[SpawningSystem] PoolingSystem component missing.");
        } 
#endif

        _currentSpawnDelay = initialSpawnDelay;
        _currentMaxEnemies = startingObjectCount;
    }

    private void Start()
    {
        // Assign maxSimultaneousEnemies from pool's maxPoolSize
        _maxSimultaneousEnemies = _poolingSystem.MaxPoolSize;
        
#if UNITY_EDITOR
        Debug.Log($"[SpawningSystem] Max simultaneous enemies set to: {_maxSimultaneousEnemies}");
#endif
    }

    private void OnDestroy()
    {
        if (_spawnCoroutine != null)
            StopCoroutine(_spawnCoroutine);
    }

    #region Public API

    /// <summary>
    /// Spawn all objects up to InitialPoolSize immediately (for static objects like trees, rocks, etc.)
    /// </summary>
    public void SpawnInitialObjects()
    {
        int successfulSpawns = 0;
        int attempts = 0;
        int targetCount = _poolingSystem.InitialPoolSize;
        int maxAttempts = targetCount * maxSpawnAttempts;

#if UNITY_EDITOR
        Debug.Log($"[SpawningSystem] Spawning initial objects. Target: {targetCount}");
#endif
        while (successfulSpawns < targetCount && attempts < maxAttempts)
        {
            if (TrySpawnEnemy())
            {
                successfulSpawns++;
            }
            attempts++;
        }
#if UNITY_EDITOR
        Debug.Log($"[SpawningSystem] Spawned {successfulSpawns}/{targetCount} initial objects.");
#endif
    }

    /// <summary>
    /// Start the progressive spawning system for enemies (gradual increase over time)
    /// </summary>
    public void StartProgressiveSpawning()
    {
        if (_spawnCoroutine != null)
            StopCoroutine(_spawnCoroutine);

        _currentSpawnDelay = initialSpawnDelay;
        _currentMaxEnemies = startingObjectCount;
        
#if UNITY_EDITOR
        //Debug.Log($"[SpawningSystem] Starting progressive spawn system. Initial enemies: {startingObjectCount}, Max: {_maxSimultaneousEnemies}");
#endif
        
        _spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    /// <summary>
    /// Stop the spawning system
    /// </summary>
    public void StopSpawning()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
    }

    /// <summary>
    /// Call this when an enemy dies and should be returned to pool
    /// </summary>
    public void ReturnObjectToPool(GameObject obj)
    {
        if (!obj) return;

        // Remove from tracking
        if (_spawnedObjects.Contains(obj))
        {
            _spawnedObjects.Remove(obj);
            RebuildPositionList();
        }

        // Return to pool
        _poolingSystem.ReturnToPool(obj);
    }

    #endregion

    #region Spawning Logic

    private IEnumerator SpawnRoutine()
    {
        // Spawn initial enemies immediately
        int spawnedInitial = 0;
        for (int i = 0; i < startingObjectCount; i++)
        {
            if (TrySpawnEnemy())
            {
                spawnedInitial++;
            }
            yield return null; // Spread across frames
        }

#if UNITY_EDITOR
        Debug.Log($"[SpawningSystem] Spawned {spawnedInitial}/{startingObjectCount} initial enemies.");
#endif
        
        // Main spawn loop
        while (true)
        {
            yield return new WaitForSeconds(_currentSpawnDelay);

            CleanupDestroyedObjects();

            int currentActive = CountActiveEnemies();
            
            // Spawn if below current max capacity
            if (currentActive < _currentMaxEnemies)
            {
                if (TrySpawnEnemy())
                {
                    // Successfully spawned - accelerate spawn rate
                    _currentSpawnDelay = Mathf.Max(minSpawnDelay, _currentSpawnDelay * spawnAcceleration);
                    
#if UNITY_EDITOR
//                    Debug.Log($"[SpawningSystem] Spawned enemy. Active: {currentActive + 1}/{_currentMaxEnemies} | " +
     //                         $"Delay: {_currentSpawnDelay:F2}s | Pool: {_poolingSystem.ActiveObjectCount()}/{_poolingSystem.TotalObjectCount()}");
#endif
                }
                else
                {
                    // Failed to spawn (no valid position or pool exhausted)
                    // Slow down to avoid spam
                    _currentSpawnDelay = Mathf.Min(initialSpawnDelay, _currentSpawnDelay * 1.1f);
                }
            }
            else
            {
                // At capacity - try to increase max enemies if possible
                if (_currentMaxEnemies < _maxSimultaneousEnemies)
                {
                    _currentMaxEnemies = Mathf.Min(_currentMaxEnemies + 1, _maxSimultaneousEnemies);
                    
#if UNITY_EDITOR
//                    Debug.Log($"[SpawningSystem] Increased max enemies to: {_currentMaxEnemies}/{_maxSimultaneousEnemies}");
#endif
                }
                else
                {
                    // At absolute max - reset spawn delay for when enemies die
                    _currentSpawnDelay = initialSpawnDelay;
                }
            }
        }
    }

    private bool TrySpawnEnemy()
    {
        if (spawnAreas.Count == 0)
        {
            
#if UNITY_EDITOR
            Debug.LogWarning("[SpawningSystem] No spawn areas assigned!");
#endif
            
            return false;
        }

        // Try to find a valid position
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector3 spawnPos = GetRandomPosition();
            
            if (!IsValidPosition(spawnPos))
                continue;

            // Get object from pool
            GameObject obj = _poolingSystem.GetObject();
            if (!obj)
            {
                // Pool exhausted - it will auto-expand if below maxPoolSize
                int poolTotal = _poolingSystem.TotalObjectCount();
                int poolMax = _poolingSystem.MaxPoolSize;
                
                if (poolTotal >= poolMax)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[SpawningSystem] Pool at maximum capacity ({poolTotal}/{poolMax}). Cannot spawn more.");
#endif
                }
                
                return false;
            }

            // Position and track the object
            spawnPos = ApplyOffsetY(spawnPos);
            obj.transform.position = spawnPos;
            obj.transform.rotation = Quaternion.identity;

            _spawnedObjects.Add(obj);
            _activePositions.Add(spawnPos);

            return true;
        }
        
#if UNITY_EDITOR
        Debug.LogWarning($"[SpawningSystem] Failed to find valid spawn position after {maxSpawnAttempts} attempts.");
#endif
        
        return false;
    }

    #endregion

    #region Helper Methods

    private Vector3 GetRandomPosition()
    {
        GameObject randomArea = spawnAreas[Random.Range(0, spawnAreas.Count)];
        Renderer planeRenderer = randomArea.GetComponent<Renderer>();
        
        if (!planeRenderer)
        {
            
#if UNITY_EDITOR
            Debug.LogError("[SpawningSystem] Spawn area has no Renderer.");
#endif
            
            return Vector3.zero;
        }

        Bounds b = planeRenderer.bounds;
        return new Vector3(
            Random.Range(b.min.x, b.max.x),
            b.max.y,
            Random.Range(b.min.z, b.max.z)
        );
    }

    private bool IsValidPosition(Vector3 pos)
    {
        float minDistSq = minDistance * minDistance;

        // Check distance from all active spawn positions
        foreach (Vector3 existingPos in _activePositions)
        {
            if ((pos - existingPos).sqrMagnitude < minDistSq)
                return false;
        }

        // Check for obstacles (optional)
        if (shouldAvoidObstacles)
        {
            if (Physics.CheckSphere(pos, 0.5f, LayerMask.GetMask("Obstacles")))
                return false;
        }

        return true;
    }

    private Vector3 ApplyOffsetY(Vector3 pos)
    {
        return new Vector3(pos.x, pos.y + yOffset, pos.z);
    }

    private void CleanupDestroyedObjects()
    {
        _spawnedObjects.RemoveWhere(obj => obj == null || !obj);
        RebuildPositionList();
    }

    private void RebuildPositionList()
    {
        _activePositions.Clear();
        foreach (var obj in _spawnedObjects)
        {
            if (obj && obj.activeInHierarchy)
            {
                _activePositions.Add(obj.transform.position);
            }
        }
    }

    private int CountActiveEnemies()
    {
        int count = 0;
        foreach (var obj in _spawnedObjects)
        {
            if (obj && obj.activeInHierarchy)
                count++;
        }
        return count;
    }

    #endregion

    #region Debug Utilities

#if UNITY_EDITOR
    [ContextMenu("Spawn Initial Objects")]
    public void DebugSpawnInitial()
    {
        SpawnInitialObjects();
    }

    [ContextMenu("Start Progressive Spawning")]
    public void DebugStartProgressiveSpawning()
    {
        StartProgressiveSpawning();
    }

    [ContextMenu("Stop Spawning")]
    public void DebugStopSpawning()
    {
        StopSpawning();
    }

    [ContextMenu("Show Stats")]
    public void ShowStats()
    {
        CleanupDestroyedObjects();
        int active = CountActiveEnemies();
        
        Debug.Log($"=== SPAWNING STATISTICS ===\n" +
                  $"Active Enemies: {active}/{_currentMaxEnemies} (Max: {_maxSimultaneousEnemies})\n" +
                  $"Tracked Objects: {_spawnedObjects.Count}\n" +
                  $"Active Positions: {_activePositions.Count}\n" +
                  $"Current Spawn Delay: {_currentSpawnDelay:F2}s\n" +
                  $"Pool Stats:\n" +
                  $"  - Active: {_poolingSystem.ActiveObjectCount()}\n" +
                  $"  - Available: {_poolingSystem.AvailableObjectCount()}\n" +
                  $"  - Total: {_poolingSystem.TotalObjectCount()}/{_poolingSystem.MaxPoolSize}");
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Draw spawn positions
        Gizmos.color = Color.green;
        foreach (var pos in _activePositions)
        {
            Gizmos.DrawWireSphere(pos, minDistance * 0.5f);
        }

        // Draw spawn areas
        if (spawnAreas != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var area in spawnAreas)
            {
                if (area)
                {
                    Renderer r = area.GetComponent<Renderer>();
                    if (r)
                    {
                        Gizmos.DrawWireCube(r.bounds.center, r.bounds.size);
                    }
                }
            }
        }
    }
#endif

    #endregion
}