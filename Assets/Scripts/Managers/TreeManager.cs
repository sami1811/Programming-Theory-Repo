using UnityEngine;

public class TreeManager : PoolingSystem
{
    [SerializeField] private float respawnDelay = 5f;

    private SpawningSystem spawningSystem;
    private int respawnTries = 5;

    private void Start()
    {
        InitializeStart();
    }

    private void Update()
    {
        RespaenTree();
    }

    private void InitializeStart()
    {
        if(spawningSystem == null)
        {
            spawningSystem = GetComponent<SpawningSystem>();

            if (spawningSystem != null )
            {
                Debug.Log("[Tree Manager] Spawn manager found.");
            }
            else
            {
                Debug.LogError($"Spawn manager is missing!");
            }
        }
    }

    private void RespaenTree()
    {
        if (!ReferenceEquals(spawningSystem, null) && spawningSystem)
        {
            spawningSystem.StartRespawn(respawnDelay, respawnTries);
        }
    }
}
