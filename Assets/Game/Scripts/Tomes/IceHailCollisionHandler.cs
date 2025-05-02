using UnityEngine;

public class IceHailCollisionHandler : MonoBehaviour
{
    private float damage;
    private float slowAmount;
    private float slowDuration;

    public void Initialize(float damageAmount, float slowAmount, float slowDuration)
    {
        damage = damageAmount;
        this.slowAmount = slowAmount; //if enemy hit, then apply slow based on this amount
        this.slowDuration = slowDuration;
    }

    private void OnParticleCollision(GameObject other) //particle are what deal damage
    {
        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);

                enemy.ApplySlow(slowAmount, slowDuration);
            }
        }
    }
}