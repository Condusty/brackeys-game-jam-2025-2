using UnityEngine;

public class RangedEnemy : BaseEnemy
{
    private void Update()
    {
        rb.linearVelocity = new Vector2(-speed, rb.linearVelocity.y);
    }

    public override void Attack()
    {
        Debug.Log("Ranged Enemy Attacks!");
    }

    public override void Die()
    {
        Debug.Log("Ranged Enemy Dies!");
    }
}
