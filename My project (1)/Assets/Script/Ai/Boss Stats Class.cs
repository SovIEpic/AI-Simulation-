using UnityEngine;

[System.Serializable]
public class BossStats
{
    public float maxHP = 1000f;
    public float currentHP;
    public float stamina = 100f;
    public float damagePerSecond = 50f;
    public float movementSpeed = 3.5f;
    public float detectionRange = 15f;
    public float attackRange = 3f;
    public float attackCooldown = 2f;
    public float staminaRegenRate = 10f;

    public void ResetHP() => currentHP = maxHP;
}
