using UnityEngine;
using UnityEngine.UI;
using ReinforcementLearning;
using System.Collections.Generic;
using UnityEngine.AI;

public class NewGenerationManager : MonoBehaviour
{
    [Header("Boss References")]
    public BossAI boss;
    public Transform bossStartPoint;

    [Header("Player References")]
    public AITankController tankAI;
    public Transform tankStartPoint;

    public AISwordmasterController swordmasterAI;
    public Transform swordmasterStartPoint;

    public AIHealerController healerAI;
    public Transform healerStartPoint;

    [Header("UI")]
    public Button resetButton;

    [Header("Settings")]
    public float spawnRadius = 3f;

    private RLRewardEstimator rewardEstimator;
    private float bossStartingHP;

    void Start()
    {
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetGame);
        else
            Debug.LogError("Reset Button not assigned!");
    }

    public void ResetGame()
    {
        Debug.Log("[Reset] New Generation starting...");

        if (rewardEstimator == null && boss != null)
            rewardEstimator = boss.GetRewardEstimator();

        if (rewardEstimator == null)
        {
            Debug.LogError("Reward Estimator is missing!");
            return;
        }

        // Capture boss original HP
        bossStartingHP = boss.stats.maxHealth;

        // 1. Reward boss for players defeated
        RewardBossForDefeatedPlayers();

        // 2. Reward players for boss damage
        RewardPlayersForBossDamage();

        // 3. Save boss Q-table
        boss.SaveQTable();

        // 4. Reset Boss
        RespawnAgent(boss.transform, bossStartPoint);
        boss.gameObject.SetActive(true);
        if (boss.stats != null)
        {
            boss.stats.Revive();
            boss.stats.stamina = 100f;
        }
        boss.GetAgent().ResetPath();
        boss.playerAgents.Clear();

        // 5. Reset Players
        ResetPlayer(tankAI, tankStartPoint);
        ResetPlayer(swordmasterAI, swordmasterStartPoint);
        ResetPlayer(healerAI, healerStartPoint);

        boss.playerAgents.Add(tankAI.transform);
        boss.playerAgents.Add(swordmasterAI.transform);
        boss.playerAgents.Add(healerAI.transform);
        boss.GetAgent().enabled = true;
        boss.GetAgent().ResetPath();
        boss.GetAgent().isStopped = false;
        // 6. Reset Threat + Learning
        boss.ResetThreatMeter();
        boss.ReloadQTable();

        Debug.Log("[Reset] New Generation Complete! Fight Ready.");
    }

    private void RewardBossForDefeatedPlayers()
    {
        RewardIfDead(tankAI, "Tank");
        RewardIfDead(swordmasterAI, "Swordmaster");
        RewardIfDead(healerAI, "Healer");
    }

    private void RewardIfDead(AIController player, string name)
    {
        if (player != null && !player.gameObject.activeInHierarchy)
        {
            rewardEstimator.LearnFromOutcome(player.transform, 50f);
            Debug.Log($"Boss rewarded for defeating {name}.");
        }
    }

    private void RewardPlayersForBossDamage()
    {
        if (boss == null || boss.stats == null) return;

        float damageDealt = bossStartingHP - boss.stats.currentHealth;
        float percentDamage = damageDealt / bossStartingHP;
        float reward = percentDamage * 100f;

        Debug.Log($"Players dealt {percentDamage * 100f}% damage. Rewarding {reward} points.");

        // Assuming your players have LearnFromReward(float)
        if (tankAI != null && tankAI is QLearningTankController tankQL)
        {
            tankQL.LearnFromReward(reward);
        }
        if (swordmasterAI != null && swordmasterAI is QLearningSwordmasterController swordmasterQL)
        {
            swordmasterQL.LearnFromReward(reward);
        } 
        if (healerAI != null && healerAI is QLearningHealerController healerQL)
        {
            healerQL.LearnFromReward(reward);
        }
    }

    private void ResetPlayer(AIController player, Transform spawnPoint)
    {
        if (player == null || spawnPoint == null) return;

        player.gameObject.SetActive(true);
        RespawnAgent(player.transform, spawnPoint);

        if (player.GetStats() != null)
            player.GetStats().Revive();
    }

    private void RespawnAgent(Transform agentTransform, Transform spawnPoint)
    {
        if (agentTransform == null || spawnPoint == null) return;

        NavMeshAgent agent = agentTransform.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false; // <-- Disable agent first
        }

        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 newPos = spawnPoint.position + new Vector3(randomOffset.x, 0f, randomOffset.y);

        agentTransform.position = newPos;
        agentTransform.rotation = spawnPoint.rotation;

        if (agent != null)
        {
            agent.enabled = true; // <-- Reactivate agent after teleport
            agent.ResetPath();
        }

        CharacterStats stats = agentTransform.GetComponent<CharacterStats>();
        if (stats != null)
        {
            stats.Revive();
        }

        agentTransform.gameObject.SetActive(true);
    }
}
