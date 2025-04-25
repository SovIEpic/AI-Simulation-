using UnityEngine;

namespace BehaviorTree.Actions
{
    public class AttackNode : ActionNode
    {
        private BossAI boss;
        private System.Func<Transform> getTarget;

        public AttackNode(BossAI boss, System.Func<Transform> getTarget)
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
            if (distance > boss.stats.attackRange) return NodeState.Failure;

            if (boss.stats.stamina < 10f) return NodeState.Failure;

            target.GetComponent<PlayerAI>()?.TakeDamage(boss.stats.damagePerSecond);
            boss.stats.stamina -= 10f;
            return NodeState.Success;
        }
    }
}
