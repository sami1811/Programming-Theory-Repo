using UnityEngine;

public class PlayerControlller : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private float moveSpeed = 0;
    [SerializeField] private float mouseSensitivity = 0;
    [SerializeField] private Camera playerCamera;

    [Header("Bound Settings")]
    [SerializeField] private float xBound = 0f;
    [SerializeField] private float zBound = 0f;

    private float yRotation= 0f;
    private LayerMask groundLayer = 1;

    private void Update()
    {
        MovePlayer();
        RotatePlayer();
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
        /*Quaternion rotatePlayer = transform.rotation;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yRotation += mouseX;
        rotatePlayer = Quaternion.Euler( 0, yRotation, 0);
        transform.rotation = rotatePlayer;*/

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
}
