using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ReinforcementLearning; // make sure your namespace matches

public class GameResetManager : MonoBehaviour
{
    [Header("References")]
    public BossAI boss;
    public Transform bossStartPoint;
    public List<PlayerAI> players;
    public List<Transform> playerStartPoints;
    public Button resetButton;

    private RLRewardEstimator rewardEstimator;

    void Start()
    {
        // Make sure button is hooked up
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetGame);
        else
            Debug.LogError("Reset Button not assigned!");
    }

    public void ResetGame()
    {
        if (rewardEstimator == null && boss != null)
        {
            rewardEstimator = boss.GetRewardEstimator();
        }

        if (rewardEstimator == null)
        {
            Debug.LogError("rewardEstimator is null! Check initialization.");
            return;
        }

        Debug.Log("Starting AI learning reward phase...");

        // Give rewards to boss for any defeated (inactive) players
        foreach (var player in players)
        {
            if (!player.gameObject.activeInHierarchy)
            {
                rewardEstimator.LearnFromOutcome(player.transform, 50f);
                Debug.Log($"Rewarded Boss for defeating {player.name}");
            }
        }

        Debug.Log("Saving Q-Table...");
        boss.SaveQTable();


        var agent = boss.GetAgent();
        agent.enabled = false; // 1. Disable the NavMeshAgent
        boss.transform.position = bossStartPoint.position; // 2. Move the boss
        agent.enabled = true; // 3. Re-enable agent
        agent.Warp(bossStartPoint.position); // 4. Warp properly onto NavMesh
        agent.ResetPath(); // 5. Now safe to reset path
        agent.isStopped = false; // 6. Resume

        boss.stats.ResetHP();
        boss.stats.stamina = 100f;
        boss.playerAgents.Clear();

        //Reset Players
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null && playerStartPoints[i] != null)
            {
                players[i].gameObject.SetActive(true);
                players[i].transform.position = playerStartPoints[i].position;
                players[i].currentHP = players[i].maxHP;

                boss.playerAgents.Add(players[i].transform);
            }
        }

        //Reinitialize threat and reload learning
        boss.ResetThreatMeter();
        boss.ReloadQTable();

        Debug.Log("Game Reset Complete. Boss will now resume learned behavior.");
    }

}
