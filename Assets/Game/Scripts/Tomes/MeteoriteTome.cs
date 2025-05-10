using UnityEngine;

public class MeteoriteTome : Tome
{
    [Header("Meteorite Configuration")]
    public GameObject meteorPrefab;
    public float spawnHeight = 25f;
    public bool createsBurnZone = false;
    public GameObject burnEffect;
    public float burnDamagePerSecond = 15f;
    public float maxCastDistance = 10f;
    public float impactRadiusMultiplier = 1f;
    
    [Header("Sound Effects")]
    public AudioClip meteorExplosionSFX;
    public AudioClip burningSFX;

    void Start()
    {
        stats = new TomeStats {
            baseDamage = 60f,
            baseCooldown = 3f,
            range = 30f
        };
        
        //verify sound references
        if (burningSFX == null)
        {
            Debug.LogWarning("burningSFX not assigned");
        }
        
        if (meteorExplosionSFX == null)
        {
            Debug.LogWarning("meteorExplosionSFX not assigned");
        }
    }

    protected override void ExecuteCast(Vector3 targetPosition)
    {
        if (playerController == null) return;

        Vector3 playerPosition = playerController.transform.position;
        Vector3 flatTarget = new Vector3(targetPosition.x, playerPosition.y, targetPosition.z);

        float distance = Vector3.Distance(playerPosition, flatTarget);

        //clamp to maxCastDistance
        if (distance > maxCastDistance)
        {
            Vector3 direction = (flatTarget - playerPosition).normalized;
            flatTarget = playerPosition + direction * maxCastDistance;
        }

        playerController.ForceLookAt(flatTarget);

        Vector3 spawnPosition = flatTarget + Vector3.up * spawnHeight;
        var meteor = Instantiate(meteorPrefab, spawnPosition, Quaternion.identity);
        var meteorProjectile = meteor.GetComponent<MeteoriteProjectile>();
        
        if (meteorExplosionSFX != null)//assign sound effects to the projectile
        {
            meteorProjectile.meteorExplosionSFX = meteorExplosionSFX;
        }

        //set leavesBurningGround directly on the projectile
        meteorProjectile.leavesBurningGround = createsBurnZone;
        
        meteorProjectile.SetImpactRadiusMultiplier(impactRadiusMultiplier);
        meteorProjectile.Initialize(currentDamage, flatTarget, burnEffect, burnDamagePerSecond, burningSFX);
    }

    public void EnableBurnZone(GameObject effectPrefab, float damage = 30f)
    {
        createsBurnZone = true;
        burnEffect = effectPrefab;
        burnDamagePerSecond = damage;
    }

    #region Upgrades
    public void UpgradeImpactRadius(float value) => impactRadiusMultiplier += value;
    public void UpgradeBurnDamage(float value) => burnDamagePerSecond += value;
    #endregion
}