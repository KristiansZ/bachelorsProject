using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BurnZoneEffect : MonoBehaviour
{
    [Header("Burn Zone Settings")]
    public float damagePerSecond = 30f;
    public float burnRadius = 3f;
    
    [Header("Sound Effects")]
    public AudioClip burningSFX;
    [Range(0f, 1f)] public float burnVolume = 0.5f;
    
    private Dictionary<EnemyController, float> burningEnemies = new Dictionary<EnemyController, float>();
    private float lastDamageTime;
    private float damageInterval = 0.5f;
    private AudioSource audioSource;
    
    void Start()
    {
        lastDamageTime = Time.time;
        
        //audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = burningSFX;
        audioSource.loop = true;
        audioSource.volume = 0.75f;
        audioSource.spatialBlend = 0.8f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.priority = 0;
        
        if (burningSFX != null)
        {
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("No burning sound assigned");
        }
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
                    burningEnemies.TryGetValue(enemy, out burnTime);
                    stillInRange[enemy] = burnTime;
                }
            }
        }
        
        burningEnemies = stillInRange;
        
        //apply damage at the specified interval
        if (Time.time >= lastDamageTime + damageInterval)
        {
            ApplyBurnDamage();
            lastDamageTime = Time.time;
        }
    }
    
    void ApplyBurnDamage()
    {
        float damageAmount = damagePerSecond * damageInterval;
        
        foreach (var enemy in burningEnemies.Keys)
        {
            enemy.TakeDamage(damageAmount);
        }
    }
}