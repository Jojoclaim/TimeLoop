using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CircleCollider2D))]
public class ZombieAI : MonoBehaviour, IDamageable
{
    [Header("Current State")]
    [SerializeField] private ZombieType zombieType;
    [SerializeField] private float currentHealth;
    [SerializeField] private ZombieState currentState = ZombieState.Idle;
    [SerializeField] private TacticalRole currentRole;
    [SerializeField] private bool isLeader = false;

    [Header("Debug Info")]
    [SerializeField] private Vector3 targetPredictedPosition;
    [SerializeField] private Vector3 assignedPosition;
    [SerializeField] private float playerVelocityMagnitude;

    private NavMeshAgent agent;
    private SpriteRenderer spriteRenderer;
    private Transform playerTransform;
    private IDamageable playerDamageable;
    private Rigidbody2D playerRigidbody;

    // Group coordination
    private List<ZombieAI> squadMembers = new List<ZombieAI>();
    private ZombieAI squadLeader;
    private Dictionary<ZombieAI, float> lastCommunicationTime = new Dictionary<ZombieAI, float>();

    // Tactical information
    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity;
    private float lastPathUpdateTime;
    private float lastTacticalUpdateTime;
    private float stuckTimer = 0f;
    private Vector3 lastPosition;

    // Attack system
    private float lastAttackTime;
    private List<Vector3> escapeRoutes = new List<Vector3>();
    private bool hasLineOfSight = false;

    // Cached values
    private float effectiveSpeed;
    private float effectiveDamage;
    private Color originalColor;

    public bool IsAlive => currentHealth > 0;
    public Transform GetTransform() => transform;
    public bool IsLeader => isLeader;
    public TacticalRole CurrentRole => currentRole;

    private enum ZombieState
    {
        Idle,
        Searching,
        Pursuing,
        Flanking,
        Surrounding,
        Blocking,
        Ambushing,
        Attacking
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Initialize(ZombieType type, Vector3 spawnPosition)
    {
        zombieType = type;
        transform.position = spawnPosition;
        currentHealth = type.health;
        currentState = ZombieState.Idle;
        currentRole = type.preferredRole;

        // Setup NavMeshAgent
        agent.speed = type.moveSpeed * ZombieManager.Instance.GetSpeedMultiplier();
        agent.stoppingDistance = type.attackRange * 0.8f;
        agent.radius = 0.5f;
        agent.acceleration = 8f;

        // Apply visuals
        if (spriteRenderer != null && type.sprite != null)
        {
            spriteRenderer.sprite = type.sprite;
            spriteRenderer.color = type.tintColor;
            originalColor = type.tintColor;
        }

        // Cache values
        UpdateCachedValues();

        // Get player reference
        playerTransform = ZombieManager.Instance.GetPlayerTransform();
        if (playerTransform != null)
        {
            playerDamageable = playerTransform.GetComponent<IDamageable>();
            playerRigidbody = playerTransform.GetComponent<Rigidbody2D>();
        }

        // Determine if this zombie should be a leader
        if (type.canBeLeader && Random.value < 0.2f)
        {
            BecomeLeader();
        }
    }

    private void UpdateCachedValues()
    {
        effectiveSpeed = zombieType.moveSpeed * ZombieManager.Instance.GetSpeedMultiplier();
        effectiveDamage = zombieType.attackDamage * ZombieManager.Instance.GetDamageMultiplier();

        if (squadMembers.Count > 2)
        {
            effectiveSpeed *= zombieType.coordinationBonus;
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            playerTransform = ZombieManager.Instance.GetPlayerTransform();
            if (playerTransform == null) return;
        }

        UpdatePlayerTracking();
        UpdateLineOfSight();

        if (Time.time - lastTacticalUpdateTime > zombieType.tacticalUpdateInterval)
        {
            UpdateTacticalState();
            lastTacticalUpdateTime = Time.time;
        }

        if (Time.time - lastPathUpdateTime > zombieType.pathUpdateInterval)
        {
            UpdatePathfinding();
            lastPathUpdateTime = Time.time;
        }

        UpdateStuckDetection();
        UpdateVisuals();
    }

    private void UpdatePlayerTracking()
    {
        if (playerTransform == null) return;

        // Calculate player velocity
        Vector3 currentPlayerPos = playerTransform.position;
        if (lastPlayerPosition != Vector3.zero)
        {
            playerVelocity = (currentPlayerPos - lastPlayerPosition) / Time.deltaTime;
            playerVelocityMagnitude = playerVelocity.magnitude;
        }
        lastPlayerPosition = currentPlayerPos;

        // Predict player position
        float predictionTime = zombieType.predictionTime * zombieType.intelligenceLevel;
        targetPredictedPosition = currentPlayerPos + playerVelocity * predictionTime;

        // Analyze escape routes
        if (isLeader && Time.frameCount % 30 == 0) // Every 0.5 seconds at 60fps
        {
            AnalyzeEscapeRoutes();
        }
    }

    private void UpdateLineOfSight()
    {
        if (playerTransform == null) return;

        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distanceToPlayer,
            LayerMask.GetMask("Default", "Obstacles"));

        hasLineOfSight = hit.collider == null || hit.collider.transform == playerTransform;
    }

    private void UpdateTacticalState()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= zombieType.attackRange)
        {
            currentState = ZombieState.Attacking;
            return;
        }

        // Leader makes tactical decisions
        if (isLeader)
        {
            CoordinateSquad();
        }

        // Individual tactical decisions based on role
        switch (currentRole)
        {
            case TacticalRole.Chaser:
                currentState = hasLineOfSight ? ZombieState.Pursuing : ZombieState.Searching;
                break;

            case TacticalRole.Flanker:
                currentState = squadMembers.Count > 0 ? ZombieState.Flanking : ZombieState.Pursuing;
                break;

            case TacticalRole.Blocker:
                currentState = escapeRoutes.Count > 0 ? ZombieState.Blocking : ZombieState.Pursuing;
                break;

            case TacticalRole.Ambusher:
                currentState = ShouldAmbush() ? ZombieState.Ambushing : ZombieState.Searching;
                break;

            case TacticalRole.Leader:
                currentState = ZombieState.Surrounding;
                break;
        }
    }

    private void UpdatePathfinding()
    {
        if (!agent.enabled) return;

        Vector3 targetPosition = transform.position;

        switch (currentState)
        {
            case ZombieState.Pursuing:
                targetPosition = targetPredictedPosition;
                break;

            case ZombieState.Searching:
                targetPosition = lastPlayerPosition != Vector3.zero ? lastPlayerPosition : targetPredictedPosition;
                break;

            case ZombieState.Flanking:
                targetPosition = CalculateFlankingPosition();
                break;

            case ZombieState.Surrounding:
                targetPosition = CalculateSurroundingPosition();
                break;

            case ZombieState.Blocking:
                targetPosition = CalculateBlockingPosition();
                break;

            case ZombieState.Ambushing:
                targetPosition = CalculateAmbushPosition();
                break;

            case ZombieState.Attacking:
                targetPosition = playerTransform.position;
                break;
        }

        assignedPosition = targetPosition;

        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private Vector3 CalculateFlankingPosition()
    {
        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        float angle = zombieType.flankingAngle;

        // Choose flank side based on position relative to other zombies
        if (squadMembers.Count > 0)
        {
            Vector3 avgSquadPos = GetAverageSquadPosition();
            Vector3 squadToPlayer = (playerTransform.position - avgSquadPos).normalized;
            float cross = Vector3.Cross(squadToPlayer, dirToPlayer).z;
            angle *= (cross > 0) ? 1 : -1;
        }
        else
        {
            angle *= (Random.value > 0.5f) ? 1 : -1;
        }

        Vector3 flankDir = Quaternion.Euler(0, 0, angle) * dirToPlayer;
        return playerTransform.position + flankDir * zombieType.surroundDistance;
    }

    private Vector3 CalculateSurroundingPosition()
    {
        if (squadMembers.Count < 2)
            return targetPredictedPosition;

        // Calculate position in surrounding formation
        int myIndex = isLeader ? 0 : squadMembers.IndexOf(this) + 1;
        float angleStep = 360f / (squadMembers.Count + 1);
        float myAngle = angleStep * myIndex;

        Vector3 offset = Quaternion.Euler(0, 0, myAngle) * Vector3.right * zombieType.surroundDistance;
        return targetPredictedPosition + offset;
    }

    private Vector3 CalculateBlockingPosition()
    {
        if (escapeRoutes.Count == 0)
            return targetPredictedPosition;

        // Find the escape route closest to our position
        Vector3 bestBlockPosition = escapeRoutes[0];
        float closestDistance = float.MaxValue;

        foreach (var route in escapeRoutes)
        {
            float dist = Vector3.Distance(transform.position, route);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                bestBlockPosition = route;
            }
        }

        return bestBlockPosition;
    }

    private Vector3 CalculateAmbushPosition()
    {
        // Find a hidden position near predicted player path
        Vector3 playerDirection = playerVelocity.normalized;
        Vector3 ambushPoint = targetPredictedPosition + playerDirection * 5f;

        // Look for cover near the ambush point
        Collider2D[] obstacles = Physics2D.OverlapCircleAll(ambushPoint, 3f, LayerMask.GetMask("Obstacles"));
        if (obstacles.Length > 0)
        {
            Vector3 nearestObstacle = obstacles[0].transform.position;
            Vector3 hideDirection = (nearestObstacle - ambushPoint).normalized;
            return nearestObstacle - hideDirection * 1.5f;
        }

        return ambushPoint;
    }

    private void CoordinateSquad()
    {
        if (squadMembers.Count < 2) return;

        // Analyze situation
        float avgDistanceToPlayer = GetAverageDistanceToPlayer();
        int zombiesWithLOS = CountZombiesWithLineOfSight();

        // Assign roles based on situation
        AssignTacticalRoles(avgDistanceToPlayer, zombiesWithLOS);

        // Share player information
        SharePlayerInformation();
    }

    private void AssignTacticalRoles(float avgDistance, int withLOS)
    {
        List<ZombieAI> unassigned = new List<ZombieAI>(squadMembers);

        // Ensure we have chasers if player is visible
        int chasersNeeded = Mathf.Min(2, withLOS);
        AssignRoleToClosest(unassigned, TacticalRole.Chaser, chasersNeeded);

        // Assign flankers
        int flankersNeeded = Mathf.Min(2, unassigned.Count);
        AssignRoleToFastest(unassigned, TacticalRole.Flanker, flankersNeeded);

        // Assign blockers if we identified escape routes
        if (escapeRoutes.Count > 0)
        {
            int blockersNeeded = Mathf.Min(escapeRoutes.Count, unassigned.Count);
            AssignRoleToClosest(unassigned, TacticalRole.Blocker, blockersNeeded);
        }

        // Rest become chasers or ambushers
        foreach (var zombie in unassigned)
        {
            zombie.currentRole = zombie.zombieType.canSetAmbush ?
                TacticalRole.Ambusher : TacticalRole.Chaser;
        }
    }

    private void AssignRoleToClosest(List<ZombieAI> available, TacticalRole role, int count)
    {
        var sorted = available.OrderBy(z => Vector3.Distance(z.transform.position, playerTransform.position)).ToList();

        for (int i = 0; i < Mathf.Min(count, sorted.Count); i++)
        {
            sorted[i].currentRole = role;
            available.Remove(sorted[i]);
        }
    }

    private void AssignRoleToFastest(List<ZombieAI> available, TacticalRole role, int count)
    {
        var sorted = available.OrderByDescending(z => z.zombieType.moveSpeed).ToList();

        for (int i = 0; i < Mathf.Min(count, sorted.Count); i++)
        {
            sorted[i].currentRole = role;
            available.Remove(sorted[i]);
        }
    }

    private void SharePlayerInformation()
    {
        foreach (var member in squadMembers)
        {
            if (!member.hasLineOfSight && hasLineOfSight)
            {
                member.lastPlayerPosition = lastPlayerPosition;
                member.playerVelocity = playerVelocity;
                member.targetPredictedPosition = targetPredictedPosition;
            }
        }
    }

    private void AnalyzeEscapeRoutes()
    {
        escapeRoutes.Clear();

        // Check 8 directions around player
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.right;
            Vector3 checkPoint = playerTransform.position + direction * 5f;

            // Check if this route is clear
            if (!Physics2D.Raycast(playerTransform.position, direction, 5f, LayerMask.GetMask("Obstacles")))
            {
                // Check if any zombie is already blocking this route
                bool blocked = false;
                foreach (var zombie in squadMembers)
                {
                    if (Vector3.Distance(zombie.transform.position, checkPoint) < 2f)
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                {
                    escapeRoutes.Add(checkPoint);
                }
            }
        }
    }

    private bool ShouldAmbush()
    {
        // Check if player is moving towards us
        if (playerVelocity.magnitude < 0.5f) return false;

        Vector3 playerToUs = (transform.position - playerTransform.position).normalized;
        float dot = Vector3.Dot(playerVelocity.normalized, playerToUs);

        // Player is moving towards us and we're hidden
        return dot > 0.5f && !hasLineOfSight;
    }

    private void UpdateStuckDetection()
    {
        if (agent.velocity.magnitude < 0.1f && currentState != ZombieState.Ambushing)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > zombieType.stuckDetectionTime)
            {
                // Try alternative path
                Vector3 randomOffset = Random.insideUnitCircle * 2f;
                agent.SetDestination(transform.position + randomOffset);
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = transform.position;
    }

    private void BecomeLeader()
    {
        isLeader = true;
        currentRole = TacticalRole.Leader;

        // Visual indication
        if (spriteRenderer != null)
        {
            // Add glow effect or particle system here
            spriteRenderer.color = Color.Lerp(originalColor, zombieType.leaderGlowColor, 0.5f);
        }
    }

    public void JoinSquad(ZombieAI leader)
    {
        if (squadLeader != null && squadLeader != leader)
        {
            squadLeader.squadMembers.Remove(this);
        }

        squadLeader = leader;
        if (!leader.squadMembers.Contains(this))
        {
            leader.squadMembers.Add(this);
        }

        UpdateCachedValues();
    }

    public void CommunicateWith(ZombieAI other)
    {
        if (Vector3.Distance(transform.position, other.transform.position) > zombieType.communicationRange)
            return;

        // Share information
        if (hasLineOfSight && !other.hasLineOfSight)
        {
            other.lastPlayerPosition = lastPlayerPosition;
            other.targetPredictedPosition = targetPredictedPosition;
        }

        // Form squads
        if (isLeader && !other.isLeader && other.squadLeader == null)
        {
            other.JoinSquad(this);
        }
        else if (!isLeader && other.isLeader && squadLeader == null)
        {
            JoinSquad(other);
        }
    }

    private void UpdateVisuals()
    {
        // Face movement direction
        if (agent.velocity.magnitude > 0.1f && spriteRenderer != null)
        {
            spriteRenderer.flipX = agent.velocity.x < 0;
        }

        // Show role indicator (you can expand this with UI elements)
        if (isLeader)
        {
            // Leader glow is already applied
        }
    }

    // Combat methods
    public void TakeDamage(float damage, GameObject attacker)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Alert nearby zombies
            AlertNearbyZombies(attacker.transform.position);
        }
    }

    private void AlertNearbyZombies(Vector3 threatPosition)
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, zombieType.communicationRange);

        foreach (var collider in nearbyColliders)
        {
            ZombieAI zombie = collider.GetComponent<ZombieAI>();
            if (zombie != null && zombie != this)
            {
                zombie.lastPlayerPosition = threatPosition;
                if (!zombie.hasLineOfSight)
                {
                    zombie.currentState = ZombieState.Searching;
                }
            }
        }
    }

    private void Die()
    {
        if (isLeader && squadMembers.Count > 0)
        {
            // Pass leadership to another zombie
            ZombieAI newLeader = squadMembers.FirstOrDefault(z => z.zombieType.canBeLeader);
            if (newLeader != null)
            {
                newLeader.BecomeLeader();
                newLeader.squadMembers = squadMembers;
                newLeader.squadMembers.Remove(newLeader);
            }
        }

        if (squadLeader != null)
        {
            squadLeader.squadMembers.Remove(this);
        }

        ZombieManager.Instance.ReturnZombie(this);
    }

    // Helper methods
    private Vector3 GetAverageSquadPosition()
    {
        if (squadMembers.Count == 0) return transform.position;

        Vector3 sum = transform.position;
        foreach (var member in squadMembers)
        {
            sum += member.transform.position;
        }
        return sum / (squadMembers.Count + 1);
    }

    private float GetAverageDistanceToPlayer()
    {
        float sum = Vector3.Distance(transform.position, playerTransform.position);
        foreach (var member in squadMembers)
        {
            sum += Vector3.Distance(member.transform.position, playerTransform.position);
        }
        return sum / (squadMembers.Count + 1);
    }

    private int CountZombiesWithLineOfSight()
    {
        int count = hasLineOfSight ? 1 : 0;
        foreach (var member in squadMembers)
        {
            if (member.hasLineOfSight) count++;
        }
        return count;
    }

    private void OnDrawGizmosSelected()
    {
        if (zombieType == null) return;

        // Current state
        Gizmos.color = GetStateColor();
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Communication range
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(transform.position, zombieType.communicationRange);

        // Target position
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, assignedPosition);
        Gizmos.DrawWireSphere(assignedPosition, 0.3f);

        // Predicted player position
        if (playerTransform != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(targetPredictedPosition, 0.5f);
            Gizmos.DrawLine(playerTransform.position, targetPredictedPosition);
        }

        // Squad connections
        if (isLeader)
        {
            Gizmos.color = Color.yellow;
            foreach (var member in squadMembers)
            {
                if (member != null)
                    Gizmos.DrawLine(transform.position, member.transform.position);
            }
        }

        // Escape routes (for leaders)
        if (isLeader && escapeRoutes.Count > 0)
        {
            Gizmos.color = Color.green;
            foreach (var route in escapeRoutes)
            {
                Gizmos.DrawLine(playerTransform.position, route);
                Gizmos.DrawWireSphere(route, 0.5f);
            }
        }
    }

    private Color GetStateColor()
    {
        switch (currentState)
        {
            case ZombieState.Idle: return Color.gray;
            case ZombieState.Searching: return Color.yellow;
            case ZombieState.Pursuing: return Color.red;
            case ZombieState.Flanking: return Color.blue;
            case ZombieState.Surrounding: return Color.cyan;
            case ZombieState.Blocking: return Color.green;
            case ZombieState.Ambushing: return Color.magenta;
            case ZombieState.Attacking: return Color.red;
            default: return Color.white;
        }
    }
}