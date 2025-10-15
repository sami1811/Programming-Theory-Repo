using UnityEngine;
using UnityEngine.Events;

public class CollectableManager : MonoBehaviour
{
    public static CollectableManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int pointsThreshold;
    [SerializeField] private int maxThresholdPoints;
    [SerializeField] private bool useIncreasingThreshold;
    [SerializeField] private float thresholdMultiplier;
    
    [Header("Events")]
    [SerializeField] private UnityEvent<int> onPointsChanged;
    public UnityEvent onThresholdReached;

    public int CurrentPoints { get; set; }
    
    public int PointsThreshold => pointsThreshold;
    
    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddPoints(int points)
    {
        CurrentPoints += points;
        onPointsChanged?.Invoke(CurrentPoints);
        
        Logger($"Points added: {points}. Total: {CurrentPoints}/{pointsThreshold}");
        
        if (CurrentPoints >= pointsThreshold)
        {
            TriggerUpgradeSelection();
        }
    }

    private void TriggerUpgradeSelection()
    {
        Logger("Triggering upgrade selection");
        onThresholdReached?.Invoke();
    }

    public void ResetPoints()
    {
        CurrentPoints = 0;
        
        if (useIncreasingThreshold)
        {
            var temp = Mathf.RoundToInt(pointsThreshold * thresholdMultiplier);

            if (temp > maxThresholdPoints)
            {
                pointsThreshold = maxThresholdPoints;
                useIncreasingThreshold = false;
            }
            else
            {
                pointsThreshold = temp;
            }
            
            Logger($"Threshold increased to: {pointsThreshold}");
        }
        
        onPointsChanged?.Invoke(CurrentPoints);
    }

    //Optional: Manually set points method
    public void SetPoints(int points)
    {
        CurrentPoints = points;
        onPointsChanged?.Invoke(CurrentPoints);
    }

    public float GetProgressPercentage()
    {
        var percentage = 100 * ((float) CurrentPoints /  pointsThreshold);
        
        return Mathf.Round(Mathf.Min(percentage, 100f));
    }
    
    public void ResetManager()
    {
        CurrentPoints = 0;
        pointsThreshold = 100;
        onPointsChanged?.Invoke(CurrentPoints);
    }
    
    private void Logger(string message)
    {
#if UNITY_EDITOR
        Debug.Log($"[Collectable Manager] {message}");
#endif
    }
}