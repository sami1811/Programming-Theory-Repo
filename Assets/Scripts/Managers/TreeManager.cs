using UnityEngine;

public class TreeManager : PoolingSystem
{
    [SerializeField] private float respawnDelay = 5f;

    private int respawnTries = 5;

    private void Start()
    {
        InitializeStart();
    }

    private void Update()
    {
        RespawnTree();
        StartAutoShrink();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void InitializeStart()
    {
        if (SpawningSystem.instance == null)
        {
            Debug.LogError($"[Tree Manager] Spawn manager is missing!");
            return;
        }

        SpawningSystem.instance.SpawnInitialObject();
    }

    private void RespawnTree()
    {
        if (SpawningSystem.instance != null)
        {
            SpawningSystem.instance.StartRespawn(respawnDelay, respawnTries);
        }
    }
}
