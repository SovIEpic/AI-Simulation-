using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class AIHealerController : AIController
{
    [Header("Healer Abilities")]
    [SerializeField] protected float healerSpeed = 5f;
    [SerializeField] protected float siphonCooldown = 20f;
    [SerializeField] protected float siphonDuration = 10f;
    [SerializeField] protected float lastStandThreshold = 0.05f;
    [SerializeField] protected float lastStandMultiplier = 3f;

    [Header("Navigation Settings")]
    [SerializeField] protected NavMeshAgent navAgent;
    [SerializeField] protected float pathUpdateRate = 0.2f;
    protected float lastPathUpdate;
    protected bool isPathfinding = false;

    [Header("Visuals")]
    [SerializeField] private Color resusColor = Color.green;
    [SerializeField] private Color siphonColor = Color.magenta;
    [SerializeField] private Color lastStandColor = new Color(1f, 0.5f, 0f); //orange

    protected float lastResusTime = Mathf.NegativeInfinity;
    protected float lastSiphonTime = Mathf.NegativeInfinity;
    private Material originalMaterial;
    protected bool siphonActive = false;

    [Header("Debug")]
    [SerializeField] private bool showAbilityLogs = true;

    protected override void Start()
    {
        base.Start();
        originalMaterial = GetComponent<Renderer>().material;

        // Initialize NavMeshAgent
        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();

        if (navAgent == null)
            navAgent = gameObject.AddComponent<NavMeshAgent>();

        // Configure NavMeshAgent
        navAgent.speed = healerSpeed;
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
            isPathfinding= false;
            bossTarget = GameObject.FindGameObjectWithTag("Boss")?.transform;
            if (bossTarget == null) return;
        }

        Vector3 bossPosition = new Vector3(
            bossTarget.position.x,
            transform.position.y,
            bossTarget.position.z
        );

        float distance = Vector3.Distance(transform.position, bossPosition);

        // Debug siphon cooldown
        if (showAbilityLogs)
        {
            Debug.Log($"Siphon Cooldown: {Mathf.Max(0, lastSiphonTime + siphonCooldown - Time.time):F1}s");
        }

        // Attempt abilities
        if (stats.currentHealth <= stats.maxHealth * lastStandThreshold)
        {
            TriggerLastStand();
        }
        else if (HasDeadTeammate())
        {
            TryResus();
        }
        else if (!siphonActive && Time.time >= lastSiphonTime + siphonCooldown)
        {
            TrySiphon();
        }
        else if (distance <= attackRange)
        {
            // Stop moving when in attack range
            if (navAgent != null)
            {
                navAgent.isStopped = true;
                isPathfinding = false;
            }

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                TryAttack();
            }
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

        // Synchronize healer speed with navAgent
        if (isPathfinding && navAgent != null)
        {
            navAgent.speed = healerSpeed;
        }
    }

    protected void TryResus()
    {
        CharacterStats[] allCharacters = Resources.FindObjectsOfTypeAll<CharacterStats>();
        foreach(var teammate in allCharacters)
        {
            if(teammate.CompareTag("AI") && teammate != this.stats && teammate.currentHealth <= 0)
            {
                Debug.Log("[Healer DEBUG] Attempting to revive " + teammate.name);
                lastResusTime = Time.time;
                teammate.Revive();
                ApplyColor(resusColor);
                if(this is QLearningHealerController q) q.SetResusUsed();
                if (showAbilityLogs) Debug.Log("Resus ACTIVATED");
                Invoke(nameof(ResetColor), 1f);
                break;
            }
        }
    }

    protected bool HasDeadTeammate()
    {
        CharacterStats[] allCharacters = Resources.FindObjectsOfTypeAll<CharacterStats>();
        foreach(var teammate in allCharacters)
        {
            if(teammate.CompareTag("AI") && teammate != this.stats && teammate.currentHealth <= 0)
            {
                return true;
            }
        }
        return false;
    }

    // Check this - not sure if OnDealDamage exists
    protected void TrySiphon()
    {
        siphonActive = true;
        lastSiphonTime = Time.time;
        ApplyColor(siphonColor);
        stats.OnDealDamage += HandleSiphonHeal;
        if(showAbilityLogs) Debug.Log("Siphon ACTIVATED");
        Invoke(nameof(StopSiphon), siphonDuration);
    }

    protected void StopSiphon()
    {
        siphonActive = false;
        stats.OnDealDamage -= HandleSiphonHeal;
        ResetColor();
    }

    //Check this for names of damage variables
    protected void HandleSiphonHeal(GameObject target, ref float damage)
    {
        if(!siphonActive || target.tag != "Boss") return;
        float originalDamage = damage;
        damage *= 0.5f;
        stats.Heal(originalDamage * 0.5f);
    }

    protected void TriggerLastStand()
    {
        ApplyColor(lastStandColor);
        if(showAbilityLogs) Debug.Log("Last Stand Used");
        var bossStats = bossTarget.GetComponent<CharacterStats>();
        if(bossStats != null)
        {
            float damage = stats.damage * lastStandMultiplier;
            bossStats.TakeDamage(damage);
        }
        stats.Die();
    }

    protected void ApplyColor(Color color)
    {
        Renderer renderer = GetComponent<Renderer>();
        if(renderer != null)
        {
            renderer.material.color = color;
        }
    }

    protected void ResetColor()
    {
        if (GetComponent<Renderer>() != null)
            GetComponent<Renderer>().material.color = originalMaterial.color;
    }

    protected void OnDrawGizmosSelected()
    {
        // Attack Range (Red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

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
