using UnityEngine;

public class AIController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float detectionRange = 10f;

    [Header("References")]
    [SerializeField] private Transform bossTarget;
    [SerializeField] private CharacterStats stats;
    private Movement movement;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    void Start()
    {
        movement = GetComponent<Movement>();
        stats = GetComponent<CharacterStats>();

        if (bossTarget == null)
        {
            GameObject boss = GameObject.FindGameObjectWithTag("Boss");
            if (boss != null) bossTarget = boss.transform;
        }

        // Validate components
        if (movement == null) Debug.LogError("Missing Movement script!", this);
        if (stats == null) Debug.LogError("Missing CharacterStats script!", this);
        if (bossTarget == null) Debug.LogError("No boss found in scene!", this);
    }

    void Update()
    {
        if (bossTarget == null || movement == null || stats == null) return;

        float distanceToBoss = Vector3.Distance(transform.position, bossTarget.position);

        // Behavior states
        if (distanceToBoss <= attackRange)
        {
            // Attack if in range
            stats.Attack(bossTarget.GetComponent<CharacterStats>());
            movement.Move(Vector3.zero);
        }
        else if (distanceToBoss <= detectionRange)
        {
            // Chase boss if detected
            Vector3 direction = (bossTarget.position - transform.position).normalized;
            movement.Move(direction * chaseSpeed);
        }
        else
        {
            // Idle
            movement.Move(Vector3.zero);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}