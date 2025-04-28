using UnityEngine;

public interface IUnitHealth
{
    float GetCurrentHP();
    float GetMaxHP();
    void TakeDamage(float amount);
    void Die();
}

