using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    public bool IsAlive => currentHealth > 0f;
    public Transform GetTransform() => transform;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage, GameObject attacker)
    {
        if (!IsAlive) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        Debug.Log($"Player took {damage} damage from {attacker.name}. Health: {currentHealth}/{maxHealth}");

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
        Debug.Log("Player died!");
        // Implement death logic, e.g., game over, respawn, etc.
    }
}