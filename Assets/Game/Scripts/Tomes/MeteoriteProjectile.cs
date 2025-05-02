using UnityEngine;

public class MeteoriteProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float damage = 150f;
    public float lifetime = 5f;
    public float fallSpeed = 15f;
    public float impactRadius = 3f;
    public GameObject impactEffect;
    
    [Header("Trajectory Settings")]
    [Range(15f, 60f)] public float entryAngleVariance = 30f; // Range of angle variation from vertical
    
    [Header("Burn Settings")]
    public bool leavesBurningGround = false;
    public GameObject burnEffect;
    public float burnDuration = 4f;
    public float burnDamagePerSecond = 15f;

    private Vector3 _targetPosition;
    private Vector3 _startPosition;
    private Vector3 _direction;
    private bool _initialized = false;
    private float _journeyLength;
    private float _startTime;

    public void Initialize(float damageAmount, Vector3 targetPos, GameObject burnPrefab, float burnDPS = 0)
    {
        damage = damageAmount;
        _targetPosition = targetPos;
        burnEffect = burnPrefab;
        burnDamagePerSecond = burnDPS;
        
        //generate random starting position above and around target
        float randomAngle = Random.Range(0f, 360f); //random angle around target
        float randomDistance = Random.Range(5f, 15f); //random distance
        float entryAngle = Random.Range(-entryAngleVariance, entryAngleVariance); //random entry angle
        
        Vector3 horizontalOffset = new Vector3(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad) * randomDistance,
            0f,
            Mathf.Sin(randomAngle * Mathf.Deg2Rad) * randomDistance
        );
        
        //create a starting position above and offset from the target
        _startPosition = _targetPosition + horizontalOffset + Vector3.up * (randomDistance * 1.5f);
        transform.position = _startPosition;
        
        _direction = (_targetPosition - _startPosition).normalized;
        
        //random rotation to the meteorite itself
        transform.rotation = Quaternion.LookRotation(_direction) * Quaternion.Euler(0, 0, Random.Range(0, 360f));
        
        _journeyLength = Vector3.Distance(_startPosition, _targetPosition);
        _startTime = Time.time;
        _initialized = true;

        Destroy(gameObject, lifetime);
    }

    void Start()
    {
        //collider is a trigger, doesn't apply forces, otherwise enemies just YEEET away (could be funny though)
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    void Update()
    {
        if (!_initialized) return;

        //move toward target along the calculated direction
        transform.position = Vector3.MoveTowards(
            transform.position,
            _targetPosition,
            fallSpeed * Time.deltaTime
        );
        
        //keep the meteorite facing its movement direction
        transform.rotation = Quaternion.LookRotation(_direction) * Quaternion.Euler(0, 0, Time.time * 100f);

        //check for impact
        if (Vector3.Distance(transform.position, _targetPosition) < 0.1f)
        {
            OnImpact();
        }
    }

    void OnImpact()
    {
        // Visual effect
        if (impactEffect != null)
        {
            Instantiate(impactEffect, transform.position, Quaternion.identity);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, impactRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                hit.GetComponent<EnemyController>()?.TakeDamage(damage);
            }
        }

        if (burnEffect != null && leavesBurningGround) //spawns bur effect
        {
            Vector3 burnSpawnPosition = transform.position;
            if (GetComponent<Collider>() != null)
            {
                burnSpawnPosition += Vector3.down * (GetComponent<Collider>().bounds.extents.y);
            }

            GameObject burn = Instantiate(burnEffect, burnSpawnPosition, Quaternion.identity);

            float scale = impactRadius;
            burn.transform.localScale = new Vector3(scale, 1f, scale);

            BurnZoneEffect burnZone = burn.AddComponent<BurnZoneEffect>();
            burnZone.damagePerSecond = burnDamagePerSecond;
            burnZone.burnRadius = impactRadius;

            Destroy(burn, burnDuration);
        }

        Destroy(gameObject);
    }

    public void SetImpactRadiusMultiplier(float multiplier) //sets impact radius based on multi gained from upgrades
    {
        impactRadius *= multiplier;
    }
}