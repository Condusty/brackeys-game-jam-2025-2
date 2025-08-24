using UnityEngine;

public enum EnemyStatType
{
    Speed,
    Strength,
    Health,
    CurrentHealth,
    AttackSpeed,
    AggroRange,
    AttackRange
}

public abstract class BaseEnemy : MonoBehaviour, IDamageable
{
    [SerializeField] EnemyStatSO enemyStat;

    public float Health { get => health; set => health = value; }

    // Runtime stats, copied from EnemyStatSO
    protected float moveSpeed;
    protected float strength;
    protected float health;
    protected float attackSpeed;
    protected float aggroRange;
    protected float attackRange;

    protected float currentHealth;

    protected Rigidbody2D rb;
    protected GameObject player;

    private void Awake()
    {
        // Copy values from ScriptableObject to runtime fields
        moveSpeed = enemyStat.moveSpeed;
        strength = enemyStat.strength;
        health = enemyStat.health;
        attackSpeed = enemyStat.attackSpeed;
        aggroRange = enemyStat.aggroRange;
        attackRange = enemyStat.attackRange;


        currentHealth = health;

        rb = GetComponent<Rigidbody2D>();

        player = GameObject.FindWithTag("Player");
    }

    public void Damage(float amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    public abstract void Attack();

    public abstract void Die();


    // Generic setter
    public void SetStat(EnemyStatType statType, int value)
    {
        switch (statType)
        {
            case EnemyStatType.Speed:
                moveSpeed = value;
                break;
            case EnemyStatType.Strength:
                strength = value;
                break;
            case EnemyStatType.Health:
                health = value;
                break;
            case EnemyStatType.CurrentHealth:
                currentHealth = value;
                break;
            case EnemyStatType.AttackSpeed:
                attackSpeed = value;
                break;
            case EnemyStatType.AggroRange:
                aggroRange = value;
                break;
            case EnemyStatType.AttackRange:
                attackRange = value;
                break;
        }
    }

    // Add flat value
    public void ModifyStatFlat(EnemyStatType statType, int value)
    {
        switch (statType)
        {
            case EnemyStatType.Speed:
                moveSpeed += value;
                break;
            case EnemyStatType.Strength:
                strength += value;
                break;
            case EnemyStatType.Health:
                health += value;
                break;
            case EnemyStatType.CurrentHealth:
                currentHealth += value;
                break;
            case EnemyStatType.AttackSpeed:
                attackSpeed += value;
                break;
            case EnemyStatType.AggroRange:
                aggroRange += value;
                break;
            case EnemyStatType.AttackRange:
                attackRange += value;
                break;
        }
    }

    // Modify by percent (positive to increase, negative to decrease)
    public void ModifyStatByPercent(EnemyStatType statType, float percent)
    {
        switch (statType)
        {
            case EnemyStatType.Speed:
                moveSpeed = Mathf.RoundToInt(moveSpeed * (1f + percent));
                break;
            case EnemyStatType.Strength:
                strength = Mathf.RoundToInt(strength * (1f + percent));
                break;
            case EnemyStatType.Health:
                health = Mathf.RoundToInt(health * (1f + percent));
                break;
            case EnemyStatType.CurrentHealth:
                currentHealth = Mathf.RoundToInt(currentHealth * (1f + percent));
                break;
            case EnemyStatType.AttackSpeed:
                attackSpeed = Mathf.RoundToInt(attackSpeed * (1f + percent));
                break;
            case EnemyStatType.AggroRange:
                aggroRange = Mathf.RoundToInt(aggroRange * (1f + percent));
                break;
            case EnemyStatType.AttackRange:
                attackRange = Mathf.RoundToInt(attackRange * (1f + percent));
                break;
        }
    }

    // Example: public methods to modify stats at runtime
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth < 0)
        {
            Debug.Log($"{gameObject.name} has been defeated!");
            Destroy(gameObject); // Or handle death in another way
        }
    }
}
