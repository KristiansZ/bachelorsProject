using UnityEngine;

public class FireballProjectile : MonoBehaviour
{
    [Header("Settings")]
    public GameObject impactEffect;

    private Rigidbody _rb;
    private Vector3 _velocity;
    private float _damage;
    private Vector3 _startPosition;
    private float _maxRange;

    void Awake()
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
    }

    void Update()
    {
        float traveledDistance = Vector3.Distance(_startPosition, transform.position);
        if (traveledDistance >= _maxRange)
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
            other.GetComponent<EnemyController>()?.TakeDamage(_damage);

            if (impactEffect != null)
            {
                Instantiate(impactEffect, transform.position, Quaternion.identity);
            }
        }
        else if (!other.isTrigger)
        {
            if (impactEffect != null)
            {
                Instantiate(impactEffect, transform.position, Quaternion.identity);
            }
        }

        Destroy(gameObject);
    }
}
