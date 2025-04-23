using System.Collections.Generic;
using UnityEngine;

namespace ReinforcementLearning
{
    public class QTableManager
    {
        private Dictionary<Transform, float> qValues = new Dictionary<Transform, float>();

        public void UpdateQValue(Transform player, float newReward)
        {
            if (!qValues.ContainsKey(player))
                qValues[player] = 0f;

            float current = qValues[player];
            qValues[player] = Mathf.Lerp(current, newReward, 0.1f); 
        }

        public float GetQValue(Transform player)
        {
            return qValues.ContainsKey(player) ? qValues[player] : 0f;
        }

        public void Reset() => qValues.Clear();
    }
}
