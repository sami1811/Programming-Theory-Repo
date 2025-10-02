using UnityEngine;

public class TreeManager : PoolingSystem
{
    private SpawningSystem _mySpawner;
    
    private void Start()
    {
        StartAutoShrink();
        InitializeStart();
    }

    private void InitializeStart()
    {
        _mySpawner = GetComponent<SpawningSystem>();

        if (!_mySpawner)
        {
#if UNITY_EDITOR
            Debug.LogError("[Tree Manager] Spawn manager is missing!");
#endif
        }
        else
        {
            _mySpawner.SpawnInitialObjects();
            _mySpawner.StartProgressiveSpawning();
        }
    }
}
