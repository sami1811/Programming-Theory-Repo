using UnityEngine;

public class MyTree : HealthSystem
{
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player") && IsAlive())
        {
            TakeDamage(damagePerHit);
        }
    }
}
