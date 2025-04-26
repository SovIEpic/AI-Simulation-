using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using BehaviorTree.Actions;
using ReinforcementLearning;
using Utilities;
using BehaviorTree;

public enum BossPhase { Phase1, Phase2, Phase3 }

[RequireComponent(typeof(NavMeshAgent))]
public class BossAI : MonoBehaviour
{
    public BossStats stats;
    public List<Transform> playerAgents;
    public GameObject hpBarPrefab;

    private NavMeshAgent agent;
    private Transform currentTarget;
    private float attackTimer;
    public ThreatMeter threatMeter;
    private Node behaviorTree;

    private QTableManager qTable;
    private RLRewardEstimator rewardEstimator;
    private TacticalDecisionFusion fusion;
    
    [Header("Attack Cooldown Settings")]
    public float attackCooldownTime = 6.0f; // seconds
    private float attackCooldownTimer = 0f;
    private bool isAttackOnCooldown = false;

    public BossPhase currentPhase = BossPhase.Phase1;
    public float phase2Threshold = 0.66f;
    public float phase3Threshold = 0.33f;
    private bool phase2Triggered = false;
    private bool phase3Triggered = false;

    [Header("Movement AI Settings")]
    public float walkSpeed = 2f;
    public float approachSpeed = 5f;
    public float attackRange = 3.0f;
    public float pauseDurationMin = 1f;
    public float pauseDurationMax = 2.5f;

    private float pauseTimer = 0f;
    private bool isPausing = false;


    [Header("AOE Settings")]
    public float aoeRadius = 4f;
    public float aoeDamage = 30f;

    private float lastHP;
    private float damageRate;
    private float dodgeCooldown = 5f;
    private float dodgeTimer;
    private int dodgesUsed = 0;

    void Start()
    {
        stats.ResetHP();
        lastHP = stats.currentHP;
        agent = GetComponent<NavMeshAgent>();
        agent.speed = stats.movementSpeed;

        threatMeter = new ThreatMeter(playerAgents);
        qTable = new QTableManager();
        qTable.LoadFromDisk();

        rewardEstimator = new RLRewardEstimator(qTable);
        fusion = new TacticalDecisionFusion(threatMeter, rewardEstimator);

        behaviorTree = BuildBehaviorTree();
        attackTimer = stats.attackCooldown;
    }
    public RLRewardEstimator GetRewardEstimator() => rewardEstimator;
    public void SaveQTable() => qTable.SaveToDisk();
    void Update()
    {
        if (isAttackOnCooldown)
        {
            attackCooldownTime -= Time.deltaTime;
            if (attackCooldownTime <= 0f) { 
                isAttackOnCooldown = false;
            }
        }

        playerAgents.RemoveAll(p => p == null || !p.gameObject.activeInHierarchy);

        threatMeter.CleanupInactiveTargets();

        if (isPausing)
        {
            pauseTimer -= Time.deltaTime;
            if(pauseTimer < 0f)
            {
                isPausing = false;
            }
            return;
        }
        if(currentTarget != null && !currentTarget.gameObject.activeInHierarchy)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.position);

            if(distance > attackRange)
            {
                agent.speed = walkSpeed;
                agent.SetDestination(currentTarget.position);

                if(Random.value < 0.005f)
                {
                    StartPause();
                }
            }
            else
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;

                Vector3 direction = (currentTarget.position - transform.position).normalized;
                direction.y = 0f;
                if(direction != Vector3.zero)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
                }
            }
        }

        if (playerAgents.Count == 0) return;

        HandlePhaseTransition();
        TrackDamageRate();
        TryDodge();

        currentTarget = ChooseBalancedTarget();
        behaviorTree.Evaluate();

        dodgeTimer -= Time.deltaTime;
        RegenerateStamina();

        if (currentTarget != null && currentTarget.gameObject.activeInHierarchy) {
            float distance = Vector3.Distance(transform.position, currentTarget.position);
            if(distance <= attackRange && !isAttackOnCooldown)
            {
                PerformAttack();
            }
        }
    }
    private void PerformAttack()
    {
        var playerAI = currentTarget.GetComponent<PlayerAI>();
        if (playerAI != null)
        {
            playerAI.TakeDamage(stats.damagePerSecond); 
        }

        Debug.Log($"Boss attacked {currentTarget.name}!");

        isAttackOnCooldown = true;
        attackCooldownTimer = attackCooldownTime;
    }

    private void StartPause()
    {
        isPausing = true;
        pauseTimer = Random.Range(pauseDurationMin, pauseDurationMax);
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }
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
            case BossPhase.Phase2:
                stats.damagePerSecond += 20f;
                stats.movementSpeed += 1.5f;
                break;
            case BossPhase.Phase3:
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
        threatMeter.AddThreat(attacker, damage);
        if (stats.currentHP <= 0f) Die();
    }

    void Die()
    {
        qTable.SaveToDisk();
        Debug.Log("Boss has been defeated.");
        Destroy(gameObject);
    }

    private Node BuildBehaviorTree()
    {
        return new Selector(new List<Node> {
            new AoeAttackNode(this, aoeRadius, aoeDamage),
            new AttackNode(this, () => currentTarget),
            new MoveToTargetNode(this, () => currentTarget),
            new RetreatNode(this)
        });
    }

    public Transform GetCurrentTarget() => currentTarget;
    public List<Transform> GetAllPlayers() => playerAgents;
    public NavMeshAgent GetAgent() => agent;

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

    public void ReloadQTable()
    {
        qTable.LoadFromDisk();
        Debug.Log("Q-table reloaded manually.");
    }

    public void ResetThreatMeter()
    {
        threatMeter = new ThreatMeter(playerAgents);
    }


    void TryDodge()
    {
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
        float alpha = 0.6f, beta = 0.4f;
        if (currentPhase == BossPhase.Phase3) { alpha = 0.3f; beta = 0.7f; }

        return fusion.GetBestTarget(playerAgents, alpha, beta);
    }
}
