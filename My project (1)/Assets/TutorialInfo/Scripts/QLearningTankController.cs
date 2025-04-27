using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.AI;

public class QLearningTankController : AITankController
{
    [Header("QLearning Settings")]
    public float learningRate = 0.1f;
    public float discountFactor = 0.9f;
    public float explorationRate = 0.3f;
    public float explorationDecay = 0.995f;
    public int maxMemorySize = 1000;

    [Header("NavMesh Settings")]
    [SerializeField] private float navigationRewardScale = 0.3f;
    [SerializeField] private int pathPointCount = 3;

    [SerializeField] new private bool useNavMeshLearning = true;

    private Dictionary<string, Dictionary<int, float>> qTable;
    private List<QLearningMemory> memory;
    private int lastAction;
    private float lastReward;
    private string lastState;
    private float lastActionTime;
    private const float minActionInterval = 0.5f;
    private List<Vector3> currentPathPoints = new List<Vector3>();

    [System.Serializable]
    private class QTableSaveData
    {
        public string version = "1.2"; // Bumped version for new format
        public List<StateEntry> entries = new List<StateEntry>();

        [System.Serializable]
        public class StateEntry
        {
            public string state;
            public List<int> actions;
            public List<float> values;

            public StateEntry(string state, Dictionary<int, float> stateData)
            {
                this.state = state;
                actions = new List<int>();
                values = new List<float>();

                foreach (var pair in stateData)
                {
                    actions.Add(pair.Key);
                    values.Add(pair.Value);
                }
            }
        }
    }

    protected override void Start()
    {
        base.Start();
        InitializeQTable();

        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();

        if (navAgent == null)
            navAgent = gameObject.AddComponent<NavMeshAgent>();

        navAgent.agentTypeID = 0;
        navAgent.speed = tankSpeed;
        navAgent.angularSpeed = 120f;
        navAgent.acceleration = 8f;
        navAgent.stoppingDistance = attackRange * 0.8f;
        navAgent.autoBraking = false;

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

        string currentState = GetState();
        int action = ChooseAction(currentState);
        ExecuteAction(action);

        float reward = 0f;
        if (bossTarget != null)
        {
            reward = CalculateReward();
        }

        if (!string.IsNullOrEmpty(lastState) && !string.IsNullOrEmpty(currentState))
        {
            memory.Add(new QLearningMemory(lastState, lastAction, reward, currentState));
            if (memory.Count > maxMemorySize)
            {
                memory.RemoveAt(0);
            }
        }

        Learn();
        explorationRate *= explorationDecay;
        explorationRate = Mathf.Max(0.01f, explorationRate);

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

        string pathState = GetPathState();

        return $"dist_{Mathf.Round(distance)}_hp_{Mathf.Round(healthPercentage * 10)}_" +
               $"taunt_{canTaunt}_bash_{canBash}_block_{canBlock}_" +
               $"atk_{canAttack}_inAtk_{inAttackRange}_inBash_{inBashRange}_inTaunt_{inTauntRange}" +
               $"_{pathState}";
    }

    private string GetPathState()
    {
        if (!useNavMeshLearning || bossTarget == null || navAgent == null)
            return "nopath";

        UpdatePathPoints();

        if (currentPathPoints.Count == 0)
            return "nopath";

        string pathState = "p";
        for (int i = 0; i < Mathf.Min(pathPointCount, currentPathPoints.Count); i++)
        {
            if (i < currentPathPoints.Count)
            {
                Vector3 dirToWaypoint = currentPathPoints[i] - transform.position;
                dirToWaypoint.y = 0;
                float angle = Vector3.SignedAngle(transform.forward, dirToWaypoint, Vector3.up);
                int angleDir = angle > 45 ? 1 : (angle < -45 ? -1 : 0);

                float dist = dirToWaypoint.magnitude;
                int distBin = dist < 2 ? 0 : (dist < 5 ? 1 : 2);

                pathState += $"{angleDir}{distBin}";
            }
        }

        NavMeshPath directPath = new NavMeshPath();
        Vector3 bossPosition = new Vector3(bossTarget.position.x, transform.position.y, bossTarget.position.z);

        int agentTypeID = navAgent.agentTypeID;

        bool hasDirectPath = false;
        try
        {
            hasDirectPath = NavMesh.CalculatePath(
                transform.position,
                bossPosition,
                new NavMeshQueryFilter
                {
                    agentTypeID = agentTypeID,
                    areaMask = navAgent.areaMask
                },
                directPath
            );
        }
        catch
        {
            return "nopath";
        }

        bool isPathStraight = directPath.status == NavMeshPathStatus.PathComplete &&
                              directPath.corners.Length <= 2;

        pathState += isPathStraight ? "d" : "i";
        return pathState;
    }

