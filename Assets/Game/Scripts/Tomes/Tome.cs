using UnityEngine;
using System.Collections;

public abstract class Tome : MonoBehaviour
{
    public TomeType tomeType;
    public Sprite iconSprite;
    public CastMode castMode = CastMode.Directional;

    public GameObject projectilePrefab;
    public float damageMultiplier = 1f;

    [Header("Casting Behavior")]
    public bool enableContinuousCasting = true;
    
    [Range(0.1f, 1.0f)]
    public float projectileDelayPercent = 0.2f;

    private float baseDirectionalAnimDuration = 2.2f;
    private float baseTargetedAnimDuration = 2.6f;

    public Sprite GetIcon() => iconSprite;

    [System.Serializable]
    public class TomeStats
    {
        public float baseDamage;
        public float baseCooldown;
        public float projectileSpeed;
        public float range = 20f;
    }

    [Header("Base Configuration")]
    public TomeStats stats;
    //might want later     public AudioClip castSound; 
    public ParticleSystem castParticles;

    protected float currentDamage;
    protected float currentCooldown;
    protected float nextCastTime;
    protected float remainingCooldown;
    protected AudioSource audioSource;
    protected Transform playerCamera;
    protected Transform projectileSpawnPoint;
    protected PlayerController playerController;
    protected PlayerAnimationManager animationManager;
    protected Vector3 pendingTargetPosition;
    protected bool hasPendingCast = false;
    protected bool isAnimationInProgress = false;

    protected virtual void Awake()
    {
        if (stats == null)
        {
            stats = new TomeStats
            {
                baseDamage = 20f,
                baseCooldown = 1f,
                projectileSpeed = 15f,
                range = 20f
            };
        }

        currentDamage = stats.baseDamage * damageMultiplier;
        currentCooldown = stats.baseCooldown;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        playerCamera = Camera.main?.transform;
        playerController = GetComponentInParent<PlayerController>();
        
        animationManager = GetComponentInParent<PlayerAnimationManager>();
        if (animationManager == null)
        {
            animationManager = playerController?.GetComponent<PlayerAnimationManager>();
        }
        if (animationManager == null)
        {
            Debug.LogWarning("No PlayerAnimationManager found. Animations won't play.");
        }
    }

    protected virtual void Update()
    {
        if (remainingCooldown > 0)
        {
            remainingCooldown = nextCastTime - Time.time;
        }

        if (!isAnimationInProgress)
        {
            if (enableContinuousCasting) //can hold M1 to cast
            {
                if (Input.GetMouseButton(0)) TryCastAtPosition();
            }
            else
            {
                if (Input.GetMouseButtonDown(0)) TryCastAtPosition();
            }
        }
    }

    protected void TryCastAtPosition()
    {
        if (Time.time < nextCastTime || (animationManager != null && animationManager.IsAttacking()))
            return;

        Ray ray = playerCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);

        switch (castMode)
        {
            case CastMode.Directional:
                if (Physics.Raycast(ray, out RaycastHit hitDir, 100f))
                {
                    Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : playerController.transform.position;
                    Vector3 targetDirection = (hitDir.point - spawnPos).normalized;
                    targetDirection.y = 0;
                    
                    float animSpeedMultiplier = baseDirectionalAnimDuration / (currentCooldown - 0.05f);
                    
                    pendingTargetPosition = spawnPos + targetDirection * stats.range;
                    hasPendingCast = true;
                    isAnimationInProgress = true;
                    
                    if (animationManager != null)
                    {
                        playerController.ForceLookAt(hitDir.point);
                        animationManager.PlayDirectionalAttack(animSpeedMultiplier);
                        
                        StartCoroutine(DelayedProjectileLaunch());
                    }
                    else
                    {
                        Debug.LogWarning("Cannot play animation: animationManager is null");

                        ExecuteCast(pendingTargetPosition);
                        PlayEffects();
                        isAnimationInProgress = false;
                    }
                    
                    nextCastTime = Time.time + currentCooldown;
                    remainingCooldown = currentCooldown;
                }
                break;

            case CastMode.Targeted:
                if (Physics.Raycast(ray, out RaycastHit hitTargeted, stats.range))
                {
                    float animSpeedMultiplier = baseTargetedAnimDuration / currentCooldown;
                    if (animSpeedMultiplier < 1.5f)
                    {
                        animSpeedMultiplier = 1.5f;
                    }
                    
                    pendingTargetPosition = hitTargeted.point;
                    hasPendingCast = true;
                    isAnimationInProgress = true;
                    
                    if (animationManager != null)
                    {
                        playerController.ForceLookAt(hitTargeted.point);
                        animationManager.PlayTargetedAttack(animSpeedMultiplier);
                        
                        StartCoroutine(DelayedProjectileLaunch());
                    }
                    else
                    {
                        Debug.LogWarning("Cannot play animation: animationManager is null");
                        ExecuteCast(pendingTargetPosition);
                        PlayEffects();
                        isAnimationInProgress = false;
                    }
                    
                    nextCastTime = Time.time + currentCooldown;
                    remainingCooldown = currentCooldown;
                }
                break;
        }
    }
    
    protected IEnumerator DelayedProjectileLaunch()
    {
        if (!hasPendingCast) yield break;
        
        float adjustedAnimLength = 0;
        
        if (castMode == CastMode.Directional)
        {
            adjustedAnimLength = currentCooldown * projectileDelayPercent;
        }
        
        yield return new WaitForSeconds(adjustedAnimLength);
        
        if (hasPendingCast)
        {
            ExecuteCast(pendingTargetPosition);
            PlayEffects();
            hasPendingCast = false;
        }
        
        yield return new WaitForSeconds(currentCooldown - adjustedAnimLength - 0.05f);
        isAnimationInProgress = false;
    }

    protected void PlayEffects()
    {
        //audioSource?.PlayOneShot(castSound);
        castParticles?.Play();
    }

    public virtual void OnTomeEquipped()
    {
        isAnimationInProgress = false;
        hasPendingCast = false;
        nextCastTime = 0f;
        remainingCooldown = 0f;
    }

    protected abstract void ExecuteCast(Vector3 targetPosition);

    #region Upgrades
    public virtual void UpgradeDamage(float percent) 
    {
        currentDamage = currentDamage * (1 + percent) * damageMultiplier;
        stats.baseDamage = currentDamage;
    }

    public virtual void UpgradeCooldown(float percent) 
    {
        currentCooldown = currentCooldown * (1 - Mathf.Clamp01(percent));
        stats.baseCooldown = currentCooldown;
    }
    #endregion
}