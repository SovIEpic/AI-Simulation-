using UnityEngine;  // Added this using directive
using UnityEngine.AI;  // Added for NavMeshAgent
namespace BehaviorTree.Actions
{
    public class MoveToTargetNode : Node
    {
        private BossAI boss;
        private System.Func<Transform> getTarget;
        private float stoppingDistance = 1.5f;

        public MoveToTargetNode(BossAI boss, System.Func<Transform> getTarget)
        {
            this.boss = boss;
            this.getTarget = getTarget;
        }

        public override NodeState Evaluate()
        {
            Transform target = getTarget();
            if (target == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            NavMeshAgent agent = boss.GetAgent();
            if (agent == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            // Check if we've reached the target
            float distance = Vector3.Distance(boss.transform.position, target.position);
            if (distance <= stoppingDistance)
            {
                state = NodeState.SUCCESS;
                return state;
            }

            // Move toward target
            agent.SetDestination(target.position);
            state = NodeState.RUNNING;
            return state;
        }
    }
}