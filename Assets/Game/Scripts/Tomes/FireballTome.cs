using UnityEngine;

public class FireballTome : Tome
{
    [Header("Fireball Configuration")]
    public float fireballOffset = 1.5f;
    public int extraProjectiles = 0;
    public float spreadAngle = 15f;
    public float scaleMultiplier = 1f;
    public AudioClip fireballSFX;

    private void Start()
    {
        stats = new TomeStats
        {
            baseDamage = 20f,
            baseCooldown = 0.8f,
            projectileSpeed = 15f,
            range = 25f
        };
    }

    protected override void ExecuteCast(Vector3 targetPosition)
    {
        if (playerController == null) return;

        playerController.ForceLookAt(targetPosition);

        Vector3 spawnPos = projectileSpawnPoint != null
            ? projectileSpawnPoint.position
            : playerController.transform.position + Vector3.up * 1.5f + playerController.transform.forward * fireballOffset;

        spawnPos.y = 2f;

        Vector3 direction = (targetPosition - spawnPos).normalized;
        direction.y = 0;
        direction = direction.normalized;

        float finalDamage = stats.baseDamage * (1f + TomeController.Instance.globalDamageBonus);


        LaunchProjectile(spawnPos, direction, finalDamage);

        for (int i = 0; i < extraProjectiles; i++)
        {
            Vector3 spread = Quaternion.Euler(
                0,
                Random.Range(-spreadAngle, spreadAngle),
                0
            ) * direction;

            LaunchProjectile(spawnPos, spread.normalized, finalDamage);
        }
    }

    private void LaunchProjectile(Vector3 spawnPos, Vector3 direction, float damage)
    {
        GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
        projectile.transform.localScale *= scaleMultiplier;

        FireballProjectile fireball = projectile.GetComponent<FireballProjectile>();


        if (fireball != null)
        {
            if (fireballSFX != null)//assign sound effects to the projectile
            {
                fireball.fireballSFX = fireballSFX;
            }

            direction = direction.normalized;
            Vector3 velocity = direction * stats.projectileSpeed;

            fireball.Initialize(damage, velocity, stats.range);
        }
    }

    #region Upgrades
    public void AddProjectile() => extraProjectiles++;
    public void IncreaseSpread(float amount) => spreadAngle += amount; //not yet used
    public void UpgradeProjectileSize(float value) => scaleMultiplier += value;
    #endregion
}
