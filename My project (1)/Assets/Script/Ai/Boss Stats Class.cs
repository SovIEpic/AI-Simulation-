using UnityEngine;

public class BossStats : MonoBehaviour
{
    public float maxHP = 1000f;
    public float currentHP;
    public float movementSpeed = 3.5f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    public float damagePerHit = 25f;
    public float stamina = 100f;
    public float staminaRegenRate = 10f;
    public float damagePerSecond = 30f;
    [Header("Charge Attack")]
    public float chargeMultiplier = 1f; // Default is normal damage (x1)

    public void ResetHP()
    {
        currentHP = maxHP;
    }

    public void PerformAttack(Transform target)
    {
        if (target == null) return;

        // Ensure the target has CharacterStats
        CharacterStats targetStats = target.GetComponent<CharacterStats>();
        if (targetStats != null)
        {
            targetStats.TakeDamage(damagePerHit);
        }
    }

}