using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("Difficulty Settings")]
    [SerializeField, Range(0, 3)] private int difficultyLevel = 1; // 0: Easy, 1: Medium, 2: Hard, 3: Expert
    [SerializeField, Range(0f, 0.5f)] private float maxConfusionFraction = 0.1f; // Max fraction confused

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public float GetSpeedMultiplier() => 1f + difficultyLevel * 0.5f;

    public float GetConfusionMultiplier() => 1f + difficultyLevel * 0.2f;

    public float GetMaxConfusionFraction() => maxConfusionFraction;

    public int GetDifficultyLevel() => difficultyLevel;
}