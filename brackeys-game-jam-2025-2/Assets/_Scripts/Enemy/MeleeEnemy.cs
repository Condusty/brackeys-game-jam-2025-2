using UnityEngine;

public class MeleeEnemy : BaseEnemy
{
    protected override float GetAttackCooldown()
    {
        return 1.5f;
    }

    public override void Attack()
    {
        Debug.Log("Melee Enemy Attacks!");
        var playerDamageable = player.GetComponent<IDamageable>();
        if (playerDamageable != null)
        {
            playerDamageable.Damage(GetStat("Strength"));
        }
    }

    public override void Die()
    {
        Debug.Log("Melee Enemy Dies!");
        Destroy(gameObject);
    }
}
