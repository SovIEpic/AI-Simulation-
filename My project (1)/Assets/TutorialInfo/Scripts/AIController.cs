using UnityEngine;

public class AIController : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] protected float chaseSpeed = 3.5f; // Renamed to chaseSpeed for clarity
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected float detectionRange = 10f;
    [SerializeField] protected float attackCooldown = 2f;

    [Header("References")]
    [SerializeField] protected Transform bossTarget;
    [SerializeField] protected CharacterStats stats;
    [SerializeField] protected Movement movement;
    [SerializeField] protected Rigidbody rb;

    protected float lastAttackTime;
    protected float groundCheckDistance = 0.5f; // Made protected

    protected virtual void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    protected virtual void Update()
    {
        if (bossTarget == null || movement == null) return;

        Vector3 bossPosition = new Vector3(
            bossTarget.position.x,
            transform.position.y,
            bossTarget.position.z
        );

        float distance = Vector3.Distance(transform.position, bossPosition);

        if (distance <= attackRange)
        {
            if (stats != null)
                TryAttack(); // Changed to TryAttack for consistency
            movement.Move(Vector3.zero);
        }
        else if (distance <= detectionRange)
        {
            Vector3 direction = (bossPosition - transform.position).normalized;
            movement.Move(direction * chaseSpeed);
        }
        else
        {
            movement.Move(Vector3.zero);
        }
    }

    protected void TryAttack()
    {
        if (Time.time > lastAttackTime + attackCooldown)
        {
            // Add robust null checking
            if (bossTarget == null) return;

            var targetStats = bossTarget.GetComponent<CharacterStats>();
            if (targetStats == null)
            {
                Debug.LogWarning($"Target {bossTarget.name} missing CharacterStats!");
                return;
            }

            // Verify distance again (in case target moved)
            if (Vector3.Distance(transform.position, bossTarget.position) <= attackRange)
            {
                lastAttackTime = Time.time;
                targetStats.TakeDamage(stats.damage); // Use the attacker's damage value
                Debug.Log($"{name} attacked {bossTarget.name} for {stats.damage} damage");
            }
        }
    }

    protected bool IsGrounded()
    {
        float rayLength = groundCheckDistance + 0.2f; // Extra margin
        bool hit = Physics.Raycast(
            transform.position + Vector3.up * 0.1f, // Start slightly above feet
            Vector3.down,
            rayLength,
            LayerMask.GetMask("Ground") // REQUIRED: Must assign "Ground" layer
        );

        // DEBUG: Visualize the ray in Scene view
        Debug.DrawRay(transform.position + Vector3.up * 0.1f,
                     Vector3.down * rayLength,
                     hit ? Color.green : Color.red);

        return hit;
    }
}