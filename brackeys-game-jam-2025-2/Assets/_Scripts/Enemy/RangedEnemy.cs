using Unity.VisualScripting;
using UnityEngine;

public class RangedEnemy : BaseEnemy
{
    [SerializeField] GameObject projectilePrefab;

    private bool isMoving = false;
    private bool isInAttackRange = false;
    float attackCd = 2;

    private void Update()
    {
        Move();
        CheckAttackCd();
        Debug.Log(isInAttackRange);
        Debug.Log(attackCd);
    }

    private void CheckAttackCd()
    {
        attackCd -= Time.deltaTime;
        if(attackCd <= 0 && isInAttackRange)
        {
            isMoving = false;
            Attack();
            attackCd = 2;
        }
        if(attackCd < 1.5f)
        {
            isMoving = true;
        }
    }

    public override void Attack()
    {
        Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        Debug.Log("Ranged Enemy Attacks!");
    }

    public override void Die()
    {
        Debug.Log("Ranged Enemy Dies!");
    }

    private void Move()
    {
        if(GetDistanceToPlayer() <= aggroRange && GetDistanceToPlayer() > attackRange)
        {
            isInAttackRange = false;
        }
        else if(GetDistanceToPlayer() > aggroRange)
        {
        }
        else if(GetDistanceToPlayer() <= attackRange)
        {
            isInAttackRange = true;
        }

        if (isMoving)
        {
            transform.position = Vector2.MoveTowards(transform.position, player.transform.position, moveSpeed * Time.deltaTime);
        }

    }

    private float GetDistanceToPlayer()
    {
        return Vector2.Distance(transform.position, player.transform.position);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
