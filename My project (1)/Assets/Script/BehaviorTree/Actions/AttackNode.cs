using UnityEngine;
using System.Collections;

namespace BehaviorTree.Actions
{
    public class AttackNode : Node
    {
        private BossAI boss;
        private System.Func<Transform> getTarget;
        private float attackCooldown = 2f;
        private float lastAttackTime;

        public AttackNode(BossAI boss, System.Func<Transform> getTarget)
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

            if (Time.time - lastAttackTime < attackCooldown)
            {
                state = NodeState.FAILURE;
                return state;
            }

            boss.transform.LookAt(new Vector3(target.position.x, boss.transform.position.y, target.position.z));
            boss.StartCoroutine(boss.ExecuteComboAttack(target));
            lastAttackTime = Time.time;

            state = NodeState.SUCCESS;
            return state;
        }
    }
}