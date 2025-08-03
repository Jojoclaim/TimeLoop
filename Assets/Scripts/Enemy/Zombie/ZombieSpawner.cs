using System.Collections.Generic;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    public static ZombieSpawner Instance { get; private set; }

    [Header("Pooling Settings")]
    [SerializeField] private GameObject zombiePrefab;
    [SerializeField, Range(10, 100)] private int initialPoolSize = 50;

    [Header("Configuration")]
    [SerializeField] private List<ZombieConfig> zombieConfigs = new List<ZombieConfig>();

    [Header("Player References")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D playerRigidbody;

    private Queue<ZombieMover> zombiePool = new Queue<ZombieMover>();
    private List<ZombieMover> activeZombies = new List<ZombieMover>();
    private int confusedCount = 0;
    private Transform playerTransform;

    private void OnValidate()
    {
        if (player != null)
        {
            playerRigidbody ??= player.GetComponent<Rigidbody2D>();
        }
    }

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

    private void Start()
    {
        player ??= GameObject.FindGameObjectWithTag("Player")?.transform;
        playerTransform = player;
        playerRigidbody ??= playerTransform?.GetComponent<Rigidbody2D>();
        if (playerTransform == null)
        {
            Debug.LogError("Player Transform not assigned in Inspector and not found by tag 'Player'! Zombies won't move.");
        }
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject obj = Instantiate(zombiePrefab);
            obj.SetActive(false);
            ZombieMover zombie = obj.GetComponent<ZombieMover>();
            zombiePool.Enqueue(zombie);
        }
    }

    public ZombieMover SpawnZombie(Vector3 position)
    {
        if (zombiePool.Count == 0) return null; // Pool expansion can be added if needed

        ZombieMover zombie = zombiePool.Dequeue();
        zombie.transform.position = position;
        zombie.gameObject.SetActive(true);

        // Assign random config for variety and collaboration simulation
        if (zombieConfigs.Count > 0)
        {
            ZombieConfig randomConfig = zombieConfigs[Random.Range(0, zombieConfigs.Count)];
            zombie.Initialize(randomConfig, playerTransform);
        }
        else
        {
            Debug.LogWarning("No ZombieConfigs assigned!");
        }

        activeZombies.Add(zombie);
        return zombie;
    }

    public void DespawnZombie(ZombieMover zombie)
    {
        zombie.gameObject.SetActive(false);
        activeZombies.Remove(zombie);
        if (zombie.IsConfused)
        {
            confusedCount = Mathf.Max(0, confusedCount - 1);
        }
        zombiePool.Enqueue(zombie);
    }

    public bool CanConfuse()
    {
        return confusedCount < (int)(activeZombies.Count * DifficultyManager.Instance.GetMaxConfusionFraction());
    }

    public void RegisterConfusion(bool isConfused)
    {
        confusedCount += isConfused ? 1 : -1;
    }

    public ZombieMover GetRandomZombie(ZombieMover exclude)
    {
        if (activeZombies.Count <= 1) return null;
        ZombieMover target;
        do
        {
            target = activeZombies[Random.Range(0, activeZombies.Count)];
        } while (target == exclude);
        return target;
    }

    public Transform GetPlayerTransform() => playerTransform;

    public Rigidbody2D GetPlayerRigidbody() => playerRigidbody;

    public List<ZombieMover> GetActiveZombies() => activeZombies;
}