using UnityEngine;

public class BlankTome : Tome
{
    [Header("RockBall Configuration")]
    public float rockBallOffset = 1.5f; //distance in front of the player for shootign
    public AudioClip rockImpactSFX;

    private void Start()
    {
        stats = new TomeStats
        {
            baseDamage = 10f,
            baseCooldown = 1.2f,
            projectileSpeed = 10f,
            range = 15f
        };
    }

    protected override void ExecuteCast(Vector3 targetPosition)
    {
        if (playerController == null) return;

        playerController.ForceLookAt(targetPosition);

        Vector3 spawnPos = playerController.transform.position + 
                           playerController.transform.forward * rockBallOffset;

        spawnPos.y = 2f; //fixed spawn height

        Vector3 direction = (targetPosition - spawnPos).normalized;
        direction.y = 0; //no vertical movement
        direction = direction.normalized;

        LaunchProjectile(spawnPos, direction);
    }

    private void LaunchProjectile(Vector3 spawnPos, Vector3 direction)
    {
        Quaternion rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);
        GameObject projectile = Instantiate(projectilePrefab, spawnPos, rotation);

        RockBallProjectile rockBall = projectile.GetComponent<RockBallProjectile>();
        if (rockBall != null)
        {
            Vector3 velocity = direction * stats.projectileSpeed;

            rockBall.Initialize(
                currentDamage,
                velocity,
                stats.range
            );

            if (rockImpactSFX != null)
            {
                rockBall.rockImpactSFX = rockImpactSFX;
            }
        }
    }
}
