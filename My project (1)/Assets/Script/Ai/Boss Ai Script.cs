using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using BehaviorTree;
using ReinforcementLearning;
using BehaviorTree.Actions;

public enum BossPhase { Phase1, Phase2, Phase3 }

[RequireComponent(typeof(NavMeshAgent))]
public class BossAI : MonoBehaviour
{
    public BossStats stats;
    public List<Transform> playerAgents;
    public GameObject hpBarPrefab;
    public GameObject aoeEffectPrefab;

    [Header("Combo Settings")]
    public int maxComboHits = 3;
    public float comboResetTime = 3f;
    public float timeBetweenComboHits = 0.5f;
    public GameObject[] attackEffects;

    [Header("AOE Settings")]
    public float aoeRadius = 4f;
    public float aoeDamage = 30f;

    [Header("Phase Settings")]
    public BossPhase currentPhase = BossPhase.Phase1;
    public float phase2Threshold = 0.66f;
    public float phase3Threshold = 0.33f;

    private NavMeshAgent agent;
    private Transform currentTarget;
    private float attackTimer;
    private ThreatMeter threatMeter;
    private Node behaviorTree;
    private bool phase2Triggered = false;
    private bool phase3Triggered = false;
    private float lastHP;
    private float damageRate;
    private float dodgeCooldown = 5f;
    private float dodgeTimer;
    private int dodgesUsed = 0;
    private int currentComboCount = 0;
    private float lastAttackTime;
    private bool isAttacking = false;
    private QTableManager qTable;
    private RLRewardEstimator rewardEstimator;
    private TacticalDecisionFusion fusion;
    private bool isStunned = false;

    [Header("Taunt Settings")]
    private Transform tauntTarget = null;
    private float tauntExpireTime = 0f;
    private bool isTaunted = false;
    public float phase1TauntDuration = 5f;
    public float phase2TauntDuration = 3.75f; // 25% reduction
    public float phase3TauntDuration = 2.5f;  // 50% reduction
    public GameObject tauntEffectPrefab;



    void Start()
    {
        if (playerAgents == null)
        {
            playerAgents = new List<Transform>();
            Debug.LogWarning("Initialized empty playerAgents list");
        }

        stats = GetComponent<BossStats>();
        if (stats == null)
        {
            Debug.LogError("Missing BossStats component!");
            enabled = false; // Disable the script
            return;
        }

        if (isStunned) return;

        stats.ResetHP();
        lastHP = stats.currentHP;
        agent = GetComponent<NavMeshAgent>();
        agent.speed = stats.movementSpeed;

        threatMeter = new ThreatMeter(playerAgents);

        // Initialize Q-learning components
        qTable = new QTableManager();
        qTable.LoadFromDisk();
        rewardEstimator = new RLRewardEstimator(qTable);
        fusion = new TacticalDecisionFusion(threatMeter, rewardEstimator);

        behaviorTree = BuildBehaviorTree();
        attackTimer = stats.attackCooldown;
    }

    void Update()
    {
        // 1. Null check for playerAgents list
        if (playerAgents == null)
        {
            Debug.LogWarning("playerAgents list is null!");
            return;
        }

        // 2. Clean up null players safely
        playerAgents.RemoveAll(p => p == null || !p.gameObject.activeInHierarchy);

        // 3. Early exit if no valid players
        if (playerAgents.Count == 0)
        {
            currentTarget = null;
            return;
        }

        // 4. Null check for threatMeter
        if (threatMeter == null)
        {
            Debug.LogWarning("threatMeter is null! Initializing new one.");
            threatMeter = new ThreatMeter(playerAgents);
        }
        else
        {
            threatMeter.CleanupInactiveTargets();
        }

        // 5. Handle null currentTarget
        if (currentTarget != null && !currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget = null;
        }

        // 6. Null check for stats
        if (stats == null)
        {
            Debug.LogError("BossStats reference is null!");
            return;
        }

        if (isTaunted && Time.time >= tauntExpireTime)
        {
            isTaunted = false;
            tauntTarget = null;
        }

        // Main update logic (now safe from null references)
        HandlePhaseTransition();
        TrackDamageRate();
        TryDodge();

        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        currentTarget = ChooseBalancedTarget();

        // Null check for behaviorTree
        if (behaviorTree != null)
        {
            behaviorTree.Evaluate();
        }
        else
        {
            Debug.LogWarning("Behavior Tree is null!");
        }

        dodgeTimer -= Time.deltaTime;
        RegenerateStamina();

        if (Time.time - lastAttackTime > comboResetTime)
        {
            currentComboCount = 0;
        }
    }

    public IEnumerator ExecuteComboAttack(Transform target)
    {
        if (isStunned || isAttacking || target == null || stats == null) yield break;

        isAttacking = true;

        int hitsInCombo = Mathf.Min(currentComboCount + 1, maxComboHits);

        for (int i = 0; i < hitsInCombo; i++)
        {
            transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));

            if (attackEffects != null && attackEffects.Length > i && attackEffects[i] != null)
            {
                GameObject effect = Instantiate(
                    attackEffects[i],
                    transform.position + Vector3.up * 1.5f,
                    Quaternion.identity
                );
                effect.transform.LookAt(target);
                Destroy(effect, 0.5f);
            }

            stats.PerformAttack(target);
            rewardEstimator.LearnFromOutcome(target, 5f); // Reward for attacking

            if (i < hitsInCombo - 1)
                yield return new WaitForSeconds(timeBetweenComboHits);
        }

        currentComboCount = (currentComboCount + 1) % maxComboHits;
        lastAttackTime = Time.time;
        isAttacking = false;
    }

    // ===== Q-Learning and Threat System Methods =====
    public RLRewardEstimator GetRewardEstimator() => rewardEstimator;

    public void SaveQTable()
    {
        qTable?.SaveToDisk();
        Debug.Log("Boss QTable saved");
    }

    public void ResetThreatMeter()
    {
        threatMeter = new ThreatMeter(playerAgents);
        Debug.Log("Threat meter reset");
    }

    public void ReloadQTable()
    {
        qTable?.LoadFromDisk();
        Debug.Log("Boss QTable reloaded");
    }

    // ===== Combat and Movement Methods =====
    void HandlePhaseTransition()
    {
        float hpPercent = stats.currentHP / stats.maxHP;
        if (!phase2Triggered && hpPercent <= phase2Threshold)
        {
            phase2Triggered = true;
            EnterPhase(BossPhase.Phase2);
        }
        if (!phase3Triggered && hpPercent <= phase3Threshold)
        {
            phase3Triggered = true;
            EnterPhase(BossPhase.Phase3);
        }
    }

    void EnterPhase(BossPhase phase)
    {
        currentPhase = phase;
        switch (phase)
        {
            case BossPhase.Phase1:
                maxComboHits = 2;
                break;
            case BossPhase.Phase2:
                maxComboHits = 3;
                timeBetweenComboHits = 0.4f;
                stats.damagePerSecond += 20f;
                stats.movementSpeed += 1.5f;
                break;
            case BossPhase.Phase3:
                maxComboHits = 4;
                timeBetweenComboHits = 0.3f;
                comboResetTime = 4f;
                stats.attackCooldown *= 0.5f;
                stats.staminaRegenRate += 20f;
                break;
        }
        Debug.Log($"Boss transitioned to {phase}!");
    }

    void RegenerateStamina()
    {
        stats.stamina = Mathf.Min(100f, stats.stamina + stats.staminaRegenRate * Time.deltaTime);
    }

    public void TakeDamage(float damage, Transform attacker)
    {
        stats.currentHP -= damage;

        // Taunting player gets extra threat
        float threatAmount = damage;
        if (isTaunted && attacker == tauntTarget)
        {
            threatAmount *= 1.5f; // 50% more threat for taunter
        }

        threatMeter.AddThreat(attacker, threatAmount);

        if (stats.currentHP <= 0f) Die();
    }

    void Die()
    {
        SaveQTable();
        Debug.Log("Boss has been defeated.");
        Destroy(gameObject);
    }

    private Node BuildBehaviorTree()
    {
        // Create condition nodes first
        Node aoeCondition = new ConditionNode(() => !isStunned && IsMultiplePlayersClose());
        Node attackCondition = new ConditionNode(() => !isStunned && IsInAttackRange() && attackTimer <= 0 && !isAttacking);
        Node retreatCondition = new ConditionNode(() => !isStunned && ShouldRetreat());
        Node moveCondition = new ConditionNode(() => !isStunned);

        // Create action nodes
        Node aoeAttack = new AoeAttackNode(this, aoeRadius, aoeDamage);
        Node attack = new AttackNode(this, () => currentTarget);
        Node retreat = new RetreatNode(this);
        Node moveTo = new MoveToTargetNode(this, () => currentTarget);

        // Create sequences
        var aoeAttackSequence = new Sequence(new List<Node> { aoeCondition, aoeAttack });
        var attackSequence = new Sequence(new List<Node> { attackCondition, attack });
        var retreatSequence = new Sequence(new List<Node> { retreatCondition, retreat });

        // For moveTo with condition, use a sequence
        var moveSequence = new Sequence(new List<Node> { moveCondition, moveTo });

        // Create main selector
        return new Selector(new List<Node>
    {
        aoeAttackSequence,
        attackSequence,
        retreatSequence,
        moveSequence
    });
    }

    bool IsInAttackRange()
    {
        if (currentTarget == null) return false;
        float distance = Vector3.Distance(transform.position, currentTarget.position);
        return distance <= stats.attackRange;
    }

    bool IsMultiplePlayersClose()
    {
        int closePlayers = 0;
        foreach (var p in playerAgents)
        {
            if (p != null && Vector3.Distance(transform.position, p.position) <= aoeRadius * 1.5f)
            {
                closePlayers++;
            }
        }
        return closePlayers >= 2;
    }

    bool ShouldRetreat()
    {
        if (isTaunted) return false;
        return (stats.currentHP / stats.maxHP) < 0.2f;
    }

    private void ApplyTauntVisuals()
    {
        if (tauntEffectPrefab != null)
        {
            Instantiate(tauntEffectPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);
        }
    }

    public void ApplyTaunt(Transform taunter, float baseDuration)
    {
        // Calculate duration based on current phase
        float duration = currentPhase switch
        {
            BossPhase.Phase1 => Mathf.Min(baseDuration, phase1TauntDuration),
            BossPhase.Phase2 => Mathf.Min(baseDuration, phase2TauntDuration),
            BossPhase.Phase3 => Mathf.Min(baseDuration, phase3TauntDuration),
            _ => baseDuration
        };

        tauntTarget = taunter;
        tauntExpireTime = Time.time + duration;
        isTaunted = true;

        // Add significant threat to the taunter
        threatMeter.AddThreat(taunter, 50f);

        ApplyTauntVisuals();
        Debug.Log($"{name} is taunted by {taunter.name} for {duration} seconds (Phase: {currentPhase})");
    }
    public void ApplyStun(float duration)
    {
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent == null) yield break;

        // Store original values
        float originalSpeed = agent.speed;
        isStunned = true;

        // Apply stun effects
        agent.speed = 0;

        // Wait for stun duration
        yield return new WaitForSeconds(duration);

        // Restore original values
        agent.speed = originalSpeed;
        isStunned = false;
    }


    void TrackDamageRate()
    {
        damageRate = (lastHP - stats.currentHP) / Time.deltaTime;
        lastHP = stats.currentHP;
    }

    bool CanDodge()
    {
        switch (currentPhase)
        {
            case BossPhase.Phase1: return dodgesUsed < 1 && damageRate > 50f;
            case BossPhase.Phase2: return dodgesUsed < 2 && damageRate > 40f;
            case BossPhase.Phase3: return damageRate > 30f;
            default: return false;
        }
    }

    void TryDodge()
    {

        if (isStunned) return;
        if (!CanDodge() || dodgeTimer > 0f) return;
        Transform target = GetCurrentTarget();
        if (target == null) return;

        Vector3 dodgeDir = (target.position - transform.position).normalized;
        Vector3 dodgeTarget = transform.position + dodgeDir * 5f;
        if (NavMesh.SamplePosition(dodgeTarget, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.ResetPath();
            agent.SetDestination(hit.position);
            dodgesUsed++;
            dodgeTimer = dodgeCooldown;
            Debug.Log("Boss dodged!");
        }
    }

    Transform ChooseBalancedTarget()
    {
        // Taunt takes absolute priority if active
        if (isTaunted && Time.time < tauntExpireTime && tauntTarget != null)
        {
            // Verify target is still valid
            if (tauntTarget.gameObject.activeInHierarchy && playerAgents.Contains(tauntTarget))
            {
                return tauntTarget;
            }
            isTaunted = false; // Reset if target became invalid
        }

        if (playerAgents == null || playerAgents.Count == 0)
            return null;

        // Phase-based targeting weights
        float threatWeight = 0.6f;
        float rewardWeight = 0.4f;

        if (currentPhase == BossPhase.Phase3)
        {
            threatWeight = 0.3f;
            rewardWeight = 0.7f;
        }

        return fusion.GetBestTarget(playerAgents, threatWeight, rewardWeight);
    }

    public Transform GetCurrentTarget() => currentTarget;
    public List<Transform> GetAllPlayers() => playerAgents;
    public NavMeshAgent GetAgent() => agent;
}