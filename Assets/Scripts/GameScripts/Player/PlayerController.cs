using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask groundLayer = 1;

    [Header("Bound Settings")]
    [SerializeField] private float xBound;
    [SerializeField] private float zBound;

    [Header("Player Marker Settings")]
    [SerializeField] private GameObject playerMarkerObj;
    [SerializeField] private float animationSpeed;

    [Header("Camera Settings")]
    [SerializeField] private float maxTiltAngle; // Maximum downward tilt
    [SerializeField] private float tiltDistance; 
    
    private Camera _mainCamera;
    private float _initialDistance;
    private bool _initialized;
    private Quaternion _initialCameraRotation;
    
    private void Awake()
    {
        InitializeAwake();
    }

    private void Update()
    {
        MovePlayer();
        RotatePlayer();
        AnimatePlayerMarker();
    }

    private void LateUpdate()
    {
        MoveCamera();
    }

    private void InitializeAwake()
    {
        _mainCamera = Camera.main;
        
        // Store initial values once
        if (!_initialized)
        {
            if (_mainCamera)
            {
                _initialDistance = Vector3.Distance(transform.position, _mainCamera.transform.position);
                _initialCameraRotation = _mainCamera.transform.rotation;
            }

            _initialized = true;
        }
        
        if (!playerMarkerObj)
        {
            playerMarkerObj = GameObject.Find("PlayerMarker").GetComponent<GameObject>();
        }
    }

    private void MoveCamera()
    {
        if (_mainCamera)
        {
            float currentDistance = Vector3.Distance(transform.position, _mainCamera.transform.position);
            float distanceTraveled = currentDistance - _initialDistance;
        
            float tiltAngle = Mathf.Clamp(distanceTraveled / tiltDistance, 0f, 1f) * maxTiltAngle;
        
            // Add tilt to the initial rotation
            Vector3 initialEuler = _initialCameraRotation.eulerAngles;
            _mainCamera.transform.rotation = Quaternion.Euler(initialEuler.x + tiltAngle, initialEuler.y, initialEuler.z);
        }
    }
    
    private void MovePlayer()
    {
        Vector3 playerPos = transform.position;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = transform.forward * vertical + transform.right * horizontal;
        Vector3 velocity = direction * (moveSpeed * Time.deltaTime);

        playerPos += velocity;

        playerPos = new Vector3(
            Mathf.Clamp(playerPos.x, -xBound, xBound),
            transform.position.y,
            Mathf.Clamp(playerPos.z, -zBound, zBound)
            );

        transform.position = playerPos;
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
}