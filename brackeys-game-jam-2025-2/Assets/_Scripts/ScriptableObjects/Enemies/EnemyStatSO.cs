using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Stat", menuName = "Stats/EnemyStat")]
public class EnemyStatSO : ScriptableObject
{
    public float moveSpeed;
    public float strength;
    public float health;
    public float attackSpeed;
    public float aggroRange;
    public float attackRange;
}
