using UnityEngine;

public class AITankController : AIController
{
    [Header("Tank Abilities")]
    [SerializeField] protected float tankSpeed = 2.5f;
    [SerializeField] protected float shieldBashRange = 3f;
    [SerializeField] protected float tauntRange = 8f;
    [SerializeField] protected float blockDuration = 3f;
    [SerializeField] protected float shieldBashCooldown = 8f;
    [SerializeField] protected float tauntCooldown = 15f;
    [SerializeField] protected float blockCooldown = 10f;
    [SerializeField] protected float tauntDuration = 5f;
    [SerializeField] protected float shieldBashDamage = 25f;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject shieldIndicator;
    [SerializeField] private Color blockColor = Color.blue;
    [SerializeField] private Color bashColor = Color.red;
    [SerializeField] private Color tauntColor = Color.yellow;
    private Material originalMaterial;

    // Cooldowns (initialized to allow immediate use)
    protected float lastTauntTime = Mathf.NegativeInfinity;
    protected float lastShieldBashTime = Mathf.NegativeInfinity;
    protected float lastBlockTime = Mathf.NegativeInfinity;
    protected bool isBlocking;

    [Header("Debug")]
    [SerializeField] private bool showAbilityLogs = true;

    protected override void Start()
    {
        base.Start();
        originalMaterial = GetComponent<Renderer>().material;

        if (shieldIndicator != null)
            shieldIndicator.SetActive(false);
    }

    protected override void Update()
    {
        if (bossTarget == null)
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

        // Debug cooldown states
        if (showAbilityLogs)
        {
            Debug.Log($"Cooldowns - " +
                     $"Taunt: {Mathf.Max(0, lastTauntTime + tauntCooldown - Time.time):F1}s | " +
                     $"Bash: {Mathf.Max(0, lastShieldBashTime + shieldBashCooldown - Time.time):F1}s | " +
                     $"Block: {Mathf.Max(0, lastBlockTime + blockCooldown - Time.time):F1}s");
        }

        // Ability priority system
        if (distance <= tauntRange && Time.time >= lastTauntTime + tauntCooldown)
        {
            TryTaunt();
        }
        else if (distance <= shieldBashRange && Time.time >= lastShieldBashTime + shieldBashCooldown)
        {
            TryShieldBash();
        }
        else if (distance <= attackRange)
        {
            if (Time.time >= lastBlockTime + blockCooldown)
                StartBlocking();
            else if (Time.time >= lastAttackTime + attackCooldown)
                TryAttack();
        }
        else if (distance <= detectionRange)
        {
            Vector3 direction = (bossPosition - transform.position).normalized;
            movement.Move(direction * tankSpeed);
        }
        else
        {
            movement.Move(Vector3.zero);
        }
    }

    protected void TryTaunt()
    {
        lastTauntTime = Time.time;
        ApplyColor(tauntColor);
        if (showAbilityLogs) Debug.Log($"Taunt ACTIVATED at {Time.time}");

        if (bossTarget.TryGetComponent<BossAI>(out var bossAI))
            bossAI.SetCurrentTarget(transform);

        Invoke(nameof(ResetColor), tauntDuration);
    }

    protected void TryShieldBash()
    {
        lastShieldBashTime = Time.time;
        ApplyColor(bashColor);
        if (showAbilityLogs) Debug.Log($"Shield Bash ACTIVATED at {Time.time}");

        Collider[] hits = Physics.OverlapSphere(transform.position, shieldBashRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Boss"))
            {
                hit.GetComponent<CharacterStats>()?.TakeDamage(shieldBashDamage);
                hit.GetComponent<Rigidbody>()?.AddForce(
                    (hit.transform.position - transform.position).normalized * 5f,
                    ForceMode.Impulse);
            }
        }
        Invoke(nameof(ResetColor), 0.5f);
    }

    protected void StartBlocking()
    {
        isBlocking = true;
        lastBlockTime = Time.time;
        ApplyColor(blockColor);
        if (showAbilityLogs) Debug.Log($"Block ACTIVATED at {Time.time}");

        if (shieldIndicator != null)
            shieldIndicator.SetActive(true);

        Invoke(nameof(StopBlocking), blockDuration);
    }

    private void StopBlocking()
    {
        isBlocking = false;
        if (shieldIndicator != null)
            shieldIndicator.SetActive(false);
        ResetColor();
    }

    public bool IsBlocking()
    {
        return isBlocking;
    }

    private void ApplyColor(Color newColor)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = newColor;
        else if (showAbilityLogs)
            Debug.LogError("Missing Renderer component!");
    }

    private void ResetColor()
    {
        if (!isBlocking && GetComponent<Renderer>() != null)
            GetComponent<Renderer>().material.color = originalMaterial.color;
    }

    protected void OnDrawGizmosSelected()
    {
        // Attack Range (Red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Shield Bash Range (Orange)
        Gizmos.color = new Color(1, 0.5f, 0);
        Gizmos.DrawWireSphere(transform.position, shieldBashRange);

        // Taunt Range (Yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, tauntRange);
    }
}