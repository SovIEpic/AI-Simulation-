using UnityEngine;

public class AITankController : MonoBehaviour
{
    [Header("Tank Settings")]
    public float healthMultiplier = 1.5f;
    public float blockDuration = 3f;
    public float blockCooldown = 10f;
    public float shieldBashRange = 3f;
    public float shieldBashDamage = 25f;
    public float shieldBashCooldown = 8f;
    public float tauntRange = 8f;
    public float tauntCooldown = 15f;
    public float tauntDuration = 5f;

    [Header("References")]
    [SerializeField] private CharacterStats stats;
    [SerializeField] private Movement movement;
    [SerializeField] private ParticleSystem tauntVFX;

    [Header("Cube Visuals")]
    [SerializeField] private GameObject shieldIndicator; // Renamed from shieldVisual
    [SerializeField] private Color tauntColor = Color.yellow;
    [SerializeField] private Color bashColor = Color.red;
    [SerializeField] private Color blockColor = Color.blue;

    private Material originalMaterial;
    private Renderer cubeRenderer;
    private Transform bossTarget;
    private float lastBlockTime;
    private float lastShieldBashTime;
    private float lastTauntTime;
    private bool isBlocking;

    void Start()
    {

        stats = GetComponent<CharacterStats>();
        movement = GetComponent<Movement>();
        cubeRenderer = GetComponent<Renderer>();

        if (stats == null) Debug.LogError("Missing CharacterStats!", gameObject);
        if (movement == null) Debug.LogError("Missing Movement!", gameObject);
        if (cubeRenderer == null) Debug.LogError("Missing Renderer!", gameObject);

        originalMaterial = cubeRenderer.material;
        bossTarget = GameObject.FindGameObjectWithTag("Boss")?.transform;

        if (bossTarget == null) Debug.LogWarning("No boss found in scene!", gameObject);

        stats.maxHealth *= healthMultiplier;
        stats.currentHealth = stats.maxHealth;

        if (shieldIndicator != null)
            shieldIndicator.SetActive(false);
        else
            Debug.LogWarning("Shield indicator not assigned", gameObject);

        stats = GetComponent<CharacterStats>();
        movement = GetComponent<Movement>();
        bossTarget = GameObject.FindGameObjectWithTag("Boss").transform;

        cubeRenderer = GetComponent<Renderer>();
        originalMaterial = cubeRenderer.material;

        stats.maxHealth *= healthMultiplier;
        stats.currentHealth = stats.maxHealth;

        if (shieldIndicator != null)
            shieldIndicator.SetActive(false);
    }

    void Update()
    {
        if (bossTarget == null) return;

        float distanceToBoss = Vector3.Distance(transform.position, bossTarget.position);

        if (distanceToBoss <= tauntRange && Time.time > lastTauntTime + tauntCooldown)
        {
            TryTaunt();
        }

        if (distanceToBoss <= shieldBashRange && Time.time > lastShieldBashTime + shieldBashCooldown)
        {
            TryShieldBash();
        }

        if (distanceToBoss <= stats.attackRange && !isBlocking && Time.time > lastBlockTime + blockCooldown)
        {
            StartBlocking();
        }
        else if (isBlocking && Time.time > lastBlockTime + blockDuration)
        {
            StopBlocking();
        }
    }

    void TryTaunt()
    {
        lastTauntTime = Time.time;
        bossTarget.GetComponent<BossAI>().SetCurrentTarget(transform);
        cubeRenderer.material.color = tauntColor;
        if (tauntVFX != null) tauntVFX.Play();
        Invoke(nameof(ResetColor), tauntDuration);
    }

    void StartBlocking()
    {
        isBlocking = true;
        lastBlockTime = Time.time;
        cubeRenderer.material.color = blockColor;
        if (shieldIndicator != null) shieldIndicator.SetActive(true);
        movement.SetSpeedMultiplier(0.5f);
    }

    void StopBlocking()
    {
        isBlocking = false;
        if (shieldIndicator != null) shieldIndicator.SetActive(false);
        movement.ResetSpeed();
        ResetColor();
    }

    void TryShieldBash()
    {
        lastShieldBashTime = Time.time;

        Collider[] hits = Physics.OverlapSphere(transform.position, shieldBashRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Boss"))
            {
                hit.GetComponent<CharacterStats>()?.TakeDamage(shieldBashDamage);

                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 direction = (hit.transform.position - transform.position).normalized;
                    rb.AddForce(direction * 5f, ForceMode.Impulse);
                }
            }
        }
        cubeRenderer.material.color = bashColor;
        Invoke(nameof(ResetColor), 0.5f);
    }

    void ResetColor()
    {
        if (!isBlocking) // Don't reset color if still blocking
        {
            cubeRenderer.material.color = originalMaterial.color;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, tauntRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shieldBashRange);
    }

    public void TakeDamage(float damage)
    {
        if (isBlocking)
        {
            damage *= 0.3f; // 70% damage reduction
        }
        stats.TakeDamage(damage);
    }
}