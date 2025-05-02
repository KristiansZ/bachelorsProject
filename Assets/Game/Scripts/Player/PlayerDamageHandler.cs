using UnityEngine;
using System.Collections;

public class PlayerDamageHandler : MonoBehaviour
{
    public event System.Action OnDamageTakenEvent;

    [Header("Damage Visual Effects")]
    [SerializeField] private float hitShakeDuration = 0.2f;
    [SerializeField] private float hitShakeIntensity = 0.1f;
    [SerializeField] private Material damageFlashMaterial;
    [SerializeField] private float damageFlashDuration = 0.1f;
    [SerializeField] private GameObject hitVFXPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip[] hitSounds;

    private PlayerStatManager statManager;
    private AudioSource audioSource;
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private Vector3 originalPosition;

    private IEnumerator Start()
    {
        //stat manager has method that actuallt takes hp from player
        while (statManager == null)
        {
            statManager = GetComponent<PlayerStatManager>();
            if (statManager == null)
            {
                statManager = FindObjectOfType<PlayerStatManager>();
            }
            yield return null;
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].material;
        }

        originalPosition = transform.position;
    }

    public void HandleDamage(float damageAmount) //enemy attack in enemyController (attack routine) calls this
    {
        statManager.TakeDamage(damageAmount);
        
        OnDamageTaken();

        OnDamageTakenEvent?.Invoke(); 
    }

    private void OnDamageTaken()
    {
        PlayRandomHitSound();
        StartCoroutine(ShakeOnHit());
        StartCoroutine(FlashOnHit());
        
        if (hitVFXPrefab != null)
        {
            GameObject vfx = Instantiate(hitVFXPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }
    }

    private void PlayRandomHitSound()
    {
        if (hitSounds != null && hitSounds.Length > 0 && audioSource != null)
        {
            AudioClip randomHitSound = hitSounds[Random.Range(0, hitSounds.Length)];
            audioSource.PlayOneShot(randomHitSound);
        }
    }

    private IEnumerator ShakeOnHit()
    {
        float elapsed = 0f;
        
        while (elapsed < hitShakeDuration)
        {
            float x = Random.Range(-1f, 1f) * hitShakeIntensity;
            float z = Random.Range(-1f, 1f) * hitShakeIntensity;
            
            transform.position = originalPosition + new Vector3(x, 0f, z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPosition;
    }

    private IEnumerator FlashOnHit()
    {
        if (damageFlashMaterial != null)
        {
            //change material of player to display damage taken
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material = damageFlashMaterial;
            }
            
            yield return new WaitForSeconds(damageFlashDuration);
            
            //restore original materials
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material = originalMaterials[i];
            }
        }
    }
}