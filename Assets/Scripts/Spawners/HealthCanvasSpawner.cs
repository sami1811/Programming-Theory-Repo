using TMPro;
using UnityEngine;

public class HealthCanvasSpawner : PoolingSystem
{
    [Header("Canvas Settings")]
    [SerializeField] private Vector3 healthBarOffset;
    [SerializeField] private Vector3 healthBarScale;

    public static HealthCanvasSpawner Instance { get; private set; }

    protected override void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(Instance);
        }

        Instance = this;
        base.Awake();
    }

    void RegisterHealthSystem (HealthSystem hs)
    {
        if (hs == null)
        {
            Debug.LogWarning("No health component found!");
            return;
        }

        objectPrefab.transform.position = hs.transform.position + healthBarOffset;
        objectPrefab.transform.localScale = healthBarScale;
    }

    public override void OnPoolReturn(GameObject objectToReturn)
    {

    }

    public override void OnPoolRetrieve(GameObject objectToRetrieve)
    {

    }
}
