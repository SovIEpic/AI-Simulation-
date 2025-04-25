using UnityEngine;

namespace BehaviorTree.Actions
{
    public class MoveToTargetNode : ActionNode
    {
        private BossAI boss;
        private System.Func<Transform> getTarget;

        public MoveToTargetNode(BossAI boss, System.Func<Transform> getTarget)
        {
            this.boss = boss;
            this.getTarget = getTarget;
        }

        public override NodeState Evaluate()
        {
            var target = getTarget();
            if (target == null || !target.gameObject.activeInHierarchy)
                return NodeState.Failure;


            float distance = Vector3.Distance(boss.transform.position, target.position);
            if (distance <= boss.stats.attackRange)
            {
                boss.GetAgent().ResetPath();
                return NodeState.Success;
            }

            boss.GetAgent().SetDestination(target.position);
            return NodeState.Running;
        }
    }
}
