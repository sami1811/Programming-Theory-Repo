using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;

    private EnemyManager enemyManager;
    private GameObject targetObj;

    private void Start()
    {
        if(enemyManager == null)
        {
            enemyManager = GetComponentInParent<EnemyManager>();

            if(enemyManager == null)
            {
                Debug.LogError("No enemy manager found!");
            }

            targetObj = enemyManager.targetObject;
            Debug.Log($"[Enemy Controller] Target found!!!");
        }
    }

    private void Update()
    {
        moveEnemy();
    }

    private void moveEnemy()
    {
        if (targetObj != null)
        {
            Vector3 targetPos = targetObj.transform.position;
            Vector3 newPos = new Vector3(targetPos.x, transform.position.y, targetPos.z);
            Vector3 newDir = (newPos - transform.position).normalized;
            Vector3 moveTo = newDir * moveSpeed * Time.deltaTime;

            transform.position += moveTo;
        }
    }
}
