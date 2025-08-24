using UnityEngine;

[CreateAssetMenu(fileName = "New Stat", menuName = "Stats/PlayerStat")]
public class PlayerStatSO : ScriptableObject
{
    public float strength;
    public float health;
    public float moveSpeed;
    public float attackSpeed;
}