using UnityEngine;

public class TreeManager : PoolingSystem
{
    private SpawningSystem _spawningSystem;

    protected override void Awake()
    {
        base.Awake();
        InitializeAwake();
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
}
