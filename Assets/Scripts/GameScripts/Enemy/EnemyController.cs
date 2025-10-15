using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private float _moveSpeed;
    private const float MinMoveSpeed = 1.5f;
    private const float MaxMoveSpeed = 5f;

    private EnemyManager _enemyManager;
    private GameObject _targetObj;

    private void Start()
    {
        if(!_enemyManager)
        {
            _enemyManager = GetComponentInParent<EnemyManager>();

            if(!_enemyManager)
            {
                Debug.LogError("No enemy manager found!");
            }

            _moveSpeed = Random.Range(MinMoveSpeed, MaxMoveSpeed);
            _targetObj = _enemyManager.targetObject;
        }
    }

    private void Update()
    {
        MoveEnemy();
    }

    private void MoveEnemy()
    {
        if (_targetObj)
        {
            var targetPos = _targetObj.transform.position;
            var newPos = new Vector3(targetPos.x, transform.position.y, targetPos.z);
            var newDir = (newPos - transform.position).normalized;
            var moveTo = newDir * (_moveSpeed * Time.deltaTime);

            transform.position += moveTo;
        }
    }
}
