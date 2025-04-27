using NUnit.Framework;
using UnityEngine;

public class PlayerAI : MonoBehaviour, IUnitHealth
{
    public float maxHP = 200f;
    public float currentHP;
    public PlayerAI player;

    void Start() => currentHP = maxHP;
    
    public void TakeDamage(float amount)
    {
        currentHP -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage.");
        if (currentHP <= 0f) {
            Die();
        }
    }
    public float GetCurrentHP()
    {
        return currentHP;
    }
    public float GetMaxHP()
    {
        return maxHP;
    }
    public void Die()
    {
        gameObject.SetActive(false);
    }
}