    private void UpdatePathPoints()
    {
        if (Time.time >= lastPathUpdate + pathUpdateRate || currentPathPoints.Count == 0)
        {
            lastPathUpdate = Time.time;

            if (navAgent.hasPath && navAgent.path.corners.Length > 1)
            {
                currentPathPoints.Clear();
                foreach (Vector3 corner in navAgent.path.corners)
                {
                    currentPathPoints.Add(corner);
                }
            }
            else if (bossTarget != null)
            {
                Vector3 bossPosition = new Vector3(
                    bossTarget.position.x,
                    transform.position.y,
                    bossTarget.position.z
                );

                NavMeshPath path = new NavMeshPath();
                int agentTypeID = navAgent.agentTypeID;

                bool pathSuccess = false;
                try
                {
                    pathSuccess = NavMesh.CalculatePath(
                        transform.position,
                        bossPosition,
                        new NavMeshQueryFilter
                        {
                            agentTypeID = agentTypeID,
                            areaMask = navAgent.areaMask
                        },
                        path
                    );
                }
                catch
                {
                    return;
                }

                if (pathSuccess)
                {
                    currentPathPoints.Clear();
                    foreach (Vector3 corner in path.corners)
                    {
                        currentPathPoints.Add(corner);
                    }
                }
            }
        }
    }

    private int ChooseAction(string state)
    {
        if (Random.Range(0f, 1f) < explorationRate)
        {
            return Random.Range(0, 6);
        }

        if (!qTable.ContainsKey(state))
        {
            qTable[state] = new Dictionary<int, float>();
        }

        for (int i = 0; i < 6; i++)
        {
            if (!qTable[state].ContainsKey(i))
            {
                qTable[state][i] = 0f;
            }
        }

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
            case 0:
                if (Time.time >= lastTauntTime + tauntCooldown)
                {
                    TryTaunt();
                }
                break;
            case 1:
                if (Time.time >= lastShieldBashTime + shieldBashCooldown)
                {
                    TryShieldBash();
                }
                break;
            case 2:
                if (Time.time >= lastBlockTime + blockCooldown)
                {
                    StartBlocking();
                }
                break;
            case 3:
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    TryAttack();
                }
                break;
            case 4:
                NavigateTowardsBoss();
                break;
            case 5:
                MoveTowardBoss();
                break;
        }
    }

    private void NavigateTowardsBoss()
    {
        if (bossTarget == null || navAgent == null) return;

        Vector3 bossPosition = new Vector3(
            bossTarget.position.x,
            transform.position.y,
            bossTarget.position.z
        );

        float distance = Vector3.Distance(transform.position, bossPosition);

        if (distance <= attackRange)
        {
            navAgent.isStopped = true;
            isPathfinding = false;
            return;
        }

        if (Time.time >= lastPathUpdate + pathUpdateRate || !isPathfinding)
        {
            navAgent.SetDestination(bossPosition);
            navAgent.isStopped = false;
            isPathfinding = true;
            lastPathUpdate = Time.time;
            UpdatePathPoints();
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

        if (navAgent != null)
        {
            navAgent.isStopped = true;
            isPathfinding = false;
        }
    }

    private float CalculateReward()
    {
        float reward = 0f;

        if (bossTarget == null)
            return reward;

        float distance = Vector3.Distance(transform.position, bossTarget.position);

        float previousDistance = float.MaxValue;
        if (!string.IsNullOrEmpty(lastState) && lastState.StartsWith("dist_"))
        {
            string[] stateParts = lastState.Split('_');
            if (stateParts.Length > 1)
            {
                float.TryParse(stateParts[1], out previousDistance);
            }
        }

        CharacterStats bossStats = bossTarget.GetComponent<CharacterStats>();
        if (bossStats != null && bossStats.currentHealth <= 0)
            reward += 100f;

        if (stats != null && stats.currentHealth <= 0)
            reward -= 100f;

        if (lastAction == 4 || lastAction == 5)
        {
            float distanceDelta = previousDistance - distance;
            reward += distanceDelta * navigationRewardScale;

            if (navAgent != null && navAgent.pathStatus == NavMeshPathStatus.PathPartial)
                reward -= 1f;

            if (distance > detectionRange)
                reward -= 1f;

            if (lastAction == 4 && navAgent != null && navAgent.path.corners.Length > 2)
                reward += 0.5f;

            if (lastAction == 5 && navAgent != null &&
               (navAgent.path.corners.Length <= 2 || Vector3.Distance(transform.position, bossTarget.position) < 3f))
                reward += 0.2f;
        }

        switch (lastAction)
        {
            case 0 when Time.time - lastTauntTime < 0.1f:
                reward += 5f;
                break;
            case 1 when Time.time - lastShieldBashTime < 0.1f:
                reward += 10f;
                break;
        }

        if (stats != null && stats.currentHealth < stats.maxHealth * 0.5f)
            reward -= 2f;

        return reward;
    }

    private void Learn()
    {
        int batchSize = Mathf.Min(32, memory.Count);
        for (int i = 0; i < batchSize; i++)
        {
            int randomIndex = Random.Range(0, memory.Count);
            QLearningMemory experience = memory[randomIndex];

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

            if (!qTable.ContainsKey(experience.state))
            {
                qTable[experience.state] = new Dictionary<int, float>();
            }
            if (!qTable[experience.state].ContainsKey(experience.action))
            {
                qTable[experience.state][experience.action] = 0f;
            }

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
        string directory = Path.Combine(Application.persistentDataPath, "QLearningSaves");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        return Path.Combine(directory, "navmesh_tank_qlearning_v1.json");
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
                HandleCorruptedSave();
                return false;
            }

            QTableSaveData saveData = JsonUtility.FromJson<QTableSaveData>(json);

            // Handle version transition
            if (saveData.version == "1.0" || saveData.version == "1.1")
            {
                Debug.Log("Converting old save format to new format");
                return ConvertLegacySave(json);
            }

            if (saveData == null || saveData.entries == null)
            {
                Debug.LogWarning("Failed to parse save file - invalid data structure");
                HandleCorruptedSave();
                return false;
            }

            // Initialize fresh Q-table
            qTable = new Dictionary<string, Dictionary<int, float>>();

            // Rebuild Q-table
            foreach (var entry in saveData.entries)
            {
                if (entry.actions == null || entry.values == null ||
                    entry.actions.Count != entry.values.Count)
                {
                    Debug.LogWarning($"Skipping corrupted state entry: {entry.state}");
                    continue;
                }

                var stateActions = new Dictionary<int, float>();
                for (int i = 0; i < entry.actions.Count; i++)
                {
                    stateActions[entry.actions[i]] = entry.values[i];
                }

                qTable[entry.state] = stateActions;
            }

            Debug.Log($"Successfully loaded Q-table with {qTable.Count} states");
            return qTable.Count > 0;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Load failed: {e.Message}\n{e.StackTrace}");
            HandleCorruptedSave();
            return false;
        }
    }

    private bool ConvertLegacySave(string json)
    {
        try
        {
            // Parse old format
            var legacyData = JsonUtility.FromJson<LegacyQTableSaveData>(json);

            if (legacyData == null || legacyData.states == null ||
                legacyData.actions == null || legacyData.values == null)
            {
                Debug.LogWarning("Failed to parse legacy save file");
                return false;
            }

            // Convert to new format
            var saveData = new QTableSaveData();

            for (int i = 0; i < legacyData.states.Count; i++)
            {
                if (i >= legacyData.actions.Count || i >= legacyData.values.Count ||
                    legacyData.actions[i].Count != legacyData.values[i].Count)
                {
                    Debug.LogWarning($"Skipping corrupted legacy state: {legacyData.states[i]}");
                    continue;
                }

                var stateActions = new Dictionary<int, float>();
                for (int j = 0; j < legacyData.actions[i].Count; j++)
                {
                    stateActions[legacyData.actions[i][j]] = legacyData.values[i][j];
                }

                saveData.entries.Add(new QTableSaveData.StateEntry(legacyData.states[i], stateActions));
            }

            // Save in new format
            string newJson = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(GetSavePath(), newJson);

            // Now load the converted file
            return TryLoadQTable();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Legacy conversion failed: {e.Message}");
            return false;
        }
    }

    [System.Serializable]
    private class LegacyQTableSaveData
    {
        public string version;
        public List<string> states;
        public List<List<int>> actions;
        public List<List<float>> values;
        public int totalExperiences;
    }

    public void ValidateQTable()
    {
        if (qTable == null)
        {
            Debug.LogWarning("QTable is null");
            return;
        }

        int corruptedStates = 0;
        foreach (var state in qTable.Keys.ToList())
        {
            if (qTable[state] == null)
            {
                Debug.LogWarning($"Removing null state: {state}");
                qTable.Remove(state);
                corruptedStates++;
                continue;
            }

            // Remove any actions with NaN values
            var actionsToRemove = qTable[state]
                .Where(pair => float.IsNaN(pair.Value))
                .Select(pair => pair.Key)
                .ToList();

            foreach (var action in actionsToRemove)
            {
                Debug.LogWarning($"Removing NaN value for action {action} in state {state}");
                qTable[state].Remove(action);
            }

            if (qTable[state].Count == 0)
            {
                Debug.LogWarning($"Removing empty state: {state}");
                qTable.Remove(state);
                corruptedStates++;
            }
        }

        if (corruptedStates > 0)
        {
            Debug.Log($"Validated Q-table, removed {corruptedStates} corrupted states");
            SaveQTable(); // Auto-save after cleanup
        }
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
            // Create new save data with encapsulated state entries
            var saveData = new QTableSaveData();

            foreach (var stateEntry in qTable)
            {
                if (stateEntry.Value == null || stateEntry.Value.Count == 0)
                    continue;

                saveData.entries.Add(new QTableSaveData.StateEntry(stateEntry.Key, stateEntry.Value));
            }

            string savePath = GetSavePath();
            string tempPath = savePath + ".tmp";
            string directory = Path.GetDirectoryName(savePath);

            // Ensure directory exists
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Serialize to JSON
            string json = JsonUtility.ToJson(saveData, true);

            // Write to temporary file first
            File.WriteAllText(tempPath, json);

            // Verify the temporary file
            if (!File.Exists(tempPath) || new FileInfo(tempPath).Length == 0)
            {
                Debug.LogError("Failed to write temporary save file");
                if (File.Exists(tempPath)) File.Delete(tempPath);
                return;
            }

            // Replace existing file atomically
            if (File.Exists(savePath))
                File.Delete(savePath);

            File.Move(tempPath, savePath);

            Debug.Log($"Successfully saved Q-table with {saveData.entries.Count} states");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Save failed: {e.Message}\n{e.StackTrace}");
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
                string backupDir = Path.Combine(Path.GetDirectoryName(savePath), "Backups");
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);

                string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = Path.Combine(backupDir, $"corrupted_save_{timestamp}.json");

                File.Copy(savePath, backupPath);
                File.Delete(savePath);

                Debug.Log($"Created backup of corrupted save at: {backupPath}");
            }

            qTable = new Dictionary<string, Dictionary<int, float>>();
            Debug.Log("Initialized fresh Q-table");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to handle corrupted save: {e.Message}");
        }
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

    private void OnApplicationQuit()
    {
        SaveQTable();
    }

    private void OnDestroy()
    {
        SaveQTable();
    }

    protected new void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (Application.isPlaying && currentPathPoints.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < currentPathPoints.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPathPoints[i], currentPathPoints[i + 1]);
            }

            Gizmos.color = Color.blue;
            foreach (Vector3 point in currentPathPoints)
            {
                Gizmos.DrawSphere(point, 0.3f);
            }
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