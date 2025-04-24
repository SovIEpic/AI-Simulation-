using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class QLearningTankController : AITankController
{
    [Header("QLearning Settings")]
    public float learningRate = 0.1f;
    public float discountFactor = 0.9f;
    public float explorationRate = 0.3f;
    public float explorationDecay = 0.995f;
    public int maxMemorySize = 1000;

    private Dictionary<string, Dictionary<int, float>> qTable;
    private List<QLearningMemory> memory;
    private int lastAction;
    private float lastReward;
    private string lastState;
    private float lastActionTime;
    private const float minActionInterval = 0.5f;

    [System.Serializable]
    private class QTableSaveData
    {
        public string version = "1.0";
        public List<string> states = new List<string>();
        public List<List<int>> actions = new List<List<int>>();
        public List<List<float>> values = new List<List<float>>();
        public int totalExperiences;
    }

    protected override void Start()
    {
        base.Start();
        InitializeQTable();

        if (!TryLoadQTable())
        {
            Debug.Log("Initializing new Q-learning table");
            qTable = new Dictionary<string, Dictionary<int, float>>();
        }
    }

    protected override void Update()
    {
        if (bossTarget == null)
        {
            bossTarget = GameObject.FindGameObjectWithTag("Boss")?.transform;
            if (bossTarget == null) return;
        }

        // Get current state
        string currentState = GetState();

        // Choose action (explore or exploit)
        int action = ChooseAction(currentState);

        // Execute action
        ExecuteAction(action);

        // If no action was taken (due to cooldowns), move toward boss
        if (action != 4 &&
            Time.time - lastActionTime > minActionInterval &&
            Vector3.Distance(transform.position, bossTarget.position) > attackRange)
        {
            MoveTowardBoss();
        }

        // Calculate reward
        float reward = CalculateReward();

        // Store experience in memory
        if (!string.IsNullOrEmpty(lastState))
        {
            memory.Add(new QLearningMemory(lastState, lastAction, reward, currentState));
            if (memory.Count > maxMemorySize)
            {
                memory.RemoveAt(0);
            }
        }

        // Learn from experience
        Learn();

        // Update exploration rate
        explorationRate *= explorationDecay;
        explorationRate = Mathf.Max(0.01f, explorationRate);

        // Save for next frame
        lastState = currentState;
        lastAction = action;
        lastReward = reward;
        lastActionTime = Time.time;
    }

    private string GetState()
    {
        if (bossTarget == null) return "no_target";

        float distance = Vector3.Distance(transform.position, bossTarget.position);
        float healthPercentage = stats.currentHealth / stats.maxHealth;
        bool canTaunt = Time.time >= lastTauntTime + tauntCooldown;
        bool canBash = Time.time >= lastShieldBashTime + shieldBashCooldown;
        bool canBlock = Time.time >= lastBlockTime + blockCooldown;
        bool canAttack = Time.time >= lastAttackTime + attackCooldown;
        bool inAttackRange = distance <= attackRange;
        bool inBashRange = distance <= shieldBashRange;
        bool inTauntRange = distance <= tauntRange;

        return $"dist_{Mathf.Round(distance)}_hp_{Mathf.Round(healthPercentage * 10)}_" +
               $"taunt_{canTaunt}_bash_{canBash}_block_{canBlock}_" +
               $"atk_{canAttack}_inAtk_{inAttackRange}_inBash_{inBashRange}_inTaunt_{inTauntRange}";
    }

    private int ChooseAction(string state)
    {
        // Explore (random action)
        if (Random.Range(0f, 1f) < explorationRate)
        {
            return Random.Range(0, 5); // 5 possible actions
        }

        // Exploit (best known action)
        if (!qTable.ContainsKey(state))
        {
            qTable[state] = new Dictionary<int, float>();
        }

        // Initialize Q-values for this state if not exists
        for (int i = 0; i < 5; i++)
        {
            if (!qTable[state].ContainsKey(i))
            {
                qTable[state][i] = 0f;
            }
        }

        // Find best action
        int bestAction = 0;
        float bestValue = float.MinValue;
        foreach (var entry in qTable[state])
        {
            if (entry.Value > bestValue)
            {
                bestValue = entry.Value;
                bestAction = entry.Key;
            }
        }

        return bestAction;
    }

    private void ExecuteAction(int action)
    {
        switch (action)
        {
            case 0: // Taunt
                if (Time.time >= lastTauntTime + tauntCooldown)
                {
                    TryTaunt();
                }
                break;
            case 1: // Shield Bash
                if (Time.time >= lastShieldBashTime + shieldBashCooldown)
                {
                    TryShieldBash();
                }
                break;
            case 2: // Block
                if (Time.time >= lastBlockTime + blockCooldown)
                {
                    StartBlocking();
                }
                break;
            case 3: // Attack
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    TryAttack();
                }
                break;
            case 4: // Move toward boss
                MoveTowardBoss();
                break;
        }
    }

    private void MoveTowardBoss()
    {
        if (bossTarget == null || movement == null) return;

        Vector3 bossPosition = new Vector3(
            bossTarget.position.x,
            transform.position.y,
            bossTarget.position.z
        );

        Vector3 direction = (bossPosition - transform.position).normalized;
        movement.Move(direction * tankSpeed);
    }

    private float CalculateReward()
    {
        float reward = 0f;
        float distance = bossTarget ? Vector3.Distance(transform.position, bossTarget.position) : float.MaxValue;

        // Positive rewards
        if (bossTarget.GetComponent<CharacterStats>().currentHealth <= 0)
            reward += 100f;

        // Negative rewards
        if (stats.currentHealth <= 0)
            reward -= 100f;

        // Action-specific rewards
        switch (lastAction)
        {
            case 0 when Time.time - lastTauntTime < 0.1f: // Taunt
                reward += 5f;
                break;
            case 1 when Time.time - lastShieldBashTime < 0.1f: // Bash
                reward += 10f;
                break;
            case 4: // Movement
                // Reward for closing distance when not in attack range
                if (distance > attackRange && distance < detectionRange)
                    reward += 0.1f * (detectionRange - distance);
                break;
        }

        // Penalty for being too far
        if (distance > detectionRange)
            reward -= 1f;

        // Health-based penalty
        if (stats.currentHealth < stats.maxHealth * 0.5f)
            reward -= 2f;

        return reward;
    }

    private void Learn()
    {
        // Sample random experiences from memory
        int batchSize = Mathf.Min(32, memory.Count);
        for (int i = 0; i < batchSize; i++)
        {
            int randomIndex = Random.Range(0, memory.Count);
            QLearningMemory experience = memory[randomIndex];

            // Get max Q for next state
            float maxNextQ = 0f;
            if (qTable.ContainsKey(experience.nextState))
            {
                foreach (var entry in qTable[experience.nextState])
                {
                    if (entry.Value > maxNextQ)
                    {
                        maxNextQ = entry.Value;
                    }
                }
            }

            // Update Q-value
            if (!qTable.ContainsKey(experience.state))
            {
                qTable[experience.state] = new Dictionary<int, float>();
            }
            if (!qTable[experience.state].ContainsKey(experience.action))
            {
                qTable[experience.state][experience.action] = 0f;
            }

            // Q-learning formula
            qTable[experience.state][experience.action] =
                (1f - learningRate) * qTable[experience.state][experience.action] +
                learningRate * (experience.reward + discountFactor * maxNextQ);
        }
    }

    private void InitializeQTable()
    {
        qTable = new Dictionary<string, Dictionary<int, float>>();
        memory = new List<QLearningMemory>(maxMemorySize);
        lastAction = -1;
        lastActionTime = Time.time;
    }

    private string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, "qlearning_tank_save_v2.json");
    }

    public void SaveQTable()
    {
        if (qTable == null || qTable.Count == 0)
        {
            Debug.LogWarning("QTable is empty - nothing to save");
            return;
        }

        try
        {
            var saveData = new QTableSaveData();
            int totalEntries = 0;

            foreach (var stateEntry in qTable.Where(entry => entry.Value != null && entry.Value.Count > 0))
            {
                saveData.states.Add(stateEntry.Key);

                var actionList = new List<int>();
                var valueList = new List<float>();

                foreach (var actionEntry in stateEntry.Value)
                {
                    actionList.Add(actionEntry.Key);
                    valueList.Add(actionEntry.Value);
                    totalEntries++;
                }

                saveData.actions.Add(actionList);
                saveData.values.Add(valueList);
            }

            saveData.totalExperiences = totalEntries;

            if (saveData.states.Count == 0)
            {
                Debug.LogWarning("No valid data to save");
                return;
            }

            string json = JsonUtility.ToJson(saveData, true);
            string savePath = GetSavePath();

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(savePath));

            // Write to temporary file first
            string tempPath = savePath + ".tmp";
            File.WriteAllText(tempPath, json);

            // Atomic file replacement
            if (File.Exists(savePath)) File.Delete(savePath);
            File.Move(tempPath, savePath);

            Debug.Log($"Saved Q-table with {saveData.states.Count} states and {totalEntries} entries");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Save failed: {e.Message}\n{e.StackTrace}");
        }
    }

    public bool TryLoadQTable()
    {
        string savePath = GetSavePath();

        if (!File.Exists(savePath))
        {
            Debug.Log("No save file found at: " + savePath);
            return false;
        }

        try
        {
            string json = File.ReadAllText(savePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("Save file is empty");
                return false;
            }

            QTableSaveData saveData = JsonUtility.FromJson<QTableSaveData>(json);
            if (saveData == null)
            {
                Debug.LogWarning("Failed to parse save file");
                return false;
            }

            // Validate data structure
            if (saveData.states == null || saveData.actions == null || saveData.values == null ||
                saveData.states.Count != saveData.actions.Count ||
                saveData.states.Count != saveData.values.Count)
            {
                Debug.LogWarning("Save data structure is invalid");
                return false;
            }

            // Rebuild Q-table with validation
            var newQTable = new Dictionary<string, Dictionary<int, float>>();
            int validStates = 0;

            for (int i = 0; i < saveData.states.Count; i++)
            {
                if (string.IsNullOrEmpty(saveData.states[i]) ||
                    saveData.actions[i] == null ||
                    saveData.values[i] == null ||
                    saveData.actions[i].Count != saveData.values[i].Count)
                {
                    Debug.LogWarning($"Skipping invalid state at index {i}");
                    continue;
                }

                var stateActions = new Dictionary<int, float>();
                for (int j = 0; j < saveData.actions[i].Count; j++)
                {
                    stateActions[saveData.actions[i][j]] = saveData.values[i][j];
                }

                newQTable[saveData.states[i]] = stateActions;
                validStates++;
            }

            if (validStates == 0)
            {
                Debug.LogWarning("No valid states found in save file");
                return false;
            }

            qTable = newQTable;
            Debug.Log($"Loaded Q-table with {validStates} valid states and ~{saveData.totalExperiences} experiences");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Load failed: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    public void HandleCorruptedSave()
    {
        Debug.Log("Handling corrupted save file...");
        string savePath = GetSavePath();

        try
        {
            if (File.Exists(savePath))
            {
                string backupPath = savePath + ".corrupted";
                File.Move(savePath, backupPath);
                Debug.Log($"Moved corrupted save to: {backupPath}");
            }

            qTable = new Dictionary<string, Dictionary<int, float>>();
            Debug.Log("Initialized fresh Q-table");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to handle corrupted save: {e.Message}");
        }
    }

    private void OnApplicationQuit()
    {
        SaveQTable();
    }

    private void OnDestroy()
    {
        SaveQTable();
    }

    public void ResetLearning()
    {
        qTable = new Dictionary<string, Dictionary<int, float>>();
        string savePath = GetSavePath();

        if (File.Exists(savePath))
        {
            try
            {
                File.Delete(savePath);
                Debug.Log("Q-learning reset complete - save file deleted");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete save file: {e.Message}");
            }
        }
        else
        {
            Debug.Log("Q-learning reset complete - no save file found");
        }
    }

    void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Movement decision range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Current target direction
        if (bossTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, bossTarget.position);
        }
    }

    private struct QLearningMemory
    {
        public string state;
        public int action;
        public float reward;
        public string nextState;

        public QLearningMemory(string state, int action, float reward, string nextState)
        {
            this.state = state;
            this.action = action;
            this.reward = reward;
            this.nextState = nextState;
        }
    }
}