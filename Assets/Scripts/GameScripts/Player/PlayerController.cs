using UnityEngine;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    [FormerlySerializedAs("playerStatsSystem")]
    [Header("Player Settings")]
    [SerializeField] private Rigidbody playerRb;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask groundLayer = 1;

    [Header("Bound Settings")]
    [SerializeField] private float xBound;
    [SerializeField] private float zBound;

    [Header("Player Marker Settings")]
    [SerializeField] private GameObject playerMarkerObj;
    [SerializeField] private float animationSpeed;

    [FormerlySerializedAs("maxTiltAngle")]
    [Header("Camera Settings")]
    [SerializeField] private float maxTiltAngleX;
    [SerializeField] private float maxTiltAngleY;
    [FormerlySerializedAs("tiltDistance")]
    [SerializeField] private float tiltDistanceX; 
    [SerializeField] private float tiltDistanceY;
    
    [Header("Gun Settings")]
    [SerializeField] private Transform muzzle;
    
    private Camera _mainCamera;
    private Vector3 _initialPlayerPosition;
    private Quaternion _initialCameraRotation;
    
    private float _initialDistanceX;
    private float _fireInterval; // Time between shots (calculated from fireRate)
    private float _nextFireTime; // When the next shot can be fired
    
    private bool _initialized;
    
    private void Awake()
    {
        InitializeAwake();
    }

    private void Update()
    {
        if (StatsSystem.Instance)
        {
            _fireInterval = 1f / StatsSystem.Instance.FireRate;
            BulletFired();
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
        RotatePlayer();
    }

    private void LateUpdate()
    {
        AnimatePlayerMarker();
        MoveCamera();
    }

    private void InitializeAwake()
    {
        if (!playerRb)
        {
            ErrorLogger("Player rigidbody not found!");
        }
        
        _mainCamera = Camera.main;
        
        // Store initial values once
        if (!_initialized)
        {
            if (_mainCamera)
            {
                _initialPlayerPosition = transform.position;
                _initialDistanceX = Vector3.Distance(transform.position, _mainCamera.transform.position);
                _initialCameraRotation = _mainCamera.transform.rotation;
            }

            _initialized = true;
        }
        
        if (!playerMarkerObj)
        {
            ErrorLogger("PlayerMarkerObj not found!");
        }

        if (!muzzle)
        {
            WarningLogger("Muzzle not found on the gun!");
        }
    }

    private void MoveCamera()
    {
        if (_mainCamera)
        {
            // Calculate forward/backward distance for X-axis tilt
            float currentDistanceZ = Vector3.Distance(transform.position, _mainCamera.transform.position);
            float distanceTraveledZ = currentDistanceZ - _initialDistanceX;
            float tiltAngleX = Mathf.Clamp(distanceTraveledZ / tiltDistanceX, 0f, 1f) * maxTiltAngleX;
        
            // Calculate sideways distance for Y-axis tilt
            Vector3 cameraForward = _mainCamera.transform.forward;
            cameraForward.y = 0; // Project to horizontal plane
            Vector3 cameraRight = Vector3.Cross(Vector3.up, cameraForward).normalized;
            
            Vector3 currentOffset = transform.position - _initialPlayerPosition;
        
            float sidewaysDistance = Vector3.Dot(currentOffset, cameraRight);
            float tiltAngleY = Mathf.Clamp(sidewaysDistance / tiltDistanceY, -1f, 1f) * maxTiltAngleY;
        
            // Apply both tilts to initial rotation
            Vector3 initialEuler = _initialCameraRotation.eulerAngles;
            _mainCamera.transform.rotation = Quaternion.Euler(
                initialEuler.x + tiltAngleX, 
                initialEuler.y + tiltAngleY, 
                initialEuler.z
            );
        }
    }
    
    private void MovePlayer()
    {
        Vector3 playerPos = transform.position;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = transform.forward * vertical + transform.right * horizontal;
        Vector3 velocity = direction * (StatsSystem.Instance.MovementSpeed * Time.deltaTime);

        playerPos += velocity;

        playerPos = new Vector3(
            Mathf.Clamp(playerPos.x, -xBound, xBound),
            Mathf.Clamp(transform.position.y, 1f, 5f),
            Mathf.Clamp(playerPos.z, -zBound, zBound)
            );
        
        playerRb?.MovePosition(playerPos);
    }
    
    void GetBullet()
    {
        GameObject bullet = BulletManager.Instance.GetObject();
        
        if (!ReferenceEquals(bullet, null) && bullet)
        {
            var bulletScript = bullet.GetComponent<Bullet>();

            if (bulletScript)
            {
                bulletScript.Initialize(muzzle.position, muzzle.rotation, muzzle.right);
            }
            else
            {
                Debug.LogWarning("Bullet script not found on pooled object!");
            }
        }
    }
    
    void BulletFired()
    {
        if (Input.GetKey(KeyCode.Mouse0) && Time.time >= _nextFireTime)
        {
            GetBullet();
            _nextFireTime = Time.time + _fireInterval; // Schedule next shot
        }
    }
    
    private void RotatePlayer()
    {
        Vector3 mouseScreenPosition = Input.mousePosition;
        Ray ray = playerCamera.ScreenPointToRay(mouseScreenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            Vector3 direction = (hit.point - transform.position).normalized;
            direction.y = 0; // Keep player upright

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    private void AnimatePlayerMarker()
    {
        if (!playerMarkerObj) return;
        
        float t = Mathf.PingPong(Time.time * animationSpeed, 1f);
        float animateY = Mathf.Lerp(1.5f, 2f, t);
        playerMarkerObj.transform.position = transform.position + new Vector3(0, animateY, 0);
    }
    
    private void ErrorLogger(string message)
    {
#if UNITY_EDITOR
        Debug.LogError($"[Payer Controller] {message}");
#endif
    }
    
    private void Logger(string message)
    {
#if UNITY_EDITOR
        Debug.Log($"[Payer Controller] {message}");
#endif
    }
    
    private void WarningLogger(string message)
    {
#if UNITY_EDITOR
        Debug.LogWarning($"[Payer Controller] {message}");
#endif
    }
}