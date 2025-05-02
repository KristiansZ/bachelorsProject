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

    void Start()
    {
        stats = new TomeStats {
            baseDamage = 60f,
            baseCooldown = 3f,
            range = 30f
        };
    }

    protected override void ExecuteCast(Vector3 targetPosition)
    {
        if (_playerController == null) return;

        Vector3 playerPosition = _playerController.transform.position;
        Vector3 flatTarget = new Vector3(targetPosition.x, playerPosition.y, targetPosition.z);

        float distance = Vector3.Distance(playerPosition, flatTarget);

        //clamp to maxCastDistance
        if (distance > maxCastDistance)
        {
            Vector3 direction = (flatTarget - playerPosition).normalized;
            flatTarget = playerPosition + direction * maxCastDistance;
        }

        _playerController.ForceLookAt(flatTarget);

        Vector3 spawnPosition = flatTarget + Vector3.up * spawnHeight;
        var meteor = Instantiate(meteorPrefab, spawnPosition, Quaternion.identity);
        var meteorProjectile = meteor.GetComponent<MeteoriteProjectile>();

        GameObject effectToUse = createsBurnZone ? burnEffect : null;
        float damageToDeal = createsBurnZone ? burnDamagePerSecond : 0;

        meteorProjectile.SetImpactRadiusMultiplier(impactRadiusMultiplier);

        meteorProjectile.Initialize(_currentDamage, flatTarget, effectToUse, damageToDeal);
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