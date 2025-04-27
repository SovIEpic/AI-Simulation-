using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class AIAssassinController : AIController
{
    [Header("Assassin Abilities")]
    [SerializeField] protected float assassinSpeed = 5f;
    [SerializeField] protected float chameleonDuration = 10f;
    [SerializeField] protected float chameleonCooldown = 25f;
    [SerializeField] protected float stunGunDuration = 5f;
    [SerializeField] protected float stunGunCooldown = 20f;
    [SerializeField] protected float stunGunDamageBuff = 0.33f;
    [SerializeField] protected float stunGunBuffDuration = 10f;
    [SerializeField] protected float speedBoostDuration = 15f;
    [SerializeField] protected float speedBoostCooldown = 35f;
    [SerializeField] protected float speedBoostMultiplier = 1.6f;
    [SerializeField] protected float speedBoostDamageReduction = 0.3f;

    [Header("Visuals")]
    [SerializeField] private Color chameleonColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    [SerializeField] private Color stunGunColor = Color.yellow;
    [SerializeField] private Color speedBoostColor = Color.blue;
    [SerializeField] private GameObject invisibilityEffectPrefab;
    [SerializeField] private GameObject stunEffectPrefab;
    [SerializeField] private GameObject speedEffectPrefab;

    [Header("Navigation")]
    [SerializeField] protected NavMeshAgent navAgent;
    [SerializeField] protected float pathUpdateRate = 0.2f;

    protected float lastChameleonTime = Mathf.NegativeInfinity;
    protected float lastStunGunTime = Mathf.NegativeInfinity;
    protected float lastSpeedBoostTime = Mathf.NegativeInfinity;
    protected float originalSpeed;
    protected float originalDamage;
    private Material originalMaterial;
    private GameObject activeInvisibilityEffect;
    private GameObject activeStunEffect;
    private GameObject activeSpeedEffect;

    protected bool isChameleonActive = false;
    protected bool isStunGunActive = false;
    protected bool isSpeedBoostActive = false;
    protected bool isBossStunned = false;

    [Header("Debug")]
    [SerializeField] private bool showAbilityLogs = true;

    protected override void Start()
    {
        base.Start();
        originalMaterial = GetComponent<Renderer>().material;
        originalSpeed = assassinSpeed;
        originalDamage = stats.damage;

        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();

        if (navAgent == null)
            navAgent = gameObject.AddComponent<NavMeshAgent>();

        navAgent.speed = assassinSpeed;
        navAgent.angularSpeed = 120f;
        navAgent.acceleration = 8f;
        navAgent.stoppingDistance = attackRange * 0.8f;
        navAgent.autoBraking = false;
    }

    protected override void Update()
    {
        base.Update();

        if (bossTarget == null)
        {
            bossTarget = GameObject.FindGameObjectWithTag("Boss")?.transform;
            if (bossTarget == null) return;
        }

        Vector3 bossPosition = new Vector3(
            bossTarget.position.x,
            transform.position.y,
            bossTarget.position.z
        );

        float distance = Vector3.Distance(transform.position, bossPosition);

        if (distance <= attackRange)
        {
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
            if (navAgent != null)
            {
                navAgent.SetDestination(bossPosition);
                navAgent.isStopped = false;
            }
        }
        else if (navAgent != null)
        {
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

            if (Vector3.Distance(transform.position, bossTarget.position) <= attackRange)
            {
                lastAttackTime = Time.time;

                float damageToDeal = stats.damage;

                // Apply speed boost damage reduction
                if (isSpeedBoostActive)
                {
                    damageToDeal *= (1f - speedBoostDamageReduction);
                }

                targetStats.TakeDamage(damageToDeal);

                if (showAbilityLogs)
                {
                    Debug.Log($"{name} attacked {bossTarget.name} for {damageToDeal} damage");
                }

                // Break chameleon if attacking
                if (isChameleonActive)
                {
                    EndChameleon();
                }
            }
        }
    }

    public void TryChameleon()
    {
        if (Time.time < lastChameleonTime + chameleonCooldown) return;

        lastChameleonTime = Time.time;
        isChameleonActive = true;
        ApplyColor(chameleonColor);

        if (invisibilityEffectPrefab != null)
        {
            if (activeInvisibilityEffect != null) Destroy(activeInvisibilityEffect);
            activeInvisibilityEffect = Instantiate(invisibilityEffectPrefab, transform);
        }

        if (showAbilityLogs) Debug.Log("Chameleon ACTIVATED");

        Invoke(nameof(EndChameleon), chameleonDuration);
    }

    private void EndChameleon()
    {
        isChameleonActive = false;
        ResetColor();
        if (activeInvisibilityEffect != null)
        {
            Destroy(activeInvisibilityEffect);
            activeInvisibilityEffect = null;
        }
    }

    public void TryStunGun()
    {
        if (Time.time < lastStunGunTime + stunGunCooldown) return;

        var bossAI = bossTarget.GetComponent<BossAI>();
        if (bossAI == null) return;

        lastStunGunTime = Time.time;
        isStunGunActive = true;
        ApplyColor(stunGunColor);

        if (stunEffectPrefab != null)
        {
            if (activeStunEffect != null) Destroy(activeStunEffect);
            activeStunEffect = Instantiate(stunEffectPrefab, bossTarget);
        }

        // Stun the boss
        bossAI.ApplyStun(stunGunDuration);
        isBossStunned = true;

        // Schedule boss damage buff after stun ends
        Invoke("ApplyBossDamageBuff", stunGunDuration);

        if (showAbilityLogs) Debug.Log("Stun Gun ACTIVATED");

        Invoke("EndStunGun", stunGunDuration);
    }

    private void ApplyBossDamageBuff()
    {
        var bossStats = bossTarget.GetComponent<CharacterStats>();
        if (bossStats != null)
        {
            bossStats.damage *= (1f + stunGunDamageBuff);
            Invoke("RemoveBossDamageBuff", stunGunBuffDuration);
        }
    }

    private void RemoveBossDamageBuff()
    {
        var bossStats = bossTarget.GetComponent<CharacterStats>();
        if (bossStats != null)
        {
            bossStats.damage /= (1f + stunGunDamageBuff);
        }
    }

    private void EndStunGun()
    {
        isStunGunActive = false;
        isBossStunned = false;
        ResetColor();
        if (activeStunEffect != null)
        {
            Destroy(activeStunEffect);
            activeStunEffect = null;
        }
    }

    public void TrySpeedBoost()
    {
        if (Time.time < lastSpeedBoostTime + speedBoostCooldown) return;

        lastSpeedBoostTime = Time.time;
        isSpeedBoostActive = true;
        assassinSpeed = originalSpeed * speedBoostMultiplier;
        navAgent.speed = assassinSpeed;
        ApplyColor(speedBoostColor);

        if (speedEffectPrefab != null)
        {
            if (activeSpeedEffect != null) Destroy(activeSpeedEffect);
            activeSpeedEffect = Instantiate(speedEffectPrefab, transform);
        }

        if (showAbilityLogs) Debug.Log("Need For Speed ACTIVATED");

        Invoke(nameof(EndSpeedBoost), speedBoostDuration);
    }

    private void EndSpeedBoost()
    {
        isSpeedBoostActive = false;
        assassinSpeed = originalSpeed;
        navAgent.speed = assassinSpeed;
        ResetColor();
        if (activeSpeedEffect != null)
        {
            Destroy(activeSpeedEffect);
            activeSpeedEffect = null;
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
        if (!isChameleonActive && !isStunGunActive && !isSpeedBoostActive &&
            GetComponent<Renderer>() != null)
        {
            GetComponent<Renderer>().material.color = originalMaterial.color;
        }
    }

    protected new void OnDrawGizmosSelected()
    {
        // Detection Range (Cyan)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack Range (Red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Current speed indicator
        if (isSpeedBoostActive)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
        }

        // Target direction
        if (bossTarget != null)
        {
            Gizmos.color = isChameleonActive ? new Color(0.5f, 0.5f, 0.5f, 0.5f) : Color.green;
            Gizmos.DrawLine(transform.position, bossTarget.position);
        }
    }
}