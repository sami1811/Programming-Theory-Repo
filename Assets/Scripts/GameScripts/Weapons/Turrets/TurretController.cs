using UnityEngine;
using System.Collections.Generic;

public class TurretController : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private SphereCollider detectionCollider;
    [SerializeField] private float detectionRange;
    [SerializeField] private float velocitySmoothingFactor; // 0-1, lower = smoother
    
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
    private readonly Dictionary<Transform, Vector3> _enemyLastPositions = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, Vector3> _enemySmoothedVelocities = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, float> _enemyLastUpdateTime = new Dictionary<Transform, float>();
    
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
        ValidateBulletSpeed();
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
        _enemySmoothedVelocities.Remove(other.transform);
        _enemyLastUpdateTime.Remove(other.transform);
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

        // Calculate intercept point
        var toTarget = target.position - muzzle.position;
        var a = targetVelocity.sqrMagnitude - (bulletSpeed * bulletSpeed);
        var b = 2f * Vector3.Dot(targetVelocity, toTarget);
        var c = toTarget.sqrMagnitude;

        // Solve quadratic equation
        var discriminant = b * b - 4f * a * c;

        // If no solution, use iterative method as fallback
        if (discriminant < 0 || Mathf.Approximately(a, 0f))
        {
            return PredictTargetPositionIterative(target, targetVelocity);
        }

        var t1 = (-b + Mathf.Sqrt(discriminant)) / (2f * a);
        var t2 = (-b - Mathf.Sqrt(discriminant)) / (2f * a);

        // Use the smallest positive time
        var timeToImpact = Mathf.Min(t1, t2);
        
        if (timeToImpact < 0)
        {
            timeToImpact = Mathf.Max(t1, t2);
        }

        // If still negative, use iterative method
        if (timeToImpact < 0)
        {
            return PredictTargetPositionIterative(target, targetVelocity);
        }

        // Calculate predicted position
        var predictedPosition = target.position + (targetVelocity * timeToImpact);

        return predictedPosition;
    }
    
    private Vector3 PredictTargetPositionIterative(Transform target, Vector3 targetVelocity)
    {
        var predictedPosition = target.position;

        for (var i = 0; i < predictionIterations; i++)
        {
            var distance = Vector3.Distance(muzzle.position, predictedPosition);
            var timeToImpact = distance / bulletSpeed;
            predictedPosition = target.position + (targetVelocity * timeToImpact);
        }

        return predictedPosition;
    }
    
    private Vector3 GetTargetVelocity(Transform target)
    {
        var currentTime = Time.time;
    
        // First time seeing this enemy
        if (!_enemyLastPositions.TryGetValue(target, out var lastPosition))
        {
            _enemyLastPositions[target] = target.position;
            _enemySmoothedVelocities[target] = Vector3.zero;
            _enemyLastUpdateTime[target] = currentTime;
            return Vector3.zero;
        }
    
        // Calculate actual velocity
        var deltaTime = currentTime - _enemyLastUpdateTime[target];
    
        // Prevent division by zero
        if (deltaTime < 0.0001f)
        {
            return _enemySmoothedVelocities[target];
        }
    
        var instantVelocity = (target.position - lastPosition) / deltaTime;
    
        // Smooth the velocity using exponential moving average
        var smoothedVelocity = Vector3.Lerp(
            _enemySmoothedVelocities[target], 
            instantVelocity, 
            velocitySmoothingFactor
        );
    
        // Update tracking data
        _enemyLastPositions[target] = target.position;
        _enemySmoothedVelocities[target] = smoothedVelocity;
        _enemyLastUpdateTime[target] = currentTime;
    
        return smoothedVelocity;
    }
    
    void BulletFired(Transform target)
    {
        // Check if target is still valid and alive
        if (!target || !target.gameObject.activeInHierarchy)
        {
            _currentTarget = null;
            return;
        }
    
        if (IsAimedAtTarget(target) && Time.time >= _nextFireTime)
        {
            GetBullet();
            _nextFireTime = Time.time + _fireInterval;
        }
    }
    
    private void ValidateBulletSpeed()
    {
        if (bulletSpeed <= 0)
        {
            Debug.LogError("Bullet speed must be greater than 0!", this);
            bulletSpeed = 20f; // Fallback
        }
    
        // Test bullet speed matches actual bullet
        if (BulletManager.Instance)
        {
            var testBullet = BulletManager.Instance.GetObject();
            if (testBullet)
            {
                var bulletScript = testBullet.GetComponentInChildren<Bullet>();
                
                if (bulletScript)
                {
                    var actualSpeed = bulletScript.BulletSpeed;
                    
                    if (Mathf.Abs(actualSpeed - bulletSpeed) > 0.1f)
                    {
#if UNITY_EDITOR
                        Debug.LogWarning($"Turret bulletSpeed ({bulletSpeed}) doesn't match actual bullet speed ({actualSpeed})!");
#endif
                    }
                }
                BulletManager.Instance.ReturnToPool(testBullet);
            }
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

        // Clean up all tracking dictionaries
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
            _enemySmoothedVelocities.Remove(key);
            _enemyLastUpdateTime.Remove(key);
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

        if (Application.isPlaying && _currentTarget)
        {
            // Draw enemy velocity vector
            if (_enemySmoothedVelocities.TryGetValue(_currentTarget, out Vector3 velocity))
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(_currentTarget.position, velocity);
                Gizmos.DrawWireSphere(_currentTarget.position + velocity, 0.2f);
            }
        
            // Draw predicted position
            Vector3 predictedPos = PredictTargetPosition(_currentTarget);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(predictedPos, 0.4f);
        
            // Draw firing line
            Gizmos.color = IsAimedAtTarget(_currentTarget) ? Color.green : Color.red;
            Gizmos.DrawLine(muzzle.position, predictedPos);
        
            // Draw actual enemy position
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(_currentTarget.position, 0.3f);
        
            // Draw lead amount
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(_currentTarget.position, predictedPos);
        }

        // Draw all enemies in range
        if (Application.isPlaying)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            foreach (var enemy in _enemiesInRange)
            {
                if (enemy && enemy != _currentTarget)
                {
                    Gizmos.DrawLine(transform.position, enemy.position);
                }
            }
        }
        
        // Draw aim cone
        if (turretHead)
        {
            Gizmos.color = new Color(1, 1, 0, 0.2f); // Transparent yellow
        
            // Right edge of cone
            Vector3 rightDir = Quaternion.Euler(0, aimThreshold, 0) * turretHead.forward;
            Gizmos.DrawRay(turretHead.position, rightDir * detectionRange);
        
            // Left edge of cone
            Vector3 leftDir = Quaternion.Euler(0, -aimThreshold, 0) * turretHead.forward;
            Gizmos.DrawRay(turretHead.position, leftDir * detectionRange);
        
            // Draw arc (optional, looks cool!)
            for (int i = 0; i <= 10; i++)
            {
                float angle = Mathf.Lerp(-aimThreshold, aimThreshold, i / 10f);
                Vector3 dir = Quaternion.Euler(0, angle, 0) * turretHead.forward;
                Gizmos.DrawRay(turretHead.position, dir * (detectionRange * 0.8f));
            }
        }
    }

    private static void Logger(string message)
    {
#if UNITY_EDITOR
        Debug.Log($"[Turret Controller] {message}");
#endif
    }
}
