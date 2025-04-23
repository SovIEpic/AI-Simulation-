using UnityEngine;
using UnityEngine.AI;

namespace BehaviorTree.Actions
{
    public class RetreatNode : ActionNode
    {
        private BossAI boss;
        private float retreatDistance = 5f;

        public RetreatNode(BossAI boss)
        {
            this.boss = boss;
        }

        public override NodeState Evaluate()
        {
            if (boss.stats.currentHP / boss.stats.maxHP > 0.2f &&
                boss.stats.stamina > 20f &&
                boss.GetAllPlayers().Count <= 2)
                return NodeState.Failure;

            Transform target = boss.GetCurrentTarget();
            if (target == null) return NodeState.Failure;

            Vector3 awayDir = (boss.transform.position - target.position).normalized;
            Vector3 retreatPoint = boss.transform.position + awayDir * retreatDistance;

            if (NavMesh.SamplePosition(retreatPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                boss.GetAgent().SetDestination(hit.position);
                Debug.Log("Boss is retreating!");
                return NodeState.Success;
            }

            return NodeState.Failure;
        }
    }
}
