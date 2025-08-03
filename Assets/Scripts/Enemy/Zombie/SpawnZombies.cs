using System.Collections;
using UnityEngine;

public class SpawnZombies : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField, Range(1f, 60f)] private float spawnInterval = 5f;

    private void Start()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points assigned to SpawnZombies!");
            return;
        }

        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            ZombieSpawner.Instance.SpawnZombie(randomPoint.position);
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}