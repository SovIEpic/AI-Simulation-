using UnityEngine;
using System.Linq;

namespace BehaviorTree.Actions
{
    public class AoeAttackNode : ActionNode
    {
        private BossAI boss;
        private float radius;
        private float damage;

        public AoeAttackNode(BossAI boss, float radius, float damage)
        {
            this.boss = boss;
            this.radius = radius;
            this.damage = damage;
        }

        public override NodeState Evaluate()
        {
            var targetsInRange = boss.GetAllPlayers()
                .Where(p => p != null && p.gameObject.activeInHierarchy &&
                            Vector3.Distance(boss.transform.position, p.position) <= radius)
                .ToList();

            if (targetsInRange.Count < 2 || boss.stats.stamina < 20f)
            {
                return NodeState.Failure;
            }

            foreach (var p in targetsInRange)
            {
                var playerAI = p.GetComponent<PlayerAI>();
                if (playerAI != null)
                {
                    playerAI.TakeDamage(damage);
                    boss.stats.stamina -= 10f;
                }
            }

            return NodeState.Success;
        }
    }
}
