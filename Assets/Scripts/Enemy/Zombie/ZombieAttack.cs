using UnityEngine;

[RequireComponent(typeof(ZombieMover))]
public class ZombieAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackRange = 1f;

    private ZombieMover zombieMover;
    private float attackTimer;
    private Transform target;

    public bool IsAttacking => attackTimer > 0f;

    private void Awake()
    {
        zombieMover = GetComponent<ZombieMover>();
        attackTimer = 0f;
    }

    private void Update()
    {
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }

        target = zombieMover.GetCurrentTarget();
        if (target == null) return;

        if (CanAttack())
        {
            Attack();
        }
    }

    private bool CanAttack()
    {
        if (attackTimer > 0) return false;

        float distance = Vector3.Distance(transform.position, target.position);
        return distance <= attackRange;
    }

    private void Attack()
    {
        IDamageable targetDamageable = target.GetComponent<IDamageable>();
        if (targetDamageable != null && targetDamageable.IsAlive)
        {
            targetDamageable.TakeDamage(attackDamage, gameObject);
            attackTimer = attackCooldown;
            Debug.Log($"{gameObject.name} attacked {target.gameObject.name} for {attackDamage} damage!");
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}