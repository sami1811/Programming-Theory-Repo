using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private float moveSpeed = 0;
    [SerializeField] private Camera playerCamera;

    [Header("Bound Settings")]
    [SerializeField] private float xBound = 0f;
    [SerializeField] private float zBound = 0f;

    [Header("Player Marker Settings")]
    [SerializeField] private GameObject playerMarkerObj;
    [SerializeField] private float animationSpeed = 0f;

    [SerializeField] private LayerMask groundLayer = 1;

    private void Awake()
    {
        if (playerMarkerObj == null)
        {
            playerMarkerObj = GameObject.Find("PlayerMarker").GetComponent<GameObject>();
        }
    }

    private void Update()
    {
        MovePlayer();
        RotatePlayer();
        AnimatePlayerMarker();
    }

    private void MovePlayer()
    {
        Vector3 playerPos = transform.position;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = transform.forward * vertical + transform.right * horizontal;
        Vector3 velocity = direction * moveSpeed * Time.deltaTime;

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
        if (playerMarkerObj != null)
        {
            float t = Mathf.PingPong(Time.time * animationSpeed, 1f);
            float animateY = Mathf.Lerp(1.5f, 2f, t);
            playerMarkerObj.transform.position = transform.position + new Vector3(0, animateY, 0);
        }
    }
}


/*private void AnimatePlayerMarker()
{
    if (playerMarkerObj != null)
    {
        float t = Mathf.PingPong(Time.time * animationSpeed, 1f);
        float animateY = Mathf.Lerp(1.5f, 2f, t);
        playerMarkerObj.transform.position = transform.position + new Vector3(0, animateY, 0);
    }
}*/