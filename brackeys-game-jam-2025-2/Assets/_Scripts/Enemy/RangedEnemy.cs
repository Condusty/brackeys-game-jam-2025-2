using UnityEngine;

public class RangedEnemy : BaseEnemy
{
    [SerializeField] private GameObject projectilePrefab;

    protected override void InitializeCustomStats()
    {
        AddStat("CanAvoidWalls", 1f);
    }

    protected override float GetAttackCooldown()
    {
        return 2f;
    }

    protected override Vector2 GetMoveDirection(Vector2 targetPosition)
    {
        if (GetStat("CanAvoidWalls") > 0)
        {
            return GetMoveDirectionWithWallAvoidance(targetPosition);
        }
        
        return base.GetMoveDirection(targetPosition);
    }

    private Vector2 GetMoveDirectionWithWallAvoidance(Vector2 targetPosition)
    {
        Vector2 directDirection = (targetPosition - (Vector2)transform.position).normalized;

        bool directBlocked = IsDirectionBlocked(directDirection, 0.5f);
        
        if (!directBlocked)
        {
            return directDirection;
        }

        Vector2 rightDirection = new Vector2(-directDirection.y, directDirection.x);
        Vector2 leftDirection = new Vector2(directDirection.y, -directDirection.x);

        bool rightBlocked = IsDirectionBlocked(rightDirection, 0.5f);
        bool leftBlocked = IsDirectionBlocked(leftDirection, 0.5f);

        if (!rightBlocked && !leftBlocked)
        {
            return Random.value > 0.5f ? rightDirection : leftDirection;
        }
        else if (!rightBlocked)
        {
            return rightDirection;
        }
        else if (!leftBlocked)
        {
            return leftDirection;
        }

        return -directDirection;
    }

    private bool IsDirectionBlocked(Vector2 direction, float distance)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance);
        return hit.collider != null && hit.collider.CompareTag(wallTag) && !hit.collider.isTrigger;
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
