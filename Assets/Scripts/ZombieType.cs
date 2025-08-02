using UnityEngine;

[CreateAssetMenu(fileName = "ZombieType", menuName = "Game/Zombie Type")]
public class ZombieType : ScriptableObject
{
    [Header("Basic Stats")]
    public string typeName = "Basic Zombie";
    public float moveSpeed = 2f;
    public float detectionRange = 10f;
    public float attackRange = 1.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 1f;
    public float health = 100f;

    [Header("Attack Settings")]
    public float attackRadius = 0.8f;
    public float attackAngle = 60f;
    public float friendlyFireDamageMultiplier = 0.5f;
    public bool canDamageSameType = false;

    [Header("AI Intelligence")]
    public float intelligenceLevel = 1f; // 0-1, affects decision making
    public float predictionTime = 1f; // How far ahead to predict player movement
    public float communicationRange = 8f; // Range to share info with other zombies
    public bool canBeLeader = false; // Can coordinate other zombies
    public float tacticalUpdateInterval = 0.5f;

    [Header("Tactical Behavior")]
    public TacticalRole preferredRole = TacticalRole.Chaser;
    public float flankingAngle = 45f; // Angle for flanking maneuvers
    public float surroundDistance = 3f; // Optimal distance for surrounding
    public bool canSetAmbush = false;
    public float coordinationBonus = 1.2f; // Speed/damage bonus when coordinating

    [Header("Pathfinding")]
    public float pathUpdateInterval = 0.25f;
    public float stuckDetectionTime = 2f; // Time before considering itself stuck
    public float avoidanceForce = 2f;
    public float separationRadius = 1f;

    [Header("Visuals")]
    public Sprite sprite;
    public Color tintColor = Color.white;
    public Color attackTintColor = Color.red;
    public Color leaderGlowColor = Color.yellow;
}

public enum TacticalRole
{
    Chaser,      // Direct pursuit
    Flanker,     // Attack from sides
    Ambusher,    // Hide and wait
    Blocker,     // Cut off escape routes
    Leader       // Coordinate others
}