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

    [Header("Combat Settings")]
    public int maxComboHits = 3;
    public float comboResetTime = 3f;
    public float timeBetweenComboHits = 0.5f;
    public GameObject[] attackEffects;
    public float aoeRadius = 4f;
    public float aoeDamage = 30f;

    [Header("Phase Settings")]
    public BossPhase currentPhase = BossPhase.Phase1;
    public float phase2Threshold = 0.66f;
    public float phase3Threshold = 0.33f;

    private NavMeshAgent agent;
    private Transform currentTarget;
    private Node behaviorTree;
    private ThreatMeter threatMeter;
    private QTableManager qTable;
    private RLRewardEstimator rewardEstimator;
    private TacticalDecisionFusion fusion;

    private bool phase2Triggered = false;
    private bool phase3Triggered = false;
    private float attackTimer;
    private float lastAttackTime;
    private float lastHP;
    private bool isAttacking = false;
    private bool isTaunted = false;
    private Transform tauntTarget;
    private float tauntExpireTime;
    private bool isStunned = false;

    void Start()
    {
        if (playerAgents == null) playerAgents = new List<Transform>();

        stats = GetComponent<BossStats>();
        agent = GetComponent<NavMeshAgent>();
        agent.speed = stats.movementSpeed;

        threatMeter = new ThreatMeter(playerAgents, 1f);

        qTable = new QTableManager();
        qTable.LoadFromDisk();

        rewardEstimator = new RLRewardEstimator(qTable);
        fusion = new TacticalDecisionFusion(threatMeter, rewardEstimator);

        lastHP = stats.currentHP;

        behaviorTree = BuildBehaviorTree();
        attackTimer = stats.attackCooldown;
    }

    void Update()
    {
        if (playerAgents == null || playerAgents.Count == 0)
        {
            currentTarget = null;
            return;
        }

        threatMeter.DecayThreat(Time.deltaTime);

        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        currentTarget = ChooseBalancedTarget();
        behaviorTree?.Evaluate();

        if (Time.time - lastAttackTime > comboResetTime)
            isAttacking = false;
    }

    public IEnumerator ExecuteComboAttack(Transform target)
    {
        if (isStunned || isAttacking || target == null) yield break;

        isAttacking = true;
        int hits = Mathf.Min(maxComboHits, 3);

        for (int i = 0; i < hits; i++)
        {
            stats.PerformAttack(target);
            rewardEstimator.LearnFromOutcome(target, 5f);
            yield return new WaitForSeconds(timeBetweenComboHits);
        }

        lastAttackTime = Time.time;
    }
    // Already partially exist, but confirming:
    public void ResetThreatMeter()
    {
        threatMeter = new ThreatMeter(playerAgents, 1f);
        Debug.Log("Threat meter reset");
    }

    public void ReloadQTable()
    {
        qTable?.LoadFromDisk();
        Debug.Log("Boss QTable reloaded");
    }

    public GameObject aoeEffectPrefab;

    public void ApplyStun(float duration)
    {
        StartCoroutine(StunRoutine(duration));
    }
    public void FreezeMovement()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    public void ResumeMovement()
    {
        if (agent != null)
        {
            agent.isStopped = false;
        }
    }

    private IEnumerator StunRoutine(float duration)
    {
        if (agent == null) yield break;

        isStunned = true;
        float originalSpeed = agent.speed;
        agent.speed = 0f;

        yield return new WaitForSeconds(duration);

        agent.speed = originalSpeed;
        isStunned = false;
    }

    public void TakeDamage(float damage, Transform attacker)
    {
        stats.currentHP -= damage;

        if (isTaunted && attacker == tauntTarget)
            damage *= 1.5f;

        threatMeter.AddDamageThreat(attacker, damage);

        if (stats.currentHP <= 0)
            Die();
    }

    public void ReceiveCC(Transform attacker)
    {
        threatMeter.AddCCThreat(attacker,10f);
    }

    public void ReceiveHealingThreat(Transform healer, float healAmount)
    {
        threatMeter.AddHealingThreat(healer, healAmount);
    }

    private void Die()
    {
        SaveQTable();
        Debug.Log("Boss defeated");
        gameObject.SetActive(false);
    }

    private Node BuildBehaviorTree()
    {
        var attackSequence = new Sequence(new List<Node>
        {
            new ConditionNode(() => !isStunned && IsInAttackRange() && attackTimer <= 0),
            new AttackNode(this, () => currentTarget)
        });

        var moveSequence = new Sequence(new List<Node>
        {
            new ConditionNode(() => !isStunned && currentTarget != null),
            new MoveToTargetNode(this, () => currentTarget)
        });

        return new Selector(new List<Node> { attackSequence, moveSequence });
    }

    private bool IsInAttackRange()
    {
        if (currentTarget == null) return false;
        return Vector3.Distance(transform.position, currentTarget.position) <= stats.attackRange;
    }

    private Transform ChooseBalancedTarget()
    {
        if (isTaunted && tauntTarget != null && tauntTarget.gameObject.activeInHierarchy)
            return tauntTarget;
        List<Transform> alivePlayers = GetAllPlayers();
        if(alivePlayers.Count == 0) return null;
        return fusion.GetBestTarget(alivePlayers,0.6f,0.4f);
    }

    public void SaveQTable()
    {
        qTable?.SaveToDisk();
    }

    public void ApplyTaunt(Transform source, float duration)
    {
        tauntTarget = source;
        tauntExpireTime = Time.time + duration;
        isTaunted = true;
    }

    public RLRewardEstimator GetRewardEstimator() => rewardEstimator;

    public List<Transform> GetAllPlayers() => playerAgents;
    public List<Transform> GetAlivePlayers()
    {
        List<Transform> alivePlayers = new List<Transform>();
        foreach (var player in playerAgents)
        {
            if (player != null && player.gameObject.activeInHierarchy)
            {
                alivePlayers.Add(player);
            }
        }
        return alivePlayers;
    }

    public NavMeshAgent GetAgent() => agent;
    public Transform GetCurrentTarget() => currentTarget;
}