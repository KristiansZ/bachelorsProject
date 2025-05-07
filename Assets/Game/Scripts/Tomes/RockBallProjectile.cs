using UnityEngine;

public class RockBallProjectile : MonoBehaviour
{
    [Header("Settings")]
    public AudioClip rockImpactSFX;
    [Range(0f, 1f)] public float impactVolume = 0.8f;

    private Rigidbody _rb;
    private float _damage;
    private Vector3 _velocity;
    private Vector3 _startPosition;
    private float _maxRange;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void Initialize(float damageAmount, Vector3 velocity, float range)
    {
        _damage = damageAmount;
        _velocity = velocity;
        _maxRange = range;
        _startPosition = transform.position;

        if (_rb != null)
        {
            _rb.linearVelocity = _velocity;
        }

        Destroy(gameObject, 5f); //fallback
    }

    private void Update()
    {
        float traveledDistance = Vector3.Distance(_startPosition, transform.position);
        if (traveledDistance >= _maxRange)
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
            other.GetComponent<EnemyController>()?.TakeDamage(_damage);
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
