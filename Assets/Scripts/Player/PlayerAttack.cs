using UnityEngine;
using System.Collections.Generic;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float maxDamage = 50f;
    [SerializeField] private float chargeTime = 2f; // Time to reach max damage
    [SerializeField] private float attackRange = 5f; // Distance
    [SerializeField] private float attackAngle = 90f; // Cone angle in degrees

    private bool isCharging;
    private float chargeTimer;
    private float currentDamage;
    private Camera mainCamera;

    public bool IsCharging => isCharging;
    public float CurrentDamage => currentDamage;
    public float ChargeProgress => chargeTime > 0 ? chargeTimer / chargeTime : 0f;

    private void Awake()
    {
        mainCamera = Camera.main;

        // Warn if no main camera found
        if (mainCamera == null)
        {
            Debug.LogWarning("PlayerAttack: No main camera found! Attack direction will default to forward.");
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartCharging();
        }

        if (isCharging)
        {
            chargeTimer += Time.deltaTime;

            // Ensure chargeTime is not zero to avoid division by zero
            if (chargeTime > 0)
            {
                currentDamage = Mathf.Lerp(baseDamage, maxDamage, chargeTimer / chargeTime);
            }
            else
            {
                currentDamage = maxDamage;
            }

            if (Input.GetMouseButtonUp(0))
            {
                PerformAttack();
                StopCharging();
            }
        }
    }

    private void StartCharging()
    {
        isCharging = true;
        chargeTimer = 0f;
        currentDamage = baseDamage;
    }

    private void StopCharging()
    {
        isCharging = false;
    }

    private void PerformAttack()
    {
        // Check if we have a valid camera for mouse position calculation
        if (mainCamera == null)
        {
            Debug.LogWarning("PlayerAttack: No camera available for attack direction calculation!");
            return;
        }

        // Check if transform is valid
        if (transform == null)
        {
            Debug.LogError("PlayerAttack: Transform is null!");
            return;
        }

        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f; // For 2D
        Vector3 attackDirection = (mousePos - transform.position).normalized;

        // Check if ZombieSpawner exists
        if (ZombieSpawner.Instance == null)
        {
            Debug.LogWarning("PlayerAttack: ZombieSpawner.Instance is null!");
            return;
        }

        // Get active zombies and check for null
        var activeZombies = ZombieSpawner.Instance.GetActiveZombies();
        if (activeZombies == null)
        {
            Debug.LogWarning("PlayerAttack: GetActiveZombies() returned null!");
            return;
        }

        // Find all active zombies
        foreach (ZombieMover zombie in activeZombies)
        {
            // Check if zombie is null or destroyed
            if (zombie == null)
            {
                continue;
            }

            // Check if zombie transform is valid
            if (zombie.transform == null)
            {
                continue;
            }

            Vector3 toZombie = zombie.transform.position - transform.position;
            float distance = toZombie.magnitude;

            if (distance > attackRange) continue;

            float angle = Vector3.Angle(attackDirection, toZombie.normalized);
            if (angle <= attackAngle / 2f)
            {
                IDamageable damageable = zombie.GetComponent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    damageable.TakeDamage(currentDamage, gameObject);
                    Debug.Log($"Player attacked {zombie.name} for {currentDamage} damage!");
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!isCharging) return;

        // Check if transform is valid
        if (transform == null) return;

        Vector3 mousePos;
        Vector3 attackDirection;

        if (mainCamera != null)
        {
            mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;
            attackDirection = (mousePos - transform.position).normalized;
        }
        else
        {
            // Fallback to forward direction if no camera
            attackDirection = transform.forward;
        }

        Gizmos.color = Color.red;

        // Draw range arc
        Vector3 from = Quaternion.Euler(0, 0, -attackAngle / 2) * attackDirection;
        Vector3 to = Quaternion.Euler(0, 0, attackAngle / 2) * attackDirection;

        DrawArc(transform.position, attackDirection, attackRange, attackAngle);

        // Draw sides
        Gizmos.DrawLine(transform.position, transform.position + from.normalized * attackRange);
        Gizmos.DrawLine(transform.position, transform.position + to.normalized * attackRange);
    }

    private void DrawArc(Vector3 center, Vector3 direction, float radius, float angle)
    {
        // Validate parameters
        if (radius <= 0 || angle <= 0) return;

        int segments = 20;
        float angleStep = angle / segments;
        Vector3 prevPoint = center + Quaternion.Euler(0, 0, -angle / 2) * direction * radius;

        for (int i = 1; i <= segments; i++)
        {
            Vector3 nextPoint = center + Quaternion.Euler(0, 0, -angle / 2 + i * angleStep) * direction * radius;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
}