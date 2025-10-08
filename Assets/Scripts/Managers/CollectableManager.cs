using UnityEngine;
using UnityEngine.Events;

public class CollectableManager : MonoBehaviour
{
    public static CollectableManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int pointsThreshold;
    [SerializeField] private bool useIncreasingThreshold;
    [SerializeField] private float thresholdMultiplier;
    
    [Header("Events")]
    [SerializeField] private UnityEvent<int> onPointsChanged;
    public UnityEvent onThresholdReached;
    
    private int _currentPoints;
    
    public int CurrentPoints => _currentPoints;
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
        _currentPoints += points;
        onPointsChanged?.Invoke(_currentPoints);
        
        Logger($"Points added: {points}. Total: {_currentPoints}/{pointsThreshold}");
        
        if (_currentPoints >= pointsThreshold)
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
        _currentPoints = 0;
        
        if (useIncreasingThreshold)
        {
            pointsThreshold = Mathf.RoundToInt(pointsThreshold * thresholdMultiplier);
            Logger($"Threshold increased to: {pointsThreshold}");
        }
        
        onPointsChanged?.Invoke(_currentPoints);
    }

    //Optional: Manually set points method
    public void SetPoints(int points)
    {
        _currentPoints = points;
        onPointsChanged?.Invoke(_currentPoints);
    }

    public float GetProgressPercentage()
    {
        var percentage = 100 * ((float) _currentPoints /  pointsThreshold);
        
        return Mathf.Round(Mathf.Min(percentage, 100f));
    }
    
    public void ResetManager()
    {
        _currentPoints = 0;
        pointsThreshold = 100;
        onPointsChanged?.Invoke(_currentPoints);
    }
    
    private void Logger(string message)
    {
#if UNITY_EDITOR
        Debug.Log($"[Collectable Manager] {message}");
#endif
    }
}