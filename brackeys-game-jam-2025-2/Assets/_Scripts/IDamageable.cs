using UnityEngine;

public interface IDamageable
{
    float Health { get; set; }
    void Damage(float amount);
}
