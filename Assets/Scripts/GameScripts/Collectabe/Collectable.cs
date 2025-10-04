using UnityEngine;

public class Collectable : MonoBehaviour
{
    [Header("Collectable Settings")]
    [SerializeField] private int pointsValue;
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
        if (!healthSystem) return;

        if (!healthSystem.IsAlive())
        {
            CollectableManager.Instance?.AddPoints(pointsValue);
        }
    }
}
