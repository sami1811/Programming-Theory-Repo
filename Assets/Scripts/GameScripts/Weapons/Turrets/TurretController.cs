using UnityEngine;
using System.Collections.Generic;

public class TurretController : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private SphereCollider detectionCollider;
    [SerializeField] private float detectionRange;
    
    [Header("Cleanup Settings")]
    [SerializeField] private float cleanupInterval;
    
    [Header("Rotation Settings")]
    [SerializeField] private Transform turretHead; // The part that rotates
    [SerializeField] private float rotationSpeed; 
    
    [Header("Gun Settings")]
    [SerializeField] private float aimThreshold; // Max angle to consider "aimed"
    [SerializeField] private float bulletSpeed; // How fast your bullets travel
    [SerializeField] private int predictionIterations;
    [SerializeField] private Transform muzzle; // Where bullets spawn
    
    
    private Transform _currentTarget;
    private readonly List<Transform> _enemiesInRange = new List<Transform>();
    private Dictionary<Transform, Vector3> _enemyLastPositions = new Dictionary<Transform, Vector3>();
    
    private float _nextCleanupTime;
    private float _fireInterval; // Time between shots (calculated from fireRate)
    private float _nextFireTime; // When the next shot can be fired
    
    private void Start()
    {
        if (!detectionCollider)
            return;

        if (!detectionCollider.isTrigger)
        {
            detectionCollider.isTrigger = true;
        }
        
        detectionCollider.radius = detectionRange;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        Logger($"[Turret Controller] Trigger Enter: {other.name}, Layer: {other.gameObject.layer}");
    
        if (!IsInLayerMask(other.gameObject, enemyLayer))
        {
            Logger($"[Turret Controller] {other.name} is not on enemy layer!");
            return;
        }
    
        if (!_enemiesInRange.Contains(other.transform))
        {
            _enemiesInRange.Add(other.transform);
            Logger($"[Turret Controller] Added enemy: {other.name}. Total enemies: {_enemiesInRange.Count}");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        _enemiesInRange.Remove(other.transform);
        _enemyLastPositions.Remove(other.transform);
    }
    
    private void Update()
    {
        // Periodic cleanup to prevent memory bloat
        if (Time.time >= _nextCleanupTime)
        {
            CleanupEnemyList();
            _nextCleanupTime = Time.time + cleanupInterval;
        }
        
        // Only search for target if we have enemies
        if (_enemiesInRange.Count > 0)
        {
            _currentTarget = GetClosestEnemy();
            
            if (_currentTarget)
            {
                RotateTowardsTarget(_currentTarget);
                
                if (StatsSystem.Instance)
                {
                    _fireInterval = 1f / StatsSystem.Instance.FireRate;
                    BulletFired(_currentTarget);
                }
            }
        }
        else
        {
            _currentTarget = null;
        }
    }
    
    private Vector3 PredictTargetPosition(Transform target)
    {
        if (!target)
            return Vector3.zero;
    
        // Get enemy's velocity
        var targetVelocity = GetTargetVelocity(target);
    
        // If target isn't moving much, no need to predict
        if (targetVelocity.sqrMagnitude < 0.1f)
        {
            return target.position;
        }
    
        var predictedPosition = target.position;
    
        // Iterate to refine prediction
        for (var i = 0; i < predictionIterations; i++)
        {
            // Calculate time for bullet to reach predicted position
            var distance = Vector3.Distance(muzzle.position, predictedPosition);
            var timeToImpact = distance / bulletSpeed;
        
            // Predict where target will be at that time
            predictedPosition = target.position + (targetVelocity * timeToImpact);
        }
    
        return predictedPosition;
    }
    
    private Vector3 GetTargetVelocity(Transform target)
    {
        if (!_enemyLastPositions.ContainsKey(target))
        {
            _enemyLastPositions[target] = target.position;
            return Vector3.zero;
        }
    
        // Calculate velocity from position change
        var lastPosition = _enemyLastPositions[target];
        var velocity = (target.position - lastPosition) / Time.deltaTime;
    
        // Update stored position for next frame
        _enemyLastPositions[target] = target.position;
    
        return velocity;
    }
    
    void BulletFired(Transform target)
    {
        if (IsAimedAtTarget(target) && Time.time >= _nextFireTime)
        {
            GetBullet();
            _nextFireTime = Time.time + _fireInterval; // Schedule next shot
        }
    }
    
    private bool IsAimedAtTarget(Transform target)
    {
        if (!target) return false;

        // Use predicted position for aim check too!
        var predictedPosition = PredictTargetPosition(target);
        var directionToTarget = predictedPosition - turretHead.position;
        directionToTarget.y = 0;

        if (directionToTarget.sqrMagnitude < 0.001f)
            return false;

        var angle = Vector3.Angle(turretHead.forward, directionToTarget);

        return angle < aimThreshold;
    }
    
    void GetBullet()
    {
        GameObject bullet = BulletManager.Instance.GetObject();
    
        if (bullet)
        {
            var bulletScript = bullet.GetComponent<Bullet>();

            if (bulletScript)
            {
                // Calculate direction to predicted position
                var predictedPosition = PredictTargetPosition(_currentTarget);
                var fireDirection = (predictedPosition - muzzle.position).normalized;
            
                bulletScript.Initialize(muzzle.position, muzzle.rotation, fireDirection);
            }
            else
            {
                Debug.LogWarning("Bullet script not found on pooled object!");
            }
        }
    }
    
    private void CleanupEnemyList()
    {
        var removedCount = _enemiesInRange.RemoveAll(enemy => !enemy || !enemy.gameObject.activeInHierarchy);
    
        // Clean up velocity tracking too
        var keysToRemove = new List<Transform>();
        foreach (var key in _enemyLastPositions.Keys)
        {
            if (!key || !key.gameObject.activeInHierarchy)
            {
                keysToRemove.Add(key);
            }
        }
        foreach (var key in keysToRemove)
        {
            _enemyLastPositions.Remove(key);
        }
    
        if (removedCount > 0)
        {
            Logger($"Cleaned up {removedCount} invalid enemies");
        }
    }
    
    private Transform GetClosestEnemy()
    {
        if (_enemiesInRange.Count == 0)
            return null;
    
        Transform closest = null;
        var closestDistanceSqr = Mathf.Infinity;
        var turretPosition = transform.position;
    
        foreach (var enemy in _enemiesInRange)
        {
            if (!enemy) continue;
        
            var distanceSqr = (enemy.position - turretPosition).sqrMagnitude;
        
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closest = enemy;
            }
        }
    
        return closest;
    }
    
    private void RotateTowardsTarget(Transform target)
    {
        if (!target)
            return;

        // Use predicted position instead of current position!
        var predictedPosition = PredictTargetPosition(target);
    
        // Calculate direction to predicted position
        var direction = predictedPosition - turretHead.position;

        // Flatten the direction (only rotate horizontally)
        direction.y = 0;

        // Check if direction is valid (not zero)
        if (direction.sqrMagnitude < 0.001f) return;

        // Calculate the rotation we want
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Smoothly rotate towards it
        turretHead.rotation = Quaternion.RotateTowards(
            turretHead.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
    
    private static bool IsInLayerMask(GameObject obj, LayerMask layerMask)
    {
        return (layerMask.value & (1 << obj.layer)) > 0;
    }
    
    // Clean up when turret is destroyed
    private void OnDestroy()
    {
        _enemiesInRange.Clear();
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw line to current target
        if (Application.isPlaying && _currentTarget)
        {
            // Green if aimed, red if not
            bool isAimed = IsAimedAtTarget(_currentTarget);
            Gizmos.color = isAimed ? Color.green : Color.red;
            Gizmos.DrawLine(turretHead.position, _currentTarget.position);
            Gizmos.DrawWireSphere(_currentTarget.position, 0.5f);
        }

        // Draw all enemies in range
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            foreach (var enemy in _enemiesInRange)
            {
                if (enemy && enemy != _currentTarget)
                {
                    Gizmos.DrawLine(transform.position, enemy.position);
                }
            }
        }
        
        // Draw predicted position
        if (Application.isPlaying && _currentTarget)
        {
            Vector3 predictedPos = PredictTargetPosition(_currentTarget);
        
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(predictedPos, 0.3f);
            Gizmos.DrawLine(muzzle.position, predictedPos);
        
            // Draw actual vs predicted
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(_currentTarget.position, predictedPos);
        }
    }

    private static void Logger(string message)
    {
#if UNITY_EDITOR
        Debug.Log($"[Turret Controller] {message}");
#endif
    }
}
