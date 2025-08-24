using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class EnemyStat
{
    public string name;
    public float value;
    
    public EnemyStat(string name, float value)
    {
        this.name = name;
        this.value = value;
    }
}

public abstract class BaseEnemy : MonoBehaviour, IDamageable
{
    [SerializeField] protected EnemyStatSO enemyStat;
    [SerializeField] protected float aggroLossTime = 5f;
    [SerializeField] protected string wallTag = "Obstacle";
    [SerializeField] protected float raycastDistance = 1f;
    [SerializeField] protected int raycastCount = 5;
    [SerializeField] protected float wallAvoidanceForce = 2f;

    public float Health { get => GetStat("Health"); set => SetStat("Health", value); }

    // Dictionary für erweiterbares Stat-System
    protected Dictionary<string, float> stats = new Dictionary<string, float>();
    
    // Common enemy state
    protected bool isMoving = false;
    protected bool isInAttackRange = false;
    protected bool isAggro = false;
    protected float attackCd = 0f;
    protected float aggroTimer = 0f;
    protected Vector2 lastKnownPlayerPosition;

    // Pathfinding
    protected Vector2 stuckPosition = Vector2.zero;
    protected float stuckTimer = 0f;
    protected Vector2 wallAvoidanceDirection = Vector2.zero;
    protected float wallAvoidanceTimer = 0f;

    protected Rigidbody2D rb;
    protected GameObject player;

    protected virtual void Awake()
    {
        InitializeStats();
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindWithTag("Player");
        attackCd = GetAttackCooldown();
    }

    protected virtual void Update()
    {
        UpdateAggro();
        UpdateMovement();
        CheckAttackCd();
    }

    protected virtual void InitializeStats()
    {
        // Basis Stats initialisieren
        stats["MoveSpeed"] = enemyStat.moveSpeed;
        stats["Strength"] = enemyStat.strength;
        stats["Health"] = enemyStat.health;
        stats["AttackSpeed"] = enemyStat.attackSpeed;
        stats["AggroRange"] = enemyStat.aggroRange;
        stats["AttackRange"] = enemyStat.attackRange;
        stats["CurrentHealth"] = enemyStat.health;
        
        // Weitere Stats können hier oder in Child-Klassen hinzugefügt werden
        InitializeCustomStats();
    }

    protected virtual void InitializeCustomStats()
    {
        // Override in child classes für spezifische Stats
    }

    // Erweitertes Stat-System
    public float GetStat(string statName)
    {
        return stats.ContainsKey(statName) ? stats[statName] : 0f;
    }

    public void SetStat(string statName, float value)
    {
        stats[statName] = value;
    }

    public void ModifyStatFlat(string statName, float value)
    {
        if (stats.ContainsKey(statName))
            stats[statName] += value;
    }

    public void ModifyStatPercent(string statName, float percent)
    {
        if (stats.ContainsKey(statName))
            stats[statName] *= (1f + percent);
    }

    public void AddStat(string statName, float value)
    {
        stats[statName] = value;
    }

    protected virtual void UpdateAggro()
    {
        float distance = GetDistanceToPlayer();
        bool canSeePlayer = CanSeePlayer();
        bool isInAggroRange = distance <= GetStat("AggroRange");

        // Gain aggro when player is in range and visible
        if (isInAggroRange && canSeePlayer)
        {
            isAggro = true;
            aggroTimer = aggroLossTime;
            lastKnownPlayerPosition = player.transform.position;
        }

        // Lose aggro when can't see player OR when outside aggro range
        if (isAggro && (!canSeePlayer || !isInAggroRange))
        {
            aggroTimer -= Time.deltaTime;
            if (aggroTimer <= 0)
            {
                isAggro = false;
            }
        }

        // Reset timer when player is visible and in range
        if (isAggro && canSeePlayer && isInAggroRange)
        {
            aggroTimer = aggroLossTime;
            lastKnownPlayerPosition = player.transform.position;
        }
    }

    protected virtual bool CanSeePlayer()
    {
        Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, directionToPlayer, distanceToPlayer);
        
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.gameObject == gameObject || hit.collider.isTrigger)
                continue;
            
            if (hit.collider.CompareTag(wallTag))
                return false;
            
            if (hit.collider.CompareTag("Player"))
                return true;
        }
        
        return false;
    }

    protected virtual void UpdateMovement()
    {
        if (!isAggro)
        {
            isMoving = false;
            isInAttackRange = false;
            return;
        }

        float distance = GetDistanceToPlayer();
        UpdateMovementState(distance);

        if (isMoving)
        {
            Vector2 targetPosition = CanSeePlayer() ? player.transform.position : lastKnownPlayerPosition;
            Vector2 moveDirection = GetMoveDirection(targetPosition);
            
            // Stuck detection
            if (Vector2.Distance(transform.position, stuckPosition) < 0.1f)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > 1f) // Nach 1 Sekunde als "stuck" betrachten
                {
                    wallAvoidanceDirection = GetWallAvoidanceDirection();
                    wallAvoidanceTimer = 2f; // 2 Sekunden Wandumgehung
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckPosition = transform.position;
                stuckTimer = 0f;
            }

            // Wall avoidance override
            if (wallAvoidanceTimer > 0)
            {
                wallAvoidanceTimer -= Time.deltaTime;
                moveDirection = wallAvoidanceDirection;
            }

            transform.position += (Vector3)moveDirection * GetStat("MoveSpeed") * Time.deltaTime;
        }
    }

    protected virtual void UpdateMovementState(float distance)
    {
        float attackRange = GetStat("AttackRange");
        
        if (distance > attackRange)
        {
            isMoving = true;
            isInAttackRange = false;
        }
        else
        {
            isMoving = false;
            isInAttackRange = true;
        }
    }

    protected virtual Vector2 GetMoveDirection(Vector2 targetPosition)
    {
        Vector2 directDirection = (targetPosition - (Vector2)transform.position).normalized;
        
        // Intelligente Wandumgehung mit mehreren Raycasts
        Vector2 finalDirection = GetSmartPathDirection(directDirection, targetPosition);
        
        return finalDirection;
    }

    protected virtual Vector2 GetSmartPathDirection(Vector2 desiredDirection, Vector2 targetPosition)
    {
        // Prüfe direkte Route
        if (!IsDirectionBlocked(desiredDirection, raycastDistance))
        {
            return desiredDirection;
        }

        // Multi-Raycast für beste Route
        float bestAngle = 0f;
        float maxDistance = 0f;
        bool foundPath = false;

        // Teste verschiedene Winkel (-90° bis +90°)
        for (int i = 0; i < raycastCount; i++)
        {
            float angle = Mathf.Lerp(-90f, 90f, (float)i / (raycastCount - 1));
            Vector2 testDirection = RotateVector(desiredDirection, angle);
            
            RaycastHit2D hit = Physics2D.Raycast(transform.position, testDirection, raycastDistance);
            
            float hitDistance = hit.collider != null && hit.collider.CompareTag(wallTag) ? 
                              hit.distance : raycastDistance;

            // Bevorzuge Richtungen, die näher zum Ziel führen
            float directionScore = Vector2.Dot(testDirection, desiredDirection);
            float totalScore = hitDistance + directionScore * 0.5f;

            if (totalScore > maxDistance)
            {
                maxDistance = totalScore;
                bestAngle = angle;
                foundPath = true;
            }
        }

        if (foundPath)
        {
            return RotateVector(desiredDirection, bestAngle);
        }

        // Fallback: Bewege dich von der nächsten Wand weg
        return GetWallAvoidanceDirection();
    }

    protected virtual Vector2 GetWallAvoidanceDirection()
    {
        Vector2 avoidanceDirection = Vector2.zero;
        
        // Prüfe alle 8 Richtungen und finde die freiste
        Vector2[] directions = {
            Vector2.up, Vector2.down, Vector2.left, Vector2.right,
            new Vector2(1, 1).normalized, new Vector2(-1, 1).normalized,
            new Vector2(1, -1).normalized, new Vector2(-1, -1).normalized
        };

        float maxDistance = 0f;
        
        foreach (Vector2 dir in directions)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, raycastDistance * 2f);
            float distance = hit.collider != null && hit.collider.CompareTag(wallTag) ? 
                           hit.distance : raycastDistance * 2f;

            if (distance > maxDistance)
            {
                maxDistance = distance;
                avoidanceDirection = dir;
            }
        }

        return avoidanceDirection;
    }

    protected virtual bool IsDirectionBlocked(Vector2 direction, float distance)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance);
        return hit.collider != null && hit.collider.CompareTag(wallTag) && !hit.collider.isTrigger;
    }

    protected Vector2 RotateVector(Vector2 vector, float angleDegrees)
    {
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angleRadians);
        float sin = Mathf.Sin(angleRadians);
        
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    protected virtual void CheckAttackCd()
    {
        attackCd -= Time.deltaTime;
        if (attackCd <= 0 && isInAttackRange && CanSeePlayer())
        {
            Attack();
            attackCd = GetAttackCooldown();
        }
    }

    protected virtual float GetAttackCooldown()
    {
        // Umkehrung: Je höher AttackSpeed, desto niedriger die Cooldown
        return 1f / Mathf.Max(0.1f, GetStat("AttackSpeed"));
    }

    protected float GetDistanceToPlayer()
    {
        return Vector2.Distance(transform.position, player.transform.position);
    }

    public void Damage(float amount)
    {
        ModifyStatFlat("CurrentHealth", -amount);
        if (GetStat("CurrentHealth") <= 0)
        {
            Die();
        }
    }

    public abstract void Attack();
    public abstract void Die();

    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, GetStat("AggroRange"));
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, GetStat("AttackRange"));

        if (player != null)
        {
            Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, player.transform.position);
        }

        if (isAggro)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);
        }

        // Zeige Pathfinding Raycasts
        if (isMoving && Application.isPlaying)
        {
            Vector2 desiredDirection = (lastKnownPlayerPosition - (Vector2)transform.position).normalized;
            
            for (int i = 0; i < raycastCount; i++)
            {
                float angle = Mathf.Lerp(-90f, 90f, (float)i / (raycastCount - 1));
                Vector2 testDirection = RotateVector(desiredDirection, angle);
                
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, testDirection * raycastDistance);
            }
        }
    }
}
