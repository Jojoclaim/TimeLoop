using UnityEngine;
using UnityEngine.AI;

public enum BehaviorType { Chase, Block, Flank }

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieMover : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform target;
    private ZombieConfig config;
    private bool isConfused;
    private float confusionTimer;
    private float flankAngle = 90f;
    private SpriteRenderer spriteRenderer;
    private ZombieAttack zombieAttack;

    public bool IsConfused => isConfused;
    public Transform GetCurrentTarget() => target;
    public Vector3 Velocity => agent != null ? agent.velocity : Vector3.zero;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        zombieAttack = GetComponent<ZombieAttack>();
        agent.updateRotation = false; // Optimized for 2D top-down
        agent.updateUpAxis = false;
    }

    public void Initialize(ZombieConfig zombieConfig, Transform playerTransform)
    {
        config = zombieConfig;
        target = playerTransform;
        isConfused = false;
        confusionTimer = 0f;
        agent.speed = config.baseSpeed * DifficultyManager.Instance.GetSpeedMultiplier();
    }

    private void Update()
    {
        if (target == null)
        {
            Debug.LogWarning("Zombie target (player) is null! Check if player is assigned in ZombieSpawner.");
            return;
        }

        HandleConfusion();

        bool attacking = zombieAttack != null && zombieAttack.IsAttacking;

        if (attacking)
        {
            if (!agent.isStopped) agent.isStopped = true;
        }
        else
        {
            if (agent.isStopped) agent.isStopped = false;
            Vector3 destination = CalculateDestination();
            agent.SetDestination(destination);
        }

        // Flip based on movement direction
        float dirX = agent.velocity.x;
        if (Mathf.Abs(dirX) > 0.01f)
        {
            transform.localScale = new Vector3(Mathf.Sign(dirX), transform.localScale.y, transform.localScale.z);
        }
    }

    private void HandleConfusion()
    {
        if (!isConfused)
        {
            float effectiveChance = config.confusionChance * DifficultyManager.Instance.GetConfusionMultiplier();
            if (Random.value < effectiveChance && ZombieSpawner.Instance.CanConfuse())
            {
                ZombieMover mistaken = ZombieSpawner.Instance.GetRandomZombie(this);
                if (mistaken != null)
                {
                    target = mistaken.transform;
                    isConfused = true;
                    confusionTimer = config.confusionDuration;
                    ZombieSpawner.Instance.RegisterConfusion(true);
                }
            }
        }
        else
        {
            confusionTimer -= Time.deltaTime;
            if (confusionTimer <= 0f)
            {
                target = ZombieSpawner.Instance.GetPlayerTransform();
                isConfused = false;
                ZombieSpawner.Instance.RegisterConfusion(false);
            }
        }
    }

    private Vector3 CalculateDestination()
    {
        switch (config.behaviorType)
        {
            case BehaviorType.Chase:
                return target.position; // Direct pursuit
            case BehaviorType.Block:
                Vector3 playerVelocity = ZombieSpawner.Instance.GetPlayerRigidbody()?.linearVelocity ?? Vector3.zero;
                return target.position + playerVelocity.normalized * config.blockDistance;
            case BehaviorType.Flank:
                Vector3 toTarget = target.position - transform.position;
                Vector3 flankDir = Quaternion.Euler(0, 0, flankAngle) * toTarget.normalized;
                flankAngle = -flankAngle; // Alternate for circling
                return target.position + flankDir * config.flankDistance;
            default:
                return target.position;
        }
    }
}