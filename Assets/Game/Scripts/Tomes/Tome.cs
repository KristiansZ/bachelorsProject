using UnityEngine;

public abstract class Tome : MonoBehaviour
{
    public TomeType tomeType;
    public Sprite iconSprite;
    public CastMode castMode = CastMode.Directional;

    public GameObject projectilePrefab;
    public float damageMultiplier = 1f;
    public float currentCooldown;

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

    protected float _currentDamage;
    protected float _currentCooldown;
    protected float _nextCastTime;
    protected float _remainingCooldown;
    protected AudioSource _audioSource;
    protected Transform _playerCamera;
    protected Transform _projectileSpawnPoint;
    protected PlayerController _playerController;

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

        _currentDamage = stats.baseDamage * damageMultiplier;
        _currentCooldown = stats.baseCooldown;

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        _playerCamera = Camera.main?.transform;
        _playerController = GetComponentInParent<PlayerController>();
    }

    protected virtual void Update()
    {
        if (_remainingCooldown > 0)
        {
            _remainingCooldown = _nextCastTime - Time.time;
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
        if (Time.time < _nextCastTime) return;

        Ray ray = _playerCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);

        switch (castMode)
        {
            case CastMode.Directional:
                if (Physics.Raycast(ray, out RaycastHit hitDir, 100f))
                {
                    Vector3 spawnPos = _projectileSpawnPoint != null ? _projectileSpawnPoint.position : _playerController.transform.position;
                    Vector3 targetDirection = (hitDir.point - spawnPos).normalized;
                    targetDirection.y = 0;

                    _playerController.LookAt(spawnPos + targetDirection);

                    Vector3 targetPoint = spawnPos + targetDirection * stats.range;

                    ExecuteCast(targetPoint); //direction

                    PlayEffects();
                    _nextCastTime = Time.time + _currentCooldown;
                    _remainingCooldown = _currentCooldown;
                }
                break;

            case CastMode.Targeted:
                if (Physics.Raycast(ray, out RaycastHit hitTargeted, stats.range))
                {
                    _playerController.LookAt(hitTargeted.point);

                    ExecuteCast(hitTargeted.point);//exact hit point

                    PlayEffects();
                    _nextCastTime = Time.time + _currentCooldown;
                    _remainingCooldown = _currentCooldown;
                }
                break;
        }
    }

    protected void PlayEffects()
    {
        //_audioSource?.PlayOneShot(castSound);
        castParticles?.Play();
    }

    protected abstract void ExecuteCast(Vector3 targetPosition);

    #region Upgrades
    public virtual void UpgradeDamage(float percent) 
    {
        _currentDamage = stats.baseDamage * (1 + percent) * damageMultiplier;
    }

    public virtual void UpgradeCooldown(float percent) 
        => _currentCooldown = stats.baseCooldown * (1 - Mathf.Clamp01(percent));

    #endregion
}