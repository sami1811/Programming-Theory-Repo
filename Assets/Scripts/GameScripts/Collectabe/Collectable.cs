using UnityEngine;

public class Collectable : MonoBehaviour
{
    [Header("Collectable Settings")]
    [SerializeField] private HealthSystem healthSystem;

    private void OnEnable()
    {
        if (!healthSystem)
        {
            Debug.LogError("[Collectable] Assign health system in inspector!");
        }
    }

    private void OnDisable()
    {
        if (!healthSystem || !StatsSystem.Instance)
            return;

        if (!healthSystem.IsAlive())
        {
            CollectableManager.Instance?.AddPoints(Mathf.FloorToInt(StatsSystem.Instance.Points));
        }
    }
}
