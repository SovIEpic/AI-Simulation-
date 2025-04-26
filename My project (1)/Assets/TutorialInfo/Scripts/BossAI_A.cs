using UnityEngine;

public class BossAI_A : MonoBehaviour
{
    [Header("Combat Settings")]
    public float attackRange = 3f;
    public float attackCooldown = 2f;
    public float specialAttackCooldown = 10f;
    public float detectionRange = 15f;
    public string aiTag = "AI"; 

    [Header("References")]
    [SerializeField] private CharacterStats stats;
    [SerializeField] private Movement movement;

    private Transform currentTarget;
    private float lastNormalAttackTime;
    private float lastSpecialAttackTime;
    private bool isEnraged;

    protected virtual void Start()
    {
        stats = GetComponent<CharacterStats>();
        movement = GetComponent<Movement>();

        if (!GameObject.FindGameObjectWithTag(aiTag))
            Debug.LogError($"No objects with tag '{aiTag}' found!", this);
    }

    protected virtual void Update()
    {
        if (stats.currentHealth <= 0) return;


        if (currentTarget != null && currentTarget.GetComponent<CharacterStats>().currentHealth <= 0)
        {
            currentTarget = null;
        }

        FindTarget();

        if (currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.position);

            if (distance <= attackRange)
            {
                if (Time.time > lastNormalAttackTime + attackCooldown)
                {
                    Attack();
                }

                if (ShouldUseSpecialAttack())
                {
                    SpecialAttack();
                }
            }
            else if (distance <= detectionRange)
            {
                Chase();
            }
        }
    }

    void FindTarget()
    {

        if (currentTarget == null)
        {
            GameObject[] aiCharacters = GameObject.FindGameObjectsWithTag(aiTag);
            float closestDistance = Mathf.Infinity;

            foreach (var ai in aiCharacters)
            {
                if (ai == null) continue;

                CharacterStats aiStats = ai.GetComponent<CharacterStats>();
                if (aiStats != null && aiStats.currentHealth > 0)
                {
                    float dist = Vector3.Distance(transform.position, ai.transform.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        currentTarget = ai.transform;
                    }
                }
            }
        }
    }

    void Chase()
    {
        Vector3 direction = (currentTarget.position - transform.position).normalized;
        movement.Move(direction);
    }

    void Attack()
    {
        CharacterStats targetStats = currentTarget.GetComponent<CharacterStats>();
        if (targetStats.currentHealth > 0)
        {
            stats.Attack(targetStats);
            lastNormalAttackTime = Time.time;

            if (!isEnraged && stats.currentHealth <= stats.maxHealth * 0.5f)
            {
                isEnraged = true;
                stats.damage *= 2f;
                Debug.Log("<color=red>BOSS ENRAGED!</color>");
            }
        }
        else
        {
            currentTarget = null;
        }
    }

    void SpecialAttack()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 5f);
        foreach (var hit in hits)
        {
            if (hit != null && hit.CompareTag(aiTag))
            {
                CharacterStats hitStats = hit.GetComponent<CharacterStats>();
                if (hitStats != null && hitStats.currentHealth > 0)
                {
                    hitStats.TakeDamage(stats.damage * 1.5f);
                }
            }
        }
        lastSpecialAttackTime = Time.time;
    }

    bool ShouldUseSpecialAttack()
    {
        return isEnraged &&
               Time.time > lastSpecialAttackTime + specialAttackCooldown &&
               Random.Range(0, 100) < 30;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
    public void SetCurrentTarget(Transform newTarget)
    {
        currentTarget = newTarget;
    }
}