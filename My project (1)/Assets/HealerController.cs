using UnityEngine;
using System.Collections;

public class HealerController : MonoBehaviour
{
    [Header("Healer Attributes")]
    [SerializeField] private float health;
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private bool isDead = false;

    [Header("Healer Abilities")]
    [SerializeField] private float healAmount = 25f;
    [SerializeField] private float damage = 5f;
    [SerializeField] private float siphonCooldown = 10f;
    [SerializeField] private float lastSiphonTime = 0f;
    [SerializeField] private bool canResus = true;
    [SerializeField] private bool canSiphon = true;
    private bool isSiphoning = false;

    void Start()
    {
        health = maxHealth;
    }

    void Update()
    {
        if(isDead) return;
        if(health == 0f) Die();

        // if player is dead: resus that player

        // No logic to activate the siphon yet
        if(Time.time - lastSiphonTime >= siphonCooldown) canSiphon = true;
        
        if(health < maxHealth*0.05f && !isSiphoning) LastStand();

    }

    private void Resus()
    {
        // implement player fetch logic and revival
        canResus = false;
    }

    private void LastStand()
    {
        damage *= 3.0f;
        // once  healer hits boss:
        Die();
    }

    private void Siphon()
    {
        if(isSiphoning)
        {
            float siphonDamage = damage * 0.5f;
            // if boss is attacked:
            // boss takes siphonDamage
            // healer health += 3
        }
    }

    private void ActivateSiphon()
    {
        if(!isSiphoning && Time.time - lastSiphonTime >= siphonCooldown && canSiphon)
        {
            canSiphon = false;
            lastSiphonTime = Time.time;
            StartCoroutine(SiphonRoutine());
        }
    }

    private IEnumerator SiphonRoutine()
    {
        isSiphoning = true;
        yield return new WaitForSeconds(10f);
        isSiphoning = false;
    }

    private void Die()
    {
        // kills healer
        isDead = true;
    }
}
