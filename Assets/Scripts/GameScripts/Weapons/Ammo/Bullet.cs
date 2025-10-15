using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Properties")]
    [SerializeField] private float lifeTime;
    [SerializeField] private float speed;
    [SerializeField] private Rigidbody bulletRigidbody;
    
    [Header("Collision Detection")]
    [SerializeField] private LayerMask hitLayers; // All layers bullet can collide with
    
    private Transform _bulletTransform;
    private bool _hasHit;
    
    public float BulletSpeed => speed;

    void Awake()
    {
        _bulletTransform = transform;
        bulletRigidbody = GetComponent<Rigidbody>();
        
        // Configure rigidbody for bullet behavior
        if (bulletRigidbody)
        {
            bulletRigidbody.useGravity = false;
            bulletRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }

    public void Initialize(Vector3 spawnPosition, Quaternion spawnRotation, Vector3 direction)
    {
        _bulletTransform.position = spawnPosition;
        _bulletTransform.rotation = spawnRotation;
        _hasHit = false;

        // Set velocity using rigidbody
        if (bulletRigidbody)
        {
            bulletRigidbody.linearVelocity = direction.normalized * speed;
        }

        // Schedule return to pool after lifetime
        Invoke(nameof(OnPoolReturn), lifeTime);
    }

    void OnEnable()
    {
        _hasHit = false;
    }

    void FixedUpdate()
    {
        // Rotate bullet to face movement direction
        if (bulletRigidbody && bulletRigidbody.linearVelocity != Vector3.zero)
        {
            _bulletTransform.rotation = Quaternion.LookRotation(bulletRigidbody.linearVelocity);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (_hasHit) return;

        // Check if the collided object is on a hittable layer
        if (IsInLayerMask(other.gameObject.layer, hitLayers))
        {
            _hasHit = true;
            
            // Stop bullet movement
            if (bulletRigidbody)
            {
                bulletRigidbody.linearVelocity = Vector3.zero;
            }

            // Return to pool after hit
            OnPoolReturn();
        }
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return layerMask == (layerMask | (1 << layer));
    }

    private void OnPoolReturn()
    {
        CancelInvoke();
        
        _hasHit = false;
        
        if (bulletRigidbody)
        {
            bulletRigidbody.linearVelocity = Vector3.zero;
        }

        if (BulletManager.Instance)
        {
            BulletManager.Instance.ReturnToPool(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void OnDisable()
    {
        CancelInvoke();
        
        if (bulletRigidbody)
        {
            bulletRigidbody.linearVelocity = Vector3.zero;
        }
    }
}