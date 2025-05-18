using UnityEngine;

public class IceHailTome : Tome
{
    [Header("Ice Hail Configuration")]
    public GameObject hailPrefab;
    public int baseHailCount = 1;
    public float radius = 5f;
    public float slowAmount = 0.4f;
    public float slowDuration = 2f;
    public float maxCastDistance = 10f;

    protected override void Awake()
    {
        stats = new TomeStats {
            baseDamage = 5f,
            baseCooldown = 3f,
            range = 20f
        };
        
        base.Awake(); //call base class awake
    }

    protected override void ExecuteCast(Vector3 centerPosition)
    {
        playerController.ForceLookAt(centerPosition);

        Vector3 playerPosition = playerController.transform.position;
        Vector3 flatTarget = new Vector3(centerPosition.x, playerPosition.y, centerPosition.z);
        float distance = Vector3.Distance(playerPosition, flatTarget);

        //clamp if too far (dont want to attack enemies too far)
        if (distance > maxCastDistance)
        {
            Vector3 direction = (flatTarget - playerPosition).normalized;
            flatTarget = playerPosition + direction * maxCastDistance;
        }

        if (Physics.Raycast(flatTarget + Vector3.up * 10f, Vector3.down, out RaycastHit hit, Mathf.Infinity))
        {
            Vector3 spawnPos = hit.point;

            var hail = Instantiate(hailPrefab, spawnPos, Quaternion.identity);

            var particleSystem = hail.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                var collisionModule = particleSystem.collision;
                collisionModule.enabled = true;
                collisionModule.collidesWith = LayerMask.GetMask("Enemy", "Ground");

                float duration = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax - 0.5f;
                Destroy(hail, duration);
            }

            var collisionHandler = hail.GetComponent<IceHailCollisionHandler>();
            if (collisionHandler != null)
            {
                collisionHandler.Initialize(stats.baseDamage, slowAmount, slowDuration);
            }
        }
    }

    #region Upgrades
    public void IncreaseHailCount(int amount) => baseHailCount += amount; //makes 2 hails (very rare upgrade?)
    public void ImproveSlowAmount(float amount) => slowAmount += amount;
    public void improveSlowDuration(float duration) => slowDuration += duration;
    #endregion
}