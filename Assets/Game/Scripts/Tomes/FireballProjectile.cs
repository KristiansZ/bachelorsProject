using UnityEngine;

public class FireballProjectile : MonoBehaviour
{
    [Header("Settings")]
    public GameObject impactEffect;
    public AudioClip fireballSFX;
    [Range(0f, 1f)] public float explosionVolume = 0.7f;
    
    private AudioSource audioSource;
    private Rigidbody rb;
    private Vector3 velocity;
    private float damage;
    private Vector3 startPosition;
    private float maxRange;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.enabled = true;
            audioSource.spatialBlend = 1.0f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 5f;
            audioSource.maxDistance = 20f;
        }
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
    }

    void Update()
    {
        float traveledDistance = Vector3.Distance(startPosition, transform.position);
        if (traveledDistance >= maxRange)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Portal")) //if portal then skip
        {
            return;
        }

        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<EnemyController>()?.TakeDamage(damage);

            if (impactEffect != null)
            {
                Instantiate(impactEffect, transform.position, Quaternion.identity);
            }

            PlayFireballSound();
        }
        else if (!other.isTrigger)
        {
            if (impactEffect != null)
            {
                Instantiate(impactEffect, transform.position, Quaternion.identity);
            }
            PlayFireballSound();
        }

        Destroy(gameObject);
    }

    private void PlayFireballSound()
    {
        if (fireballSFX == null) return;

        GameObject tempAudio = new GameObject("TempFireballAudio");
        tempAudio.transform.position = transform.position;

        AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
        tempSource.clip = fireballSFX;
        tempSource.spatialBlend = 1.0f;
        tempSource.rolloffMode = AudioRolloffMode.Linear;
        tempSource.volume = explosionVolume;
        tempSource.minDistance = 1f;
        tempSource.maxDistance = 20f;

        tempSource.Play();

        Destroy(tempAudio, fireballSFX.length);
    }
}
