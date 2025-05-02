using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    public enum State { Idle, Chasing, Attacking, Dead }

    [Header("Components")]
    private EnemyHealthBar healthBar;
    [SerializeField] private GameObject healthBarPrefab;

    [Header("Dissolve Material")]
    [SerializeField] private Material dissolveMaterialTemplate;

    [Header("Chase Settings")]
    [SerializeField] private float chaseUpdateRate = 0.03f;
    [SerializeField] private float detectionRange = 12f;

    private Material instancedDissolveMaterial;
    private Renderer enemyRenderer;

    private NavMeshAgent agent;
    private Animator animator;
    private EnemyStats stats;
    
    private float currentHealth;
    private float originalMoveSpeed;
    private Vector3 spawnPosition;
    
    private Coroutine slowCoroutine;
    private Coroutine currentStateRoutine;
    private State currentState;

    private bool isBurning;
    private float burningTimer;
    private float attackCooldown;
    
    private bool isInitialized = false;
    private bool isDead = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        enemyRenderer = GetComponentInChildren<Renderer>();
    }

    void Start()
    {
        StartCoroutine(InitializeNavMesh());
    }

    private IEnumerator InitializeNavMesh()
    {
        yield return null;

        if (agent == null) yield break;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 1.0f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            
            if (isInitialized)
            {
                SetState(State.Idle);
            }
        }
        else
        {
            Debug.LogWarning($"Enemy {gameObject.name} is not on a NavMesh. Navigation will be disabled.");
            agent.enabled = false;
        }
    }

    public void Initialize(EnemyStats enemyStats)
    {
        stats = enemyStats;
        isDead = false;

        currentHealth = stats.maxHealth;
        originalMoveSpeed = stats.moveSpeed;
        attackCooldown = 1f / stats.attackSpeed;
        spawnPosition = transform.position;

        ConfigureAgent();
        CreateHealthBar();
        
        isInitialized = true;
        SetState(State.Idle);
    }

    private void ConfigureAgent()
    {
        if (agent == null || !agent.enabled) return;
        
        agent.speed = originalMoveSpeed;
        agent.updateRotation = true;
        agent.avoidancePriority = 50;
        agent.stoppingDistance = stats.attackRange * 0.9f;
        agent.angularSpeed = 9999f;
    }

    private void CreateHealthBar()
    {
        if (healthBarPrefab == null)
        {
            Debug.LogWarning("Health bar prefab not assigned on " + gameObject.name);
            return;
        }
        
        GameObject healthBarObj = Instantiate(healthBarPrefab);
        healthBar = healthBarObj.GetComponent<EnemyHealthBar>();
        
        if (healthBar != null)
        {
            healthBar.Initialize(transform, stats.maxHealth);
        }
        else
        {
            Debug.LogError("EnemyHealthBar component missing on health bar prefab!");
        }
    }

    void SetState(State newState)
    {
        if (currentState == newState && newState != State.Idle) return;
        
        if (isDead && newState != State.Dead) return;

        if (currentStateRoutine != null)
        {
            StopCoroutine(currentStateRoutine);
            currentStateRoutine = null;
        }

        currentState = newState;
        UpdateAnimationForState(newState);

        if (agent != null && agent.enabled)
        {
            agent.isStopped = (newState == State.Attacking || newState == State.Dead);
        }

        switch (currentState)
        {
            case State.Idle:
                currentStateRoutine = StartCoroutine(IdleRoutine());
                break;
            case State.Chasing:
                currentStateRoutine = StartCoroutine(ChaseRoutine());
                break;
            case State.Attacking:
                currentStateRoutine = StartCoroutine(AttackRoutine());
                break;
        }
    }

    private void UpdateAnimationForState(State state)
    {
        if (animator == null) return;
        
        switch (state)
        {
            case State.Idle:
                animator.SetBool("IsRunning", false);
                animator.SetBool("IsAttacking", false);
                break;
            case State.Chasing:
                animator.SetBool("IsRunning", true);
                animator.SetBool("IsAttacking", false);
                break;
            case State.Attacking:
                animator.SetBool("IsRunning", false);
                animator.SetBool("IsAttacking", true);
                break;
            case State.Dead:
                animator.SetBool("IsRunning", false);
                animator.SetBool("IsAttacking", false);
                break;
        }
    }

    IEnumerator IdleRoutine()
    {
        if (agent == null || !agent.isOnNavMesh)
            yield break;

        while (!isDead)
        {
            if (ShouldChasePlayer())
            {
                SetState(State.Chasing);
                yield break;
            }

            yield return WanderAround();
            
            yield return new WaitForSeconds(Random.Range(3f, 6f));
        }
    }

    private IEnumerator WanderAround()
    {
        Vector3 randomOffset = Random.insideUnitSphere * 5f;
        randomOffset.y = 0f;
        Vector3 wanderPos = spawnPosition + randomOffset;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(wanderPos, out hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            animator.SetBool("IsRunning", true);

            float waitTime = 0f;
            while (agent.pathPending && waitTime < 1f)
            {
                waitTime += Time.deltaTime;
                yield return null;
            }

            if (!agent.hasPath)
            {
                yield return new WaitForSeconds(1f);
                yield break;
            }

            float timeout = 0f;
            while (agent.remainingDistance > agent.stoppingDistance && timeout < 5f)
            {
                timeout += Time.deltaTime;

                if (isDead) yield break;

                if (ShouldChasePlayer()) //player can get in range while wandering
                {
                    SetState(State.Chasing);
                    yield break;
                }

                yield return null;
            }

            animator.SetBool("IsRunning", false);
        }
    }

    private bool ShouldChasePlayer()
    {
        if (Player.Instance == null) return false;
        
        float distToPlayer = Vector3.Distance(transform.position, Player.Instance.PlayerTransform.position);
        return distToPlayer <= detectionRange;
    }

    IEnumerator ChaseRoutine()
    {
        if (agent == null || !agent.isOnNavMesh)
            yield break;

        WaitForSeconds pathUpdateWait = new WaitForSeconds(chaseUpdateRate);

        while (!isDead)
        {
            if (Player.Instance == null)
            {
                SetState(State.Idle);
                yield break;
            }

            Vector3 playerPos = Player.Instance.PlayerTransform.position;
            float dist = Vector3.Distance(transform.position, playerPos);

            if (dist <= stats.attackRange)
            {
                SetState(State.Attacking);
                yield break;
            }

            agent.SetDestination(playerPos);
            yield return pathUpdateWait;
        }
    }

    IEnumerator AttackRoutine()
    {
        while (!isDead)
        {
            if (Player.Instance == null)
            {
                SetState(State.Idle);
                yield break;
            }

            float dist = Vector3.Distance(transform.position, Player.Instance.PlayerTransform.position);
            if (dist > stats.attackRange * 1.1f)
            {
                SetState(State.Chasing);
                yield break;
            }

            FaceTarget(Player.Instance.PlayerTransform.position);

            float animSpeed = stats.attackSpeed;
            animator.SetFloat("AttackSpeedMultiplier", animSpeed);

            yield return new WaitForSeconds(0.5f / animSpeed);

            if (Player.Instance != null && Player.Instance.DamageHandler != null)
            {
                Player.Instance.DamageHandler.HandleDamage(stats.damage);
            }

            yield return new WaitForSeconds(attackCooldown - (0.5f / animSpeed));
        }
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 dirToTarget = target - transform.position;
        dirToTarget.y = 0;
        
        if (dirToTarget != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dirToTarget);
        }
    }

    #region Status Effects

    public void ApplySlow(float slowPercent, float duration)
    {
        if (isDead) return;
        
        if (slowCoroutine != null) StopCoroutine(slowCoroutine);
        slowCoroutine = StartCoroutine(SlowEffect(slowPercent, duration));
    }

    IEnumerator SlowEffect(float slowPercent, float duration)
    {
        if (agent != null && agent.enabled)
        {
            agent.speed = originalMoveSpeed * (1 - Mathf.Clamp01(slowPercent));
        }

        yield return new WaitForSeconds(duration);

        if (isDead) yield break;

        if (agent != null && agent.enabled)
        {
            agent.speed = originalMoveSpeed;
        }

        slowCoroutine = null;
    }

    public void StartBurning(float damage, float duration)
    {
        if (isDead) return;
        
        if (!isBurning)
        {
            isBurning = true;
            burningTimer = 0f;
            StartCoroutine(BurnCoroutine(damage, duration));
        }
    }

    IEnumerator BurnCoroutine(float damage, float duration)
    {
        WaitForSeconds tickInterval = new WaitForSeconds(0.5f);

        while (burningTimer < duration && !isDead)
        {
            yield return tickInterval;
            
            TakeDamage(damage);
            burningTimer += 0.5f;
        }

        isBurning = false;
    }

    #endregion

    #region Health and Death

    public void TakeDamage(float damage)
    {
        if (!isInitialized || isDead) return;
        
        currentHealth -= damage;

        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth);
        }

        if (currentHealth <= 0) 
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        SetState(State.Dead);
        
        StopAllCoroutines();
        
        DisableColliders();
        AwardExperienceAndNotify();
        StartDissolveEffect();
        
        StartCoroutine(WaitAndReturnToPool(3f));
    }

    private void DisableColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
    }

    private void AwardExperienceAndNotify()
    {
        if (Player.Instance != null)
        {
            Player.Instance.Leveling.AddExperience(stats.xpReward);
        }

        if (TeleportingPortal.instance != null)
        {
            TeleportingPortal.instance.EnemyKilled();
        }

        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
        }
    }

    private void StartDissolveEffect()
    {
        ApplyDissolveToAll();
        StartCoroutine(DissolveAnimation());
    }

    void ApplyDissolveToAll()
    {
        instancedDissolveMaterial = new Material(dissolveMaterialTemplate);
        instancedDissolveMaterial.SetVector("_Dissolve_Amount", new Vector2(-0.8f, 1f));

        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            rend.material = instancedDissolveMaterial;
        }
    }

    IEnumerator DissolveAnimation()
    {
        float dissolveTime = 2f;
        float elapsed = 0f;

        while (elapsed < dissolveTime)
        {
            float t = elapsed / dissolveTime;
            float dissolveX = Mathf.Lerp(-0.8f, 1f, t);
            instancedDissolveMaterial.SetVector("_Dissolve_Amount", new Vector2(dissolveX, 1f));

            elapsed += Time.deltaTime;
            yield return null;
        }

        instancedDissolveMaterial.SetVector("_Dissolve_Amount", new Vector2(1f, 1f));
    }

    IEnumerator WaitAndReturnToPool(float time)
    {
        yield return new WaitForSeconds(time);
        gameObject.SetActive(false);
    }

    #endregion
}