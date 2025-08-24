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
    [SerializeField] private bool showEnemyCountDebug = true;
    [SerializeField] private Color triggerRangeColor = Color.yellow;
    [SerializeField] private Color spawnRangeColor = Color.red;
    
    // Events
    public System.Action<EnemySpawner> OnAllEnemiesDead;
    
    private GameObject player;
    private bool hasSpawned = false;
    private bool isSpawning = false;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private bool allEnemiesDeadEventFired = false;
    private bool isCheckingDeadEnemies = false;
    private int totalEnemiesSpawned = 0; // Track total enemies spawned in this session

    // Properties for debugging and external access
    public int AliveEnemyCount => GetAliveEnemyCount();
    public int TotalSpawnedCount => spawnedEnemies.Count;
    public int TotalEnemiesSpawned => totalEnemiesSpawned;
    public bool AllEnemiesDead => AliveEnemyCount == 0 && totalEnemiesSpawned > 0;

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
        
        // Debug: Check if anyone is subscribed to our event
        Debug.Log($"EnemySpawner ({gameObject.name}): OnAllEnemiesDead subscribers: {(OnAllEnemiesDead?.GetInvocationList()?.Length ?? 0)}");
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
        
        // Check if all enemies are dead (but not if already checking)
        if (!isCheckingDeadEnemies)
        {
            CheckAllEnemiesDead();
        }
    }

    private void CheckAllEnemiesDead()
    {
        if (!allEnemiesDeadEventFired && hasSpawned && !isSpawning)
        {
            if (showEnemyCountDebug)
            {
                Debug.Log($"EnemySpawner: Checking dead enemies... Alive: {AliveEnemyCount}, Total: {TotalSpawnedCount}, TotalSpawned: {TotalEnemiesSpawned}");
            }
            
            // Start coroutine to check after frame delay
            StartCoroutine(CheckAllEnemiesDeadDelayed());
        }
    }

    private IEnumerator CheckAllEnemiesDeadDelayed()
    {
        isCheckingDeadEnemies = true;
        
        // Wait one frame for Destroy() to take effect
        yield return new WaitForEndOfFrame();
        
        CleanUpDeadEnemies();
        
        if (showEnemyCountDebug)
        {
            Debug.Log($"EnemySpawner: After cleanup - Alive: {AliveEnemyCount}, Total: {TotalSpawnedCount}, TotalSpawned: {TotalEnemiesSpawned}, AllDead: {AllEnemiesDead}");
        }
        
        if (AllEnemiesDead)
        {
            allEnemiesDeadEventFired = true;
            
            if (showEnemyCountDebug)
            {
                Debug.Log($"EnemySpawner ({gameObject.name}): All enemies are dead! Firing OnAllEnemiesDead event.");
                Debug.Log($"Event subscribers: {(OnAllEnemiesDead?.GetInvocationList()?.Length ?? 0)}");
            }
            
            if (OnAllEnemiesDead != null)
            {
                Debug.Log($"EnemySpawner: Invoking OnAllEnemiesDead event...");
                OnAllEnemiesDead.Invoke(this);
                Debug.Log($"EnemySpawner: Event invoked successfully!");
            }
            else
            {
                Debug.LogWarning($"EnemySpawner: OnAllEnemiesDead event is null! No subscribers!");
            }
        }
        
        isCheckingDeadEnemies = false;
    }

    private void CleanUpDeadEnemies()
    {
        int beforeCount = spawnedEnemies.Count;
        
        // Remove null references (destroyed enemies)
        spawnedEnemies.RemoveAll(enemy => enemy == null);
        
        int afterCount = spawnedEnemies.Count;
        int removedCount = beforeCount - afterCount;
        
        if (showEnemyCountDebug && removedCount > 0)
        {
            Debug.Log($"EnemySpawner: Cleaned up {removedCount} dead enemies. Remaining: {afterCount}");
        }
    }

    private int GetAliveEnemyCount()
    {
        int aliveCount = 0;
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
                aliveCount++;
        }
        return aliveCount;
    }

    private IEnumerator SpawnEnemiesWithDelay()
    {
        isSpawning = true;
        allEnemiesDeadEventFired = false; // Reset event flag when spawning new enemies
        
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
            totalEnemiesSpawned++; // Increment total counter
            
            if (showEnemyCountDebug)
            {
                Debug.Log($"EnemySpawner: Spawned {enemyToSpawn.name} at {spawnPosition} (Enemy {i + 1}/{enemyCount})");
            }
            
            // Wait before spawning next enemy (except for the last one)
            if (i < enemyCount - 1)
            {
                float delay = GetSpawnDelay();
                yield return new WaitForSeconds(delay);
            }
        }
        
        isSpawning = false;
        
        if (showEnemyCountDebug)
        {
            Debug.Log($"EnemySpawner: Finished spawning all {enemyCount} enemies. Total alive: {AliveEnemyCount}, Total spawned this session: {TotalEnemiesSpawned}");
        }
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

        allEnemiesDeadEventFired = false; // Reset event flag

        for (int i = 0; i < enemyCount; i++)
        {
            GameObject enemyToSpawn = GetRandomEnemyPrefab();
            Vector3 spawnPosition = GetSpawnPosition();
            
            GameObject spawnedEnemy = Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity);
            spawnedEnemies.Add(spawnedEnemy);
            totalEnemiesSpawned++; // Increment total counter
        }
        hasSpawned = true;
        
        if (showEnemyCountDebug)
        {
            Debug.Log($"EnemySpawner: Force spawned {enemyCount} enemies immediately. Total alive: {AliveEnemyCount}, Total spawned this session: {TotalEnemiesSpawned}");
        }
    }

    public void ResetSpawner()
    {
        hasSpawned = false;
        isSpawning = false;
        allEnemiesDeadEventFired = false;
        isCheckingDeadEnemies = false;
        totalEnemiesSpawned = 0; // Reset total counter
        StopAllCoroutines();
        
        // Optionally destroy previously spawned enemies
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        spawnedEnemies.Clear();
        
        if (showEnemyCountDebug)
        {
            Debug.Log($"EnemySpawner: Spawner reset. All enemies destroyed.");
        }
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

    // Debug methods
    [ContextMenu("Debug: Show Enemy Status")]
    public void DebugShowEnemyStatus()
    {
        CleanUpDeadEnemies();
        Debug.Log($"EnemySpawner ({gameObject.name}) Status:\n" +
                  $"- Current List Count: {TotalSpawnedCount}\n" +
                  $"- Total Spawned This Session: {TotalEnemiesSpawned}\n" +
                  $"- Alive Count: {AliveEnemyCount}\n" +
                  $"- All Enemies Dead: {AllEnemiesDead}\n" +
                  $"- Has Spawned: {hasSpawned}\n" +
                  $"- Is Spawning: {isSpawning}\n" +
                  $"- Event Fired: {allEnemiesDeadEventFired}\n" +
                  $"- Is Checking: {isCheckingDeadEnemies}\n" +
                  $"- Event Subscribers: {(OnAllEnemiesDead?.GetInvocationList()?.Length ?? 0)}");
    }

    [ContextMenu("Debug: Force Check All Dead")]
    public void DebugForceCheckAllDead()
    {
        if (!isCheckingDeadEnemies)
        {
            Debug.Log("Manually forcing dead check...");
            StartCoroutine(CheckAllEnemiesDeadDelayed());
        }
    }

    [ContextMenu("Debug: Test Event")]
    public void DebugTestEvent()
    {
        Debug.Log("Testing OnAllEnemiesDead event manually...");
        if (OnAllEnemiesDead != null)
        {
            Debug.Log($"Invoking event with {OnAllEnemiesDead.GetInvocationList().Length} subscribers");
            OnAllEnemiesDead.Invoke(this);
        }
        else
        {
            Debug.LogError("OnAllEnemiesDead is null!");
        }
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
            else if (AllEnemiesDead && hasSpawned)
            {
                Gizmos.color = Color.magenta; // All enemies dead
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

        // Show debug info when selected
        if (Application.isPlaying && showEnemyCountDebug)
        {
            // Draw lines to alive enemies
            Gizmos.color = Color.green;
            foreach (GameObject enemy in spawnedEnemies)
            {
                if (enemy != null)
                {
                    Gizmos.DrawLine(transform.position, enemy.transform.position);
                }
            }
        }
    }
}
