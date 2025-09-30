using UnityEngine;

public class EnemyManager : PoolingSystem
{
    [Header("Enemy Target Settings")]
    public GameObject targetObject;

    private SpawningSystem mySpawner;

    private void Start()
    {
        InitializeStart();
    }

    private void InitializeStart()
    {
        mySpawner = GetComponent<SpawningSystem>();

        if (mySpawner != null)
        {
            mySpawner.SpawnInitialObject();
        }
    }

    private void Update()
    {
        mySpawner?.RespawnObject();
        StartAutoShrink();
    }
}
