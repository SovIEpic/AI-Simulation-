using UnityEngine;

public class SelectableUnit : MonoBehaviour
{
    public string unitName = "Enemy";
    public float maxHP = 100f;
    public float currentHP = 100f;

    public void TakeDamage(float amount)
    {
        currentHP -= amount;
        if (currentHP <= 0f) Destroy(gameObject);
    }

    public float GetHP() => currentHP;
    public float GetMaxHP() => maxHP;
}
