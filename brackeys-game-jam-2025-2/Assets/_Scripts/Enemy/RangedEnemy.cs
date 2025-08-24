using UnityEngine;

public class RangedEnemy : BaseEnemy
{
    [SerializeField] private GameObject projectilePrefab;

    protected override float GetAttackCooldown()
    {
        return 2f;
    }

    // Override movement state to consider line of sight for ranged attacks
    protected override void UpdateMovementState(float distance)
    {
        float attackRange = GetStat("AttackRange");
        bool hasLineOfSight = CanSeePlayer();
        
        // Ranged enemies need both range AND line of sight to stop moving
        if (distance > attackRange || !hasLineOfSight)
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

    public override void Attack()
    {
        if (projectilePrefab != null)
        {
            Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        }
        Debug.Log("Ranged Enemy Attacks!");
    }

    public override void Die()
    {
        Debug.Log("Ranged Enemy Dies!");
        Destroy(gameObject);
    }
}
