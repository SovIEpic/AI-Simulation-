using System.Collections.Generic;
using UnityEngine;
using ReinforcementLearning;

namespace Utilities
{
    public class TacticalDecisionFusion
    {
        private ThreatMeter threatMeter;
        private RLRewardEstimator rl;

        public TacticalDecisionFusion(ThreatMeter meter, RLRewardEstimator rlEstimator)
        {
            threatMeter = meter;
            rl = rlEstimator;
        }

        public Transform GetBestTarget(List<Transform> players, float alpha, float beta)
        {
            Transform bestTarget = null;
            float bestScore = float.MinValue;

            foreach (var player in players)
            {
                float threat = threatMeter.GetThreatValue(player);
                float reward = rl.EstimateReward(player);
                float score = alpha * threat + beta * reward;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = player;
                }
            }

            return bestTarget;
        }
    }
}
