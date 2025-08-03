using UnityEngine;

[CreateAssetMenu(fileName = "ZombieConfig", menuName = "Zombie/ZombieConfig", order = 1)]
public class ZombieConfig : ScriptableObject
{
    [Header("General Settings")]
    public float baseSpeed = 3f;
    public float confusionChance = 0.05f;
    public float confusionDuration = 5f;

    [Header("Behavior Settings")]
    public BehaviorType behaviorType = BehaviorType.Chase;
    public float blockDistance = 2f; // For Block behavior
    public float flankDistance = 3f; // For Flank behavior
}