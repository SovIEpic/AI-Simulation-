using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    // Basic Stats
    public float maxHealth = 100f;
    public float currentHealth;
    public float damage = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    private float lastAttackTime;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public virtual void TakeDamage(float damageAmount)
    {
        var tankController = GetComponent<AITankController>();
        if (tankController != null && tankController.IsBlocking())
        {
            damageAmount *= 0.5f;
            Debug.Log("Block reduced damage to: " + damageAmount);
        }

        currentHealth -= damageAmount;
        Debug.Log(gameObject.name + " took " + damageAmount + " damage!");

        if (currentHealth <= 0)
        {
            Die();
        }
    }


    public void Attack(CharacterStats target)
    {
        if (target == null)
        {
            Debug.LogWarning("Attack called with null target!");
            return;
        }

        if (Time.time > lastAttackTime + attackCooldown)
        {
            if (Vector3.Distance(transform.position, target.transform.position) <= attackRange)
            {
                target.TakeDamage(damage);
                Debug.Log($"{gameObject.name} attacked {target.gameObject.name} for {damage} damage");
                lastAttackTime = Time.time;
            }
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " died!");
        Destroy(gameObject);
    }
}