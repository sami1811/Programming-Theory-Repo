using UnityEngine;

public class TreeManager : PoolingSystem
{
    [SerializeField] private float respawnDelay = 5f;

    private SpawningSystem _spawningSystem;
    private int respawnTries = 5;

    protected override void Awake()
    {
        base.Awake();
        InitializeAwake();
    }

    private void Update()
    {
        RespaenTree();
    }

    private void InitializeAwake()
    {
        if(_spawningSystem == null)
        {
            _spawningSystem = GetComponent<SpawningSystem>();
            Debug.Log("Spawn manager found.");
        }
        else
        {
            Debug.LogError("Spawn manager missing!");
        }
    }

    private void RespaenTree()
    {
        if (!ReferenceEquals(_spawningSystem, null) && _spawningSystem)
        {
            _spawningSystem.StartRespawn(respawnDelay, respawnTries);
        }
    }
}
