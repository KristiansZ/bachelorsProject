using UnityEngine;

public class RockBallProjectile : MonoBehaviour
{
    [Header("Settings")]
    public AudioClip rockImpactSFX;
    [Range(0f, 1f)] public float impactVolume = 0.8f;

    private Rigidbody rb;
    private float damage;
    private Vector3 velocity;
    private Vector3 startPosition;
    private float maxRange;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(float damageAmount, Vector3 velocity, float range)
    {
        damage = damageAmount;
        velocity = velocity;
        maxRange = range;
        startPosition = transform.position;

        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }

        Destroy(gameObject, 5f); //fallback
    }

    private void Update()
    {
        float traveledDistance = Vector3.Distance(startPosition, transform.position);
        if (traveledDistance >= maxRange)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Portal"))
            return;

        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<EnemyController>()?.TakeDamage(damage);
        }

        PlayRockImpactSound();

        Destroy(gameObject);
    }

    private void PlayRockImpactSound()
    {
        if (rockImpactSFX == null) return;

        GameObject tempAudio = new GameObject("TempRockImpactAudio");
        tempAudio.transform.position = transform.position;

        AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
        tempSource.clip = rockImpactSFX;
        tempSource.volume = impactVolume;
        tempSource.spatialBlend = 1.0f;
        tempSource.rolloffMode = AudioRolloffMode.Linear;
        tempSource.time = 0.05f;
        tempSource.minDistance = 2f;
        tempSource.maxDistance = 25f;

        tempSource.Play();

        Destroy(tempAudio, rockImpactSFX.length);
    }
}
