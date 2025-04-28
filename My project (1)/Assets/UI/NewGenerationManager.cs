using UnityEngine;
using UnityEngine.UI;
using ReinforcementLearning;

public class NewGenerationManager : MonoBehaviour
{
    [Header("Player AIs")]
    public QLearningTankController tankAI;
    public QLearningSwordmasterController swordmasterAI;
    public QLearningHealerController healerAI;

    [Header("Boss")]
    public QTableManager bossQTableManager;
    public Transform bossTransform;

    [Header("Spawn Settings")]
    public Transform tankSpawnPoint;
    public Transform swordmasterSpawnPoint;
    public Transform healerSpawnPoint;
    public Transform bossSpawnPoint;
    public float spawnRadius = 3f;

    [Header("UI Button")]
    public Button newGenerationButton;

    private void Start()
    {
        if (newGenerationButton != null)
            newGenerationButton.onClick.AddListener(HandleNewGeneration);
    }

    public void HandleNewGeneration()
    {
        Debug.Log("[New Generation] Loading previous Q-Tables and respawning...");

        if (tankAI != null)
        {
            if (!tankAI.TryLoadQTable())
                Debug.LogWarning("[Tank] No saved Q-Table found.");
            RespawnAgent(tankAI.transform, tankSpawnPoint);
        }

        if (swordmasterAI != null)
        {
            if (!swordmasterAI.TryLoadQTable())
                Debug.LogWarning("[Swordmaster] No saved Q-Table found.");
            RespawnAgent(swordmasterAI.transform, swordmasterSpawnPoint);
        }

        if (healerAI != null)
        {
            if (!healerAI.TryLoadQTable())
                Debug.LogWarning("[Healer] No saved Q-Table found.");
            RespawnAgent(healerAI.transform, healerSpawnPoint);
        }

        if (bossQTableManager != null)
        {
            bossQTableManager.LoadFromDisk();
        }

        if (bossTransform != null)
        {
            RespawnAgent(bossTransform, bossSpawnPoint);
        }

        Debug.Log("[New Generation] Respawn complete. Smarter AI ready for battle!");
    }

    private void RespawnAgent(Transform agentTransform, Transform spawnPoint)
    {
        if (agentTransform == null || spawnPoint == null) return;

        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 newPos = spawnPoint.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

        agentTransform.position = newPos;
        agentTransform.rotation = spawnPoint.rotation;

        // ALSO revive if they have CharacterStats!
        CharacterStats stats = agentTransform.GetComponent<CharacterStats>();
        if (stats != null)
        {
            stats.Revive();
        }

        agentTransform.gameObject.SetActive(true); // Ensure the object is active
    }
}
