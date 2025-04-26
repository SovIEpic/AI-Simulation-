using UnityEngine;
using BehaviorTree; // Add this using directive

namespace BehaviorTree.Actions
{
    public class AoeAttackNode : Node
    {
        private BossAI boss;
        private float radius;
        private float damage;
        private float cooldown = 5f;
        private float lastCastTime;

        public AoeAttackNode(BossAI boss, float radius, float damage) : base()
        {
            this.boss = boss;
            this.radius = radius;
            this.damage = damage;
        }

        public override NodeState Evaluate()
        {
            // Check cooldown
            if (Time.time - lastCastTime < cooldown)
            {
                return NodeState.FAILURE;
            }

            // Find all players in radius
            Collider[] hitPlayers = Physics.OverlapSphere(boss.transform.position, radius, LayerMask.GetMask("Player"));
            if (hitPlayers.Length < 2) // Only use AOE when 2+ players are close
            {
                return NodeState.FAILURE;
            }

            // Damage all players in radius
            foreach (Collider player in hitPlayers)
            {
                CharacterStats playerStats = player.GetComponent<CharacterStats>();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(damage);
                    Debug.Log($"AOE attack hit {player.name} for {damage} damage");
                }
            }

            // Play AOE effect
            if (boss.aoeEffectPrefab != null)
            {
                GameObject effect = GameObject.Instantiate(boss.aoeEffectPrefab, boss.transform.position, Quaternion.identity);
                GameObject.Destroy(effect, 3f);
            }

            lastCastTime = Time.time;
            return NodeState.SUCCESS;
        }
    }
}