using UnityEngine;  // Added this using directive
using UnityEngine.AI;  // Added for NavMesh
namespace BehaviorTree.Actions
{
    public class RetreatNode : Node
    {
        private BossAI boss;
        private float retreatDistance = 10f;
        private float retreatCooldown = 10f;
        private float lastRetreatTime;

        public RetreatNode(BossAI boss)
        {
            this.boss = boss;
        }

        public override NodeState Evaluate()
        {
            // Check cooldown
            if (Time.time - lastRetreatTime < retreatCooldown)
            {
                state = NodeState.FAILURE;
                return state;
            }

            // Only retreat when health is low
            if (boss.stats.currentHP / boss.stats.maxHP > 0.3f)
            {
                state = NodeState.FAILURE;
                return state;
            }

            // Find retreat position
            Vector3 retreatDirection = -boss.transform.forward;
            Vector3 retreatPosition = boss.transform.position + retreatDirection * retreatDistance;

            // Sample position on NavMesh
            if (NavMesh.SamplePosition(retreatPosition, out NavMeshHit hit, retreatDistance, NavMesh.AllAreas))
            {
                boss.GetAgent().SetDestination(hit.position);
                lastRetreatTime = Time.time;
                Debug.Log("Boss is retreating!");
                state = NodeState.SUCCESS;
                return state;
            }

            state = NodeState.FAILURE;
            return state;
        }
    }
}