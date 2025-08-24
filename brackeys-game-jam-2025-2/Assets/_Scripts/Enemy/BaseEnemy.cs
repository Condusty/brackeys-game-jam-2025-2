using UnityEngine;

public enum EnemyStatType
{
    Speed,
    Strength,
    Health,
    CurrentHealth
}

public abstract class BaseEnemy : MonoBehaviour
{
    [SerializeField] EnemyStatSO enemyStat;

    // Runtime stats, copied from EnemyStatSO
    protected int speed;
    protected int strength;
    protected int health;
    protected int currentHealth;

    protected Rigidbody2D rb;

    private void Awake()
    {
        // Copy values from ScriptableObject to runtime fields
        speed = enemyStat.speed;
        strength = enemyStat.strength;
        health = enemyStat.health;

        currentHealth = health;

        rb = GetComponent<Rigidbody2D>();
    }

    public abstract void Attack();

    public abstract void Die();


    // Generic setter
    public void SetStat(EnemyStatType statType, int value)
    {
        switch (statType)
        {
            case EnemyStatType.Speed:
                speed = value;
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
        }
    }

    // Add flat value
    public void AddToStat(EnemyStatType statType, int value)
    {
        switch (statType)
        {
            case EnemyStatType.Speed:
                speed += value;
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
        }
    }

    // Subtract flat value
    public void SubtractFromStat(EnemyStatType statType, int value)
    {
        AddToStat(statType, -value);
    }

    // Modify by percent (positive to increase, negative to decrease)
    public void ModifyStatByPercent(EnemyStatType statType, float percent)
    {
        switch (statType)
        {
            case EnemyStatType.Speed:
                speed = Mathf.RoundToInt(speed * (1f + percent));
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
