using UnityEngine;
using System.Collections.Generic;

namespace ReinforcementLearning
{
    public class TacticalDecisionFusion
    {
        private ThreatMeter threatMeter;
        private RLRewardEstimator rewardEstimator;

        public TacticalDecisionFusion(ThreatMeter threatMeter, RLRewardEstimator rewardEstimator)
        {
            this.threatMeter = threatMeter;
            this.rewardEstimator = rewardEstimator;
        }

        public Transform GetBestTarget(List<Transform> players, float threatWeight, float rewardWeight)
        {
            if (players == null || players.Count == 0)
                return null;

            Transform bestTarget = null;
            float bestScore = float.MinValue;

            foreach (Transform player in players)
            {
                if (player == null) continue;

                float threatScore = threatMeter.GetThreat(player) * threatWeight;
                float rewardScore = rewardEstimator.EstimateReward(player) * rewardWeight;
                float totalScore = threatScore + rewardScore;

                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    bestTarget = player;
                }
            }

            return bestTarget;
        }
    }
}