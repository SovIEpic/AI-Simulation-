using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ReinforcementLearning
{
    [System.Serializable]
    public class QEntry
    {
        public string unitName;
        public float value;
    }

    [System.Serializable]
    public class QSaveData
    {
        public List<QEntry> entries = new List<QEntry>();
    }

    public class QTableManager
    {
        private Dictionary<string, float> qTable = new Dictionary<string, float>();
        private string filePath => Application.persistentDataPath + "/qtable.json";

        public void UpdateQValue(Transform player, float newReward)
        {
            string id = player.name;
            if (!qTable.ContainsKey(id)) qTable[id] = 0f;
            qTable[id] = Mathf.Lerp(qTable[id], newReward, 0.1f);
        }

        public float GetQValue(Transform player)
        {
            string id = player.name;
            return qTable.ContainsKey(id) ? qTable[id] : 0f;
        }

        public void SaveToDisk()
        {
            QSaveData data = new QSaveData();
            foreach (var kvp in qTable)
            {
                data.entries.Add(new QEntry { unitName = kvp.Key, value = kvp.Value });
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, json);
            Debug.Log("QTable saved to " + filePath);
        }

        public void LoadFromDisk()
        {
            if (!File.Exists(filePath))
            {
                Debug.Log("No QTable file found.");
                return;
            }

            string json = File.ReadAllText(filePath);
            QSaveData data = JsonUtility.FromJson<QSaveData>(json);
            qTable.Clear();
            foreach (var entry in data.entries)
            {
                qTable[entry.unitName] = entry.value;
            }

            Debug.Log("QTable loaded from " + filePath);
        }

        public void Reset() => qTable.Clear();
    }
}
