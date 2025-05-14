using UnityEngine;

public abstract class Tome : MonoBehaviour
{
    public TomeType tomeType;
    public Sprite iconSprite;
    public CastMode castMode = CastMode.Directional;

    public GameObject projectilePrefab;
    public float damageMultiplier = 1f;

    [Header("Casting Behavior")]
    public bool enableContinuousCasting = true;

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
    }

    protected virtual void Update()
    {
        if (remainingCooldown > 0)
        {
            remainingCooldown = nextCastTime - Time.time;
        }

        if (enableContinuousCasting) //can hold M1 to cast
        {
            if (Input.GetMouseButton(0)) TryCastAtPosition();
        }
        else
        {
            if (Input.GetMouseButtonDown(0)) TryCastAtPosition();
        }
    }

    protected void TryCastAtPosition()
    {
        if (Time.time < nextCastTime) return;

        Ray ray = playerCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);

        switch (castMode)
        {
            case CastMode.Directional:
                if (Physics.Raycast(ray, out RaycastHit hitDir, 100f))
                {
                    Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : playerController.transform.position;
                    Vector3 targetDirection = (hitDir.point - spawnPos).normalized;
                    targetDirection.y = 0;

                    playerController.LookAt(spawnPos + targetDirection);

                    Vector3 targetPoint = spawnPos + targetDirection * stats.range;

                    ExecuteCast(targetPoint); //direction

                    PlayEffects();
                    nextCastTime = Time.time + currentCooldown;
                    remainingCooldown = currentCooldown;
                }
                break;

            case CastMode.Targeted:
                if (Physics.Raycast(ray, out RaycastHit hitTargeted, stats.range))
                {
                    playerController.LookAt(hitTargeted.point);

                    ExecuteCast(hitTargeted.point);//exact hit point

                    PlayEffects();
                    nextCastTime = Time.time + currentCooldown;
                    remainingCooldown = currentCooldown;
                }
                break;
        }
    }

    protected void PlayEffects()
    {
        //audioSource?.PlayOneShot(castSound);
        castParticles?.Play();
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