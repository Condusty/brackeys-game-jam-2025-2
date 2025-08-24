using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [SerializeField] private int enemyCount = 3;
    [SerializeField] private float spawnRange = 2f;
    
    [Header("Timing Settings")]
    [SerializeField] private float spawnDelay = 0.5f;
    [SerializeField] private bool useRandomDelay = false;
    [SerializeField] private float minRandomDelay = 0.3f;
    [SerializeField] private float maxRandomDelay = 1.0f;
    
    [Header("Trigger Settings")]
    [SerializeField] private float triggerRange = 5f;
    [SerializeField] private bool spawnOnlyOnce = true;
    
    [Header("Spawn Position Settings")]
    [SerializeField] private bool useRandomPositions = true;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    
    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color triggerRangeColor = Color.yellow;
    [SerializeField] private Color spawnRangeColor = Color.red;
    
    private GameObject player;
    private bool hasSpawned = false;
    private bool isSpawning = false;
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    private void Start()
    {
        player = GameObject.FindWithTag("Player");
        
        if (player == null)
        {
            Debug.LogWarning($"EnemySpawner on {gameObject.name}: Player not found! Make sure the player has the 'Player' tag.");
        }
        
        if (enemyPrefabs.Count == 0)
        {
            Debug.LogWarning($"EnemySpawner on {gameObject.name}: No enemy prefabs assigned!");
        }
    }

    private void Update()
    {
        if (player == null || (hasSpawned && spawnOnlyOnce) || isSpawning)
            return;
            
        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
        
        if (distanceToPlayer <= triggerRange)
        {
            StartCoroutine(SpawnEnemiesWithDelay());
            
            if (spawnOnlyOnce)
                hasSpawned = true;
        }
    }

    private IEnumerator SpawnEnemiesWithDelay()
    {
        isSpawning = true;
        
        if (enemyPrefabs.Count == 0)
        {
            Debug.LogWarning($"EnemySpawner on {gameObject.name}: Cannot spawn enemies - no prefabs assigned!");
            isSpawning = false;
            yield break;
        }

        for (int i = 0; i < enemyCount; i++)
        {
            GameObject enemyToSpawn = GetRandomEnemyPrefab();
            Vector3 spawnPosition = GetSpawnPosition();
            
            GameObject spawnedEnemy = Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity);
            spawnedEnemies.Add(spawnedEnemy);
            
            Debug.Log($"EnemySpawner: Spawned {enemyToSpawn.name} at {spawnPosition} (Enemy {i + 1}/{enemyCount})");
            
            // Wait before spawning next enemy (except for the last one)
            if (i < enemyCount - 1)
            {
                float delay = GetSpawnDelay();
                yield return new WaitForSeconds(delay);
            }
        }
        
        isSpawning = false;
        Debug.Log($"EnemySpawner: Finished spawning all {enemyCount} enemies.");
    }

    private float GetSpawnDelay()
    {
        if (useRandomDelay)
        {
            return Random.Range(minRandomDelay, maxRandomDelay);
        }
        else
        {
            return spawnDelay;
        }
    }

    private void SpawnEnemies()
    {
        // Legacy method for immediate spawning - kept for compatibility
        StartCoroutine(SpawnEnemiesWithDelay());
    }

    private GameObject GetRandomEnemyPrefab()
    {
        int randomIndex = Random.Range(0, enemyPrefabs.Count);
        return enemyPrefabs[randomIndex];
    }

    private Vector3 GetSpawnPosition()
    {
        if (!useRandomPositions && spawnPoints.Count > 0)
        {
            // Use predefined spawn points
            int randomIndex = Random.Range(0, spawnPoints.Count);
            return spawnPoints[randomIndex].position;
        }
        else
        {
            // Generate random position within spawn range
            Vector2 randomOffset = Random.insideUnitCircle * spawnRange;
            return transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);
        }
    }

    // Public methods for external control
    public void ForceSpawn()
    {
        if (!isSpawning)
        {
            StartCoroutine(SpawnEnemiesWithDelay());
            hasSpawned = true;
        }
    }

    public void ForceSpawnImmediate()
    {
        // For cases where immediate spawning is needed
        if (enemyPrefabs.Count == 0)
        {
            Debug.LogWarning($"EnemySpawner on {gameObject.name}: Cannot spawn enemies - no prefabs assigned!");
            return;
        }

        for (int i = 0; i < enemyCount; i++)
        {
            GameObject enemyToSpawn = GetRandomEnemyPrefab();
            Vector3 spawnPosition = GetSpawnPosition();
            
            GameObject spawnedEnemy = Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity);
            spawnedEnemies.Add(spawnedEnemy);
        }
        hasSpawned = true;
    }

    public void ResetSpawner()
    {
        hasSpawned = false;
        isSpawning = false;
        StopAllCoroutines();
        
        // Optionally destroy previously spawned enemies
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        spawnedEnemies.Clear();
    }

    public void AddEnemyPrefab(GameObject enemyPrefab)
    {
        if (enemyPrefab != null && !enemyPrefabs.Contains(enemyPrefab))
        {
            enemyPrefabs.Add(enemyPrefab);
        }
    }

    public void RemoveEnemyPrefab(GameObject enemyPrefab)
    {
        enemyPrefabs.Remove(enemyPrefab);
    }

    // Gizmos for visual feedback in editor
    private void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        // Draw trigger range
        Gizmos.color = triggerRangeColor;
        Gizmos.DrawWireSphere(transform.position, triggerRange);

        // Draw spawn range
        Gizmos.color = spawnRangeColor;
        Gizmos.DrawWireSphere(transform.position, spawnRange);

        // Draw spawn points if not using random positions
        if (!useRandomPositions && spawnPoints.Count > 0)
        {
            Gizmos.color = Color.blue;
            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawWireCube(spawnPoint.position, Vector3.one * 0.5f);
                    Gizmos.DrawLine(transform.position, spawnPoint.position);
                }
            }
        }

        // Draw spawner center
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);

        // Show status indicator
        if (Application.isPlaying)
        {
            if (isSpawning)
            {
                Gizmos.color = Color.yellow;
            }
            else if (hasSpawned)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.red;
            }
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, Vector3.one * 0.2f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        // Draw more detailed gizmos when selected
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.1f);

        // Draw potential spawn positions preview
        if (useRandomPositions)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
            Gizmos.DrawSphere(transform.position, spawnRange);
        }
    }
}
