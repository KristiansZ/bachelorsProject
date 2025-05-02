using UnityEngine;

public class RockBallProjectile : MonoBehaviour
{
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

        //fallback destroy if it never hits anything and doesnt pass max range
        Destroy(gameObject, 5f);
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
        {
            return;
        }
        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<EnemyController>()?.TakeDamage(_damage);
        }
        Destroy(gameObject);
    }
}