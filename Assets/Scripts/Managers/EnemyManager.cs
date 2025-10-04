using UnityEngine;

public class EnemyManager : PoolingSystem
{
    [Header("Enemy Target Settings")]
    public GameObject targetObject;

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
            Debug.LogError("[Enemy Manager] Spawn manager is missing!");
#endif
        }
        else
        {
            _mySpawner.StartProgressiveSpawning();
        }
    }
}