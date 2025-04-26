using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class AIHealerController : AIController
{
    [Header("Healer Abilities")]
    [SerializeField] protected float healerSpeed = 5f;
    [SerializeField] protected float siphonCooldown = 20f;
    [SerializeField] protected float siphonDuration = 10f;
    [SerializeField] protected float lastStandThreshold = 0.05f;
    [SerializeField] protected float lastStandMultiplier = 3f;

    [Header("Visuals")]
    [SerializeField] private Color resusColor = Color.green;
    [SerializeField] private Color siphonColor = Color.magenta;
    [SerializeField] private Color lastStandColor = new Color(1f, 0.5f, 0f); //orange

    protected float lastResusTime = Mathf.NegativeInfinity;
    protected float lastSiphonTime = Mathf.NegativeInfinity;
    private Material originalMaterial;
    protected bool siphonActive = false;
    protected virtual void OnDrawGizmosSelected() {}

    [Header("Debug")]
    [SerializeField] private bool showAbilityLogs = true;

    protected override void Start()
    {
        base.Start();
        originalMaterial = GetComponent<Renderer>().material;
    }

    protected override void Update()
    {
        base.Update();

        if(bossTarget == null)
        {
            bossTarget = GameObject.FindGameObjectWithTag("Boss")?.transform;
            if (bossTarget == null) return;
        }

        Vector3 bossPosition = new Vector3(
            bossTarget.position.x,
            transform.position.y,
            bossTarget.position.z
        );

        float distance = Vector3.Distance(transform.position, bossPosition);

        // Debug siphon cooldown
        if(showAbilityLogs)
        {
            Debug.Log($"Siphon Cooldown: {Mathf.Max(0, lastSiphonTime + siphonCooldown - Time.time):F1}s");
        }

        // Attempt abilities
        if(stats.currentHealth <= stats.maxHealth * lastStandThreshold)
        {
            TriggerLastStand();
        }
        else if (HasDeadTeammate())
        {
            TryResus();
        }
        else if(!siphonActive && Time.time >= lastSiphonTime + siphonCooldown)
        {
            TrySiphon();
        }
        else if(distance <= attackRange)
        {
            if(Time.time >= lastAttackTime + attackCooldown)
            {
                TryAttack();
            }
        }
        else if(distance <= detectionRange)
        {
            Vector3 direction = (bossPosition - transform.position).normalized;
            movement.Move(direction * chaseSpeed);
        }
        else
        {
            movement.Move(Vector3.zero);
        }
    }

    protected void TryResus()
    {
        CharacterStats[] allCharacters = Resources.FindObjectsOfTypeAll<CharacterStats>();
        foreach(var teammate in allCharacters)
        {
            if(teammate.CompareTag("AI") && teammate != this.stats && teammate.currentHealth <= 0)
            {
                Debug.Log("[Healer DEBUG] Attempting to revive " + teammate.name);
                lastResusTime = Time.time;
                teammate.Revive();
                ApplyColor(resusColor);
                if(this is QLearningHealerController q) q.SetResusUsed();
                if (showAbilityLogs) Debug.Log("Resus ACTIVATED");
                Invoke(nameof(ResetColor), 1f);
                break;
            }
        }
    }

    protected bool HasDeadTeammate()
    {
        CharacterStats[] allCharacters = Resources.FindObjectsOfTypeAll<CharacterStats>();
        foreach(var teammate in allCharacters)
        {
            if(teammate.CompareTag("AI") && teammate != this.stats && teammate.currentHealth <= 0)
            {
                return true;
            }
        }
        return false;
    }

    // Check this - not sure if OnDealDamage exists
    protected void TrySiphon()
    {
        siphonActive = true;
        lastSiphonTime = Time.time;
        ApplyColor(siphonColor);
        stats.OnDealDamage += HandleSiphonHeal;
        if(showAbilityLogs) Debug.Log("Siphon ACTIVATED");
        Invoke(nameof(StopSiphon), siphonDuration);
    }

    protected void StopSiphon()
    {
        siphonActive = false;
        stats.OnDealDamage -= HandleSiphonHeal;
        ResetColor();
    }

    //Check this for names of damage variables
    protected void HandleSiphonHeal(GameObject target, ref float damage)
    {
        if(!siphonActive || target.tag != "Boss") return;
        float originalDamage = damage;
        damage *= 0.5f;
        stats.Heal(originalDamage * 0.5f);
    }

    protected void TriggerLastStand()
    {
        ApplyColor(lastStandColor);
        if(showAbilityLogs) Debug.Log("Last Stand Used");
        var bossStats = bossTarget.GetComponent<CharacterStats>();
        if(bossStats != null)
        {
            float damage = stats.damage * lastStandMultiplier;
            bossStats.TakeDamage(damage);
        }
        stats.Die();
    }

    protected void ApplyColor(Color color)
    {
        Renderer renderer = GetComponent<Renderer>();
        if(renderer != null)
        {
            renderer.material.color = color;
        }
    }

    protected void ResetColor()
    {
        if (GetComponent<Renderer>() != null)
            GetComponent<Renderer>().material.color = originalMaterial.color;
    }
}