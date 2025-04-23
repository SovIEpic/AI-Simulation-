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
    private ThreatMeter threatMeter;
    private Node behaviorTree;

    private QTableManager qTable;
    private RLRewardEstimator rewardEstimator;
    private TacticalDecisionFusion fusion;

    public BossPhase currentPhase = BossPhase.Phase1;
    public float phase2Threshold = 0.66f;
    public float phase3Threshold = 0.33f;
    private bool phase2Triggered = false;
    private bool phase3Triggered = false;

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
        rewardEstimator = new RLRewardEstimator(qTable);
        fusion = new TacticalDecisionFusion(threatMeter, rewardEstimator);

        behaviorTree = BuildBehaviorTree();
        attackTimer = stats.attackCooldown;
    }

    void Update()
    {
        if (playerAgents.Count == 0) return;

        HandlePhaseTransition();
        TrackDamageRate();
        TryDodge();

        currentTarget = ChooseBalancedTarget();
        behaviorTree.Evaluate();

        dodgeTimer -= Time.deltaTime;
        RegenerateStamina();
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
