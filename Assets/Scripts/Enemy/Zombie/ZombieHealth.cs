using UnityEngine;

public class ZombieHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 50f;
    private float currentHealth;

    public bool IsAlive => currentHealth > 0f;
    public Transform GetTransform() => transform;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage, GameObject attacker)
    {
        if (!IsAlive) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        Debug.Log($"{gameObject.name} took {damage} damage from {attacker.name}. Health: {currentHealth}/{maxHealth}");

        if (TryGetComponent<FeedbackHandler>(out var handler))
        {
            handler.Play();
        }

        if (!IsAlive)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} died!");
        ZombieSpawner.Instance.DespawnZombie(GetComponent<ZombieMover>());
    }
}