using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class AISwordmasterController : AIController
{
    [Header("Swordmaster Abilities")]
    [SerializeField] protected float swordmasterSpeed = 4f;
    [SerializeField] protected float lungeDuration = 10f;
    [SerializeField] protected float lungeCooldown = 25f;
    [SerializeField] protected float lungeRangeMultiplier = 1.3f;
    [SerializeField] protected float lungeMissChance = 0.15f;

    [SerializeField] protected float fatalFlurryDuration = 10f;
    [SerializeField] protected float fatalFlurryCooldown = 40f;
    [SerializeField] protected float fatalFlurryDamageMultiplier = 2f;
    [SerializeField] protected float fatalFlurryRangeMultiplier = 0.5f;
    [SerializeField] protected float fatalFlurryThreshold = 0.2f;

    [SerializeField] protected float poisonDuration = 10f;
    [SerializeField] protected float poisonCooldown = 30f;
    [SerializeField] protected float poisonDamagePercentage = 0.05f;

    [Header("Visuals")]
    [SerializeField] private Color lungeColor = Color.cyan;
    [SerializeField] private Color fatalFlurryColor = Color.red;
    [SerializeField] private Color poisonColor = Color.green;
    [SerializeField] private GameObject poisonEffectPrefab;

    [Header("Navigation")]
    [SerializeField] protected NavMeshAgent navAgent;
    [SerializeField] protected float pathUpdateRate = 0.2f;

    protected float lastLungeTime = Mathf.NegativeInfinity;
    protected float lastFatalFlurryTime = Mathf.NegativeInfinity;
    protected float lastPoisonTime = Mathf.NegativeInfinity;
    protected float originalAttackRange;
    protected float originalDamage;
    private Material originalMaterial;
    private GameObject activePoisonEffect;

    protected bool isLungeActive = false;
    protected bool isFatalFlurryActive = false;
    protected bool isPoisonActive = false;

    [Header("Debug")]
    [SerializeField] private bool showAbilityLogs = true;

    protected override void Start()
    {
        base.Start();
        originalMaterial = GetComponent<Renderer>().material;
        originalAttackRange = attackRange;
        originalDamage = stats.damage;

        // Initialize NavMeshAgent
        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();

        if (navAgent == null)
            navAgent = gameObject.AddComponent<NavMeshAgent>();

        // Configure NavMeshAgent
        navAgent.speed = swordmasterSpeed;
        navAgent.angularSpeed = 120f;
        navAgent.acceleration = 8f;
        navAgent.stoppingDistance = attackRange * 0.8f;
        navAgent.autoBraking = false;
    }

    protected override void Update()
    {
        base.Update();

        if (bossTarget == null || !bossTarget.gameObject.activeInHierarchy)
        {
            navAgent.isStopped = true;
            bossTarget = GameObject.FindGameObjectWithTag("Boss")?.transform;
            if (bossTarget == null) return;
        }

        Vector3 bossPosition = new Vector3(
            bossTarget.position.x,
            transform.position.y,
            bossTarget.position.z
        );

        float distance = Vector3.Distance(transform.position, bossPosition);

        // Check ability conditions
        if (IsInAttackRange())
        {
            // Stop moving when in attack range
            if (navAgent != null)
            {
                navAgent.isStopped = true;
            }

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                TryAttack();
            }
        }
        else if (distance <= detectionRange)
        {
            // Use NavMesh for pathing to target
            if (navAgent != null)
            {
                navAgent.SetDestination(bossPosition);
                navAgent.isStopped = false;
            }
        }
        else if (navAgent != null)
        {
            // Stop moving when target is out of detection range
            navAgent.isStopped = true;
        }
    }

    protected new void TryAttack()
    {
        if (Time.time > lastAttackTime + attackCooldown)
        {
            if (bossTarget == null) return;

            var targetStats = bossTarget.GetComponent<CharacterStats>();
            if (targetStats == null)
            {
                Debug.LogWarning($"Target {bossTarget.name} missing CharacterStats!");
                return;
            }

            // Verify distance again (in case target moved)
            if (Vector3.Distance(transform.position, bossTarget.position) <= attackRange)
            {
                lastAttackTime = Time.time;

                float damageToDeal = stats.damage;
                bool attackMissed = false;

                // Apply lunge miss chance
                if (isLungeActive && Random.Range(0f, 1f) < lungeMissChance)
                {
                    damageToDeal = 0;
                    attackMissed = true;
                }

                // Apply fatal flurry damage multiplier
                if (isFatalFlurryActive)
                {
                    damageToDeal *= fatalFlurryDamageMultiplier;
                }

                targetStats.TakeDamage(damageToDeal);

                if (showAbilityLogs)
                {
                    if (attackMissed)
                        Debug.Log($"{name} missed the attack due to Lunge!");
                    else
                        Debug.Log($"{name} attacked {bossTarget.name} for {damageToDeal} damage");
                }

                // Apply poison on successful hit
                if (!attackMissed && isPoisonActive)
                {
                    StartCoroutine(ApplyPoisonOverTime(targetStats, stats.damage * poisonDamagePercentage));
                }
            }
        }
    }

    public void TryLunge()
    {
        if (Time.time < lastLungeTime + lungeCooldown) return;

        lastLungeTime = Time.time;
        isLungeActive = true;
        attackRange = originalAttackRange * lungeRangeMultiplier;
        ApplyColor(lungeColor);

        if (showAbilityLogs) Debug.Log("Lunge ACTIVATED");

        Invoke(nameof(EndLunge), lungeDuration);
    }

    private void EndLunge()
    {
        isLungeActive = false;
        attackRange = originalAttackRange;
        ResetColor();
    }

    public void TryFatalFlurry()
    {
        if (Time.time < lastFatalFlurryTime + fatalFlurryCooldown) return;

        var bossStats = bossTarget.GetComponent<CharacterStats>();
        if (bossStats == null || bossStats.currentHealth > bossStats.maxHealth * fatalFlurryThreshold) return;

        lastFatalFlurryTime = Time.time;
        isFatalFlurryActive = true;
        attackRange = originalAttackRange * fatalFlurryRangeMultiplier;
        ApplyColor(fatalFlurryColor);

        if (showAbilityLogs) Debug.Log("Fatal Flurry ACTIVATED");

        Invoke(nameof(EndFatalFlurry), fatalFlurryDuration);
    }

    private void EndFatalFlurry()
    {
        isFatalFlurryActive = false;
        attackRange = originalAttackRange;
        ResetColor();
    }

    public void TryPoisonSword()
    {
        if (Time.time < lastPoisonTime + poisonCooldown) return;

        lastPoisonTime = Time.time;
        isPoisonActive = true;
        ApplyColor(poisonColor);

        if (poisonEffectPrefab != null)
        {
            if (activePoisonEffect != null) Destroy(activePoisonEffect);
            activePoisonEffect = Instantiate(poisonEffectPrefab, transform);
        }

        if (showAbilityLogs) Debug.Log("Poison Sword ACTIVATED");

        Invoke(nameof(EndPoisonSword), poisonDuration);
    }

    private void EndPoisonSword()
    {
        isPoisonActive = false;
        ResetColor();
        if (activePoisonEffect != null)
        {
            Destroy(activePoisonEffect);
            activePoisonEffect = null;
        }
    }

    private IEnumerator ApplyPoisonOverTime(CharacterStats targetStats, float damagePerTick)
    {
        float endTime = Time.time + poisonDuration;

        while (Time.time < endTime && targetStats != null && targetStats.currentHealth > 0)
        {
            targetStats.TakeDamage(damagePerTick);
            if (showAbilityLogs) Debug.Log($"Poison dealt {damagePerTick} damage");
            yield return new WaitForSeconds(1f);
        }
    }

    private void ApplyColor(Color color)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    private void ResetColor()
    {
        if (!isLungeActive && !isFatalFlurryActive && !isPoisonActive &&
            GetComponent<Renderer>() != null)
        {
            GetComponent<Renderer>().material.color = originalMaterial.color;
        }
    }

    protected new void OnDrawGizmosSelected()
    {

        // Current attack range (takes into account active abilities)
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}