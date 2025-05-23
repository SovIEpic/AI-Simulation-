using UnityEngine;
using UnityEngine.AI; // For NavMesh functionality

public class AITankController : AIController
{
    [Header("Tank Abilities")]
    [SerializeField] protected float tankSpeed = 2.5f;
    [SerializeField] protected float shieldBashRange = 3f;
    [SerializeField] protected float tauntRange = 8f;
    [SerializeField] protected float blockDuration = 3f;
    [SerializeField] protected float shieldBashCooldown = 8f;
    [SerializeField] protected float tauntCooldown = 15f;
    [SerializeField] protected float blockCooldown = 10f;
    [SerializeField] protected float tauntDuration = 5f;
    [SerializeField] protected float shieldBashDamage = 25f;
    [Header("Shield Bash Settings")]
    [SerializeField] private float bashKnockbackForce = 3f;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject shieldIndicator;
    [SerializeField] private Color blockColor = Color.blue;
    [SerializeField] private Color bashColor = Color.red;
    [SerializeField] private Color tauntColor = Color.yellow;
    private Material originalMaterial;

    // Cooldowns (initialized to allow immediate use)
    protected float lastTauntTime = Mathf.NegativeInfinity;
    protected float lastShieldBashTime = Mathf.NegativeInfinity;
    protected float lastBlockTime = Mathf.NegativeInfinity;
    protected bool isBlocking;

    [Header("Debug")]
    [SerializeField] private bool showAbilityLogs = true;

    [Header("Navigation Settings")]
    [SerializeField] protected NavMeshAgent navAgent;
    [SerializeField] protected float pathUpdateRate = 0.2f;

    [Header("Effects")]
    [SerializeField] private GameObject tauntEffectPrefab;

    protected float lastPathUpdate;
    protected bool isPathfinding = false;

    protected override void Start()
    {
        base.Start();
        originalMaterial = GetComponent<Renderer>().material;

        if (shieldIndicator != null)
            shieldIndicator.SetActive(false);

        // Get or add NavMeshAgent
        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();

        if (navAgent == null)
            navAgent = gameObject.AddComponent<NavMeshAgent>();

        // Configure NavMeshAgent
        navAgent.speed = tankSpeed;
        navAgent.angularSpeed = 120f;
        navAgent.acceleration = 8f;
        navAgent.stoppingDistance = attackRange * 0.8f;
        navAgent.autoBraking = false;
    }

    protected override void Update()
    {
        if (bossTarget == null || !bossTarget.gameObject.activeInHierarchy)
        {
            navAgent.enabled = true;
            isPathfinding = false;
            bossTarget = GameObject.FindGameObjectWithTag("Boss")?.transform;
            if (bossTarget == null) return;
        }

        Vector3 bossPosition = new Vector3(
            bossTarget.position.x,
            transform.position.y,
            bossTarget.position.z
        );

        float distance = Vector3.Distance(transform.position, bossPosition);

        // Debug cooldown states
        if (showAbilityLogs)
        {
            Debug.Log($"Cooldowns - " +
                     $"Taunt: {Mathf.Max(0, lastTauntTime + tauntCooldown - Time.time):F1}s | " +
                     $"Bash: {Mathf.Max(0, lastShieldBashTime + shieldBashCooldown - Time.time):F1}s | " +
                     $"Block: {Mathf.Max(0, lastBlockTime + blockCooldown - Time.time):F1}s");
        }

        // Ability priority system
        if (distance <= tauntRange && Time.time >= lastTauntTime + tauntCooldown)
        {
            TryTaunt();
        }
        else if (distance <= shieldBashRange && Time.time >= lastShieldBashTime + shieldBashCooldown)
        {
            TryShieldBash();
        }
        else if (IsInAttackRange())
        {
            // Stop moving when in attack range
            if (navAgent != null)
            {
                navAgent.isStopped = true;
                isPathfinding = false;
            }

            if (Time.time >= lastBlockTime + blockCooldown)
                StartBlocking();
            else if (Time.time >= lastAttackTime + attackCooldown)
                TryAttack();
        }
        else if (distance <= detectionRange)
        {
            // Use NavMesh for pathing to target
            if (navAgent != null && (Time.time >= lastPathUpdate + pathUpdateRate || !isPathfinding))
            {
                navAgent.SetDestination(bossPosition);
                navAgent.isStopped = false;
                isPathfinding = true;
                lastPathUpdate = Time.time;
            }
        }
        else if (navAgent != null)
        {
            // Stop moving when target is out of detection range
            navAgent.isStopped = true;
            isPathfinding = false;
        }

        // Synchronize tank speed with navAgent
        if (isPathfinding && navAgent != null)
        {
            navAgent.speed = tankSpeed;
        }
    }
    protected new void TryAttack()
    {
        if(Time.time > lastAttackTime + attackCooldown)
        {
            if (bossTarget == null) return;

            var targetStats = bossTarget.GetComponent<CharacterStats>();
            if (targetStats == null)
            {
                Debug.LogWarning($"Tank target {bossTarget.name} missing CharacterStats!");
                return;
            }

            if (Vector3.Distance(transform.position, bossTarget.position) <= attackRange)
            {
                lastAttackTime = Time.time;
                targetStats.TakeDamage(stats.damage);
                Debug.Log($"{name} attacked {bossTarget.name} for {stats.damage} damage");
            }

        }
    }
    protected void TryTaunt()
    {
        if (Time.time < lastTauntTime + tauntCooldown) return;

        lastTauntTime = Time.time;
        ApplyColor(tauntColor);
        if (showAbilityLogs) Debug.Log($"Taunt ACTIVATED at {Time.time}");

        if (bossTarget != null && bossTarget.TryGetComponent<BossAI>(out var bossAI))
        {
            bossAI.ApplyTaunt(transform, tauntDuration);

            if (tauntEffectPrefab != null)
            {
                Instantiate(tauntEffectPrefab, bossTarget.position, Quaternion.identity);
            }
        }

        Invoke(nameof(ResetColor), 0.5f);
    }

    protected void TryShieldBash()
    {
        lastShieldBashTime = Time.time;
        ApplyColor(bashColor);
        if (showAbilityLogs) Debug.Log($"Shield Bash ACTIVATED at {Time.time}");

        Collider[] hits = Physics.OverlapSphere(transform.position, shieldBashRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Boss"))
            {
                // Apply damage
                hit.GetComponent<CharacterStats>()?.TakeDamage(shieldBashDamage);

                // Apply knockback (MODIFIED PART)
                Rigidbody bossRb = hit.GetComponent<Rigidbody>();
                if (bossRb != null)
                {
                    Vector3 knockbackDir = (hit.transform.position - transform.position).normalized;
                    bossRb.AddForce(knockbackDir * bashKnockbackForce, ForceMode.Impulse);
                }
            }
        }
        Invoke(nameof(ResetColor), 0.5f);
    }

    protected void StartBlocking()
    {
        isBlocking = true;
        lastBlockTime = Time.time;
        ApplyColor(blockColor);
        if (showAbilityLogs) Debug.Log($"Block ACTIVATED at {Time.time}");

        if (shieldIndicator != null)
            shieldIndicator.SetActive(true);

        Invoke(nameof(StopBlocking), blockDuration);
    }

    private void StopBlocking()
    {
        isBlocking = false;
        if (shieldIndicator != null)
            shieldIndicator.SetActive(false);
        ResetColor();
    }

    public bool IsBlocking()
    {
        return isBlocking;
    }

    private void ApplyColor(Color newColor)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = newColor;
        else if (showAbilityLogs)
            Debug.LogError("Missing Renderer component!");
    }

    private void ResetColor()
    {
        if (!isBlocking && GetComponent<Renderer>() != null)
            GetComponent<Renderer>().material.color = originalMaterial.color;
    }

    protected void OnDrawGizmosSelected()
    {
        // Attack Range (Red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Shield Bash Range (Orange)
        Gizmos.color = new Color(1, 0.5f, 0);
        Gizmos.DrawWireSphere(transform.position, shieldBashRange);

        // Taunt Range (Yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, tauntRange);

        // Detection Range (Cyan)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // If we have a target and are pathfinding, show the path
        if (Application.isPlaying && navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = Color.green;
            Vector3[] corners = navAgent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }
}