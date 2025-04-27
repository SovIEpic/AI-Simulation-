using UnityEngine;
using UnityEngine.UI;
using ReinforcementLearning;

public class GameResetManager : MonoBehaviour
{
    [Header("References")]
    public BossAI boss;
    public Transform bossStartPoint;

    [Header("Single Player Setup")]
    public PlayerAI player; // The regular DPS player
    public Transform playerStartPoint;

    [Header("Single Tank Setup")]
    public AITankController tank; // The tank
    public Transform tankStartPoint;

    public Button resetButton;

    private RLRewardEstimator rewardEstimator;

    void Start()
    {
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

        // Reward boss if player is dead
        if (player != null && !player.gameObject.activeInHierarchy)
        {
            rewardEstimator.LearnFromOutcome(player.transform, 50f);
            Debug.Log($"Rewarded Boss for defeating {player.name}");
        }

        // Reward boss if tank is dead
        if (tank != null && !tank.gameObject.activeInHierarchy)
        {
            rewardEstimator.LearnFromOutcome(tank.transform, 70f);
            Debug.Log($"Rewarded Boss for defeating {tank.name}");
        }

        Debug.Log("Saving Q-Table...");
        boss.SaveQTable();

        // Reset Boss]
        if (boss != null && bossStartPoint != null)
        {
            boss.transform.position = bossStartPoint.position;
            boss.stats.ResetHP();
            boss.stats.stamina = 100f;
            boss.GetAgent().ResetPath();
            boss.playerAgents.Clear();
        }
        // Reset Player
        if (player != null && playerStartPoint != null)
        {
            player.gameObject.SetActive(true);
            player.transform.position = playerStartPoint.position;
            player.currentHP = player.maxHP;

            boss.playerAgents.Add(player.transform);
        }

        // Reset Tank
        if (tank != null && tankStartPoint != null)
        {
            tank.gameObject.SetActive(true);
            tank.transform.position = tankStartPoint.position;
            var stats = tank.GetComponent<CharacterStats>();
            if (stats != null)
                stats.currentHealth = stats.maxHealth;

            boss.playerAgents.Add(tank.transform);
        }

        // Refresh Boss memory
        boss.ResetThreatMeter();
        boss.ReloadQTable();

        Debug.Log("Game Reset Complete. Boss will now resume learned behavior.");
    }
}
