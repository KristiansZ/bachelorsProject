using UnityEngine;
using System.Collections.Generic;

public class BurnZoneEffect : MonoBehaviour
{
    [Header("Burn Zone Settings")]
    public float damagePerSecond = 30f;
    public float burnRadius = 3f;
    
    private Dictionary<EnemyController, float> _burningEnemies = new Dictionary<EnemyController, float>();
    private float _lastDamageTime;
    private float _damageInterval = 0.5f;

    void Start()
    {
        _lastDamageTime = Time.time;
    }

    void Update()
    {
        //find all enemies in the burn radius
        Collider[] hits = Physics.OverlapSphere(transform.position, burnRadius);
        
        //keep checking which enemies are still in range
        Dictionary<EnemyController, float> stillInRange = new Dictionary<EnemyController, float>();
        
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyController enemy = hit.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    //if this enemy was already burning, keep their burn time
                    float burnTime = 0f;
                    _burningEnemies.TryGetValue(enemy, out burnTime);
                    stillInRange[enemy] = burnTime;
                }
            }
        }
        
        _burningEnemies = stillInRange;
        
        //apply damage at the specified interval
        if (Time.time >= _lastDamageTime + _damageInterval)
        {
            ApplyBurnDamage();
            _lastDamageTime = Time.time;
        }
    }
    
    void ApplyBurnDamage()
    {
        float damageAmount = damagePerSecond * _damageInterval;
        
        foreach (var enemy in _burningEnemies.Keys)
        {
            enemy.TakeDamage(damageAmount);
        }
    }
}