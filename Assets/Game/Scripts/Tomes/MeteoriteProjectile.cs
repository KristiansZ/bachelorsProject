using UnityEngine;
using System.Linq;

public class MeteoriteProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float damage = 150f;
    public float lifetime = 5f;
    public float fallSpeed = 15f;
    public float impactRadius = 3f;
    public GameObject impactEffect;
    
    [Header("Trajectory Settings")]
    [Range(15f, 60f)] public float entryAngleVariance = 30f;
    
    [Header("Burn Settings")]
    public bool leavesBurningGround = false;
    public GameObject burnEffect;
    public float burnDuration = 4f;
    public float burnDamagePerSecond = 15f;
    public AudioClip burningSoundEffect;  //added direct reference for burn sound
    
    [Header("Sound Effects")]
    public AudioClip meteorExplosionSFX;
    [Range(0f, 1f)] public float explosionVolume = 0.7f;
    [Range(0f, 1f)] public float explosionTimeOffset = 0.1f;  //skip empty part at beginning
    private AudioSource audioSource;

    private Vector3 targetPosition;
    private Vector3 startPosition;
    private Vector3 direction;
    private bool initialized = false;
    private float journeyLength;
    private float startTime;

    public void Initialize(float damageAmount, Vector3 targetPos, GameObject burnPrefab, float burnDPS = 0, AudioClip burnSFX = null)
    {
        damage = damageAmount;
        targetPosition = targetPos;
        burnEffect = burnPrefab;
        burnDamagePerSecond = burnDPS;
        burningSoundEffect = burnSFX;  //store burn SFX directly
        
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
        startPosition = targetPosition + horizontalOffset + Vector3.up * (randomDistance * 1.5f);
        transform.position = startPosition;
        
        direction = (targetPosition - startPosition).normalized;
        
        //random rotation to the meteorite itself
        transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 0, Random.Range(0, 360f));
        
        journeyLength = Vector3.Distance(startPosition, targetPosition);
        startTime = Time.time;
        initialized = true;

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
        
        //audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = 30f;
    }

    void Update()
    {
        if (!initialized) return;

        //move toward target along the calculated direction
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            fallSpeed * Time.deltaTime
        );
        
        //keep the meteorite facing its movement direction
        transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 0, Time.time * 100f);

        //check for impact
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            OnImpact();
        }
    }

    void OnImpact()
    {
        if (meteorExplosionSFX != null)
        {
            //create a temporary audio source to play the sound because the meteor is destrpoyed
            GameObject tempAudio = new GameObject("TempExplosionAudio");
            tempAudio.transform.position = transform.position;
            AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
            tempSource.clip = meteorExplosionSFX;
            tempSource.volume = explosionVolume;
            tempSource.spatialBlend = 1.0f;
            tempSource.time = explosionTimeOffset;  //skip the first 0.1 seconds because its just silence
            tempSource.Play();
            
            //destroy the temporary object after the sound finishes
            Destroy(tempAudio, meteorExplosionSFX.length);
        }
        
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

        if (burnEffect != null && leavesBurningGround) //spawns burn effect
        {
            Vector3 burnSpawnPosition = transform.position;
            if (GetComponent<Collider>() != null)
            {
                float bottomOfObject = transform.position.y - GetComponent<Collider>().bounds.extents.y;
                burnSpawnPosition.y = bottomOfObject + 1.5f;
            }

            GameObject burn = Instantiate(burnEffect, burnSpawnPosition, Quaternion.identity);

            float scale = impactRadius;
            burn.transform.localScale = new Vector3(scale, 1f, scale);

            BurnZoneEffect burnZone = burn.AddComponent<BurnZoneEffect>();
            burnZone.damagePerSecond = burnDamagePerSecond;
            burnZone.burnRadius = impactRadius;
            
            //directly pass the burn sound effect
            if (burningSoundEffect != null)
            {
                burnZone.burningSFX = burningSoundEffect;
            }
            else //fallback
            {
                Debug.LogWarning("No burn sound effect assigned to meteorite");
                MeteoriteTome tome = FindObjectOfType<MeteoriteTome>();
                if (tome != null && tome.burningSFX != null)
                {
                    burnZone.burningSFX = tome.burningSFX;
                    Debug.Log("Using fallback sound from MeteoriteTome");
                }
            }

            Destroy(burn, burnDuration);
        }

        Destroy(gameObject);
    }

    public void SetImpactRadiusMultiplier(float multiplier) //sets impact radius based on multi gained from upgrades
    {
        impactRadius *= multiplier;
    }
}