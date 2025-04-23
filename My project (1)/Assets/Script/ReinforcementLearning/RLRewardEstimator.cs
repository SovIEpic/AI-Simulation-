using UnityEngine;

namespace ReinforcementLearning
{
    public class RLRewardEstimator
    {
        private QTableManager qTable;

        public RLRewardEstimator(QTableManager manager)
        {
            qTable = manager;
        }

        public float EstimateReward(Transform player)
        {
            return qTable.GetQValue(player);
        }

        public void LearnFromOutcome(Transform player, float reward)
        {
            qTable.UpdateQValue(player, reward);
            qTable.SaveToDisk();
        }
    }
}
