using UnityEngine;

public class PlayerAI : MonoBehaviour
{
    public float maxHP = 200f;
    public float currentHP;

    void Start() => currentHP = maxHP;

    public void TakeDamage(float amount)
    {
        currentHP -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage.");
        if (currentHP <= 0f) Die();
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} died.");
        Destroy(gameObject);
    }
}
