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

        if (distance <= GetStat("AggroRange") && canSeePlayer)
        {
            isAggro = true;
            aggroTimer = aggroLossTime;
            lastKnownPlayerPosition = player.transform.position;
        }

        if (isAggro && !canSeePlayer)
        {
            aggroTimer -= Time.deltaTime;
            if (aggroTimer <= 0)
            {
                isAggro = false;
            }
        }

        if (isAggro && canSeePlayer)
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
        // Basis Implementation - direkte Bewegung
        return (targetPosition - (Vector2)transform.position).normalized;
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
    }
}
