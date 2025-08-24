using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Stat", menuName = "Stats/EnemyStat")]
public class EnemyStatSO : ScriptableObject
{
    public int speed;
    public int strength;
    public int health;
}
