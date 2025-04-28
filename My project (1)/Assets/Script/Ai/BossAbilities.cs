using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class BossAbilities : MonoBehaviour
{
    [Header("Boss Ability Settings")]
    public BossStats stats;
    public Renderer bossRenderer;

    [Header("Colors")]
    public Color tidalWaveColor = Color.blue;
    public Color teleportColor = Color.green;
    public Color chargeColor = Color.red;

    [Header("Cooldowns")]
    public float tidalWaveCooldown = 25f;
    public float teleportCooldown = 15f;
    public float chargeCooldown = 40f;

    private float lastTidalWaveTime = Mathf.NegativeInfinity;
    private float lastTeleportTime = Mathf.NegativeInfinity;
    private float lastChargeTime = Mathf.NegativeInfinity;

    private Color originalColor;
    private BossAI boss;
    void Start()
    {
        if (bossRenderer == null)
            bossRenderer = GetComponentInChildren<Renderer>();

        if (bossRenderer != null)
            originalColor = bossRenderer.material.color;
        boss = GetComponent<BossAI>();
    }

    void Update()
    {
        if (CanUseAbility())
            DecideAbility();
    }

    bool CanUseAbility()
    {
        // Check if any ability is off cooldown
        return (Time.time >= lastTidalWaveTime + tidalWaveCooldown ||
                Time.time >= lastTeleportTime + teleportCooldown ||
                Time.time >= lastChargeTime + chargeCooldown);
    }

    void DecideAbility()
    {
        float distanceToClosestPlayer = FindClosestPlayerDistance();

        if (distanceToClosestPlayer <= 10f && Time.time >= lastTidalWaveTime + tidalWaveCooldown)
        {
            StartCoroutine(ActivateTidalWave());
            lastTidalWaveTime = Time.time;
        }
        else if (Time.time >= lastTeleportTime + teleportCooldown)
        {
            ActivateTeleport();
            lastTeleportTime = Time.time;
        }
        else if (Time.time >= lastChargeTime + chargeCooldown)
        {
            StartCoroutine(ActivateChargeAttack());
            lastChargeTime = Time.time;
        }
    }

    float FindClosestPlayerDistance()
    {
        float closest = Mathf.Infinity;
        var players = FindObjectsOfType<PlayerAI>();
        foreach (var player in players)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < closest)
                closest = dist;
        }
        return closest;
    }

    IEnumerator ActivateTidalWave()
    {
        boss.FreezeMovement();
        Debug.Log("Boss: Activating Tidal Wave");
        ChangeColor(tidalWaveColor);

        // Simulate 5s attack period
        yield return new WaitForSeconds(5f);
        boss.ResumeMovement();
        ResetColor();
    }

    void ActivateTeleport()
    {
        Debug.Log("Boss: Teleporting Randomly");
        ChangeColor(teleportColor);

        Vector3 randomPos = Random.insideUnitSphere * 15f;
        randomPos.y = 0;
        transform.position += randomPos;

        stats.currentHP = Mathf.Min(stats.maxHP, stats.currentHP + 200f);
        Debug.Log("Boss healed 200HP after teleport");

        StartCoroutine(ResetColorDelayed(1f));
    }

    IEnumerator ActivateChargeAttack()
    {
        boss.FreezeMovement();
        Debug.Log("Boss: Charging Attack");
        ChangeColor(chargeColor);

        stats.chargeMultiplier = 3f; // Boost next attack

        yield return new WaitForSeconds(10f);

        stats.chargeMultiplier = 1f; // Reset
        boss.ResumeMovement();
        ResetColor();
    }

    void ChangeColor(Color newColor)
    {
        if (bossRenderer != null)
            bossRenderer.material.color = newColor;
    }

    void ResetColor()
    {
        if (bossRenderer != null)
            bossRenderer.material.color = originalColor;
    }

    IEnumerator ResetColorDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetColor();
    }
}
