using UnityEngine;
using System.Collections.Generic;

public class PlayerCtrl : MonoBehaviour, IPlayerStats
{
    [SerializeField] PlayerStatSO playerStat;
    Rigidbody2D rb;
    float speedX, speedY;
    
    // Dictionary für erweiterbares Stat-System (ähnlich wie BaseEnemy)
    private Dictionary<string, float> stats = new Dictionary<string, float>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        InitializeStats();
    }

    private void InitializeStats()
    {
        // Basis Stats aus ScriptableObject initialisieren
        stats["Strength"] = playerStat.strength;
        stats["Health"] = playerStat.health;
        stats["MoveSpeed"] = playerStat.moveSpeed;
        stats["AttackSpeed"] = playerStat.attackSpeed;
        stats["CurrentHealth"] = playerStat.health;
        
        Debug.Log($"Player stats initialized: Health={GetStat("Health")}, Strength={GetStat("Strength")}, MoveSpeed={GetStat("MoveSpeed")}, AttackSpeed={GetStat("AttackSpeed")}");
    }

    // Update is called once per frame
    void Update()
    {
        // Verwende das Stat-System für Bewegungsgeschwindigkeit
        float currentMoveSpeed = GetStat("MoveSpeed");
        speedX = Input.GetAxis("Horizontal") * currentMoveSpeed;
        speedY = Input.GetAxis("Vertical") * currentMoveSpeed;
        rb.linearVelocity = new Vector2(speedX, speedY);

        if(Input.GetKeyDown(KeyCode.M))
        {
            foreach( var enemy in FindObjectsOfType<BaseEnemy>() )
            {
                enemy.Die();
            }
        }
        
        // Test: UpgradeMenu direkt öffnen
        if(Input.GetKeyDown(KeyCode.U))
        {
            Debug.Log("Manual upgrade menu test!");
            if(UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.ShowUpgradeMenu();
            }
            else
            {
                Debug.LogError("UpgradeManager.Instance is null!");
            }
        }
        
        // Debug: Stats anzeigen
        if (Input.GetKeyDown(KeyCode.P))
        {
            ShowCurrentStats();
        }
    }

    // IPlayerStats Interface Implementation
    public float GetStat(string statName)
    {
        return stats.ContainsKey(statName) ? stats[statName] : 0f;
    }

    public void SetStat(string statName, float value)
    {
        stats[statName] = value;
        Debug.Log($"Player stat changed: {statName} = {value}");
    }

    public void ModifyStatFlat(string statName, float value)
    {
        if (stats.ContainsKey(statName))
        {
            float oldValue = stats[statName];
            stats[statName] += value;
            Debug.Log($"Player stat modified (flat): {statName} {oldValue} -> {stats[statName]} (+{value})");
        }
        else
        {
            Debug.LogWarning($"Trying to modify unknown stat: {statName}");
        }
    }

    public void ModifyStatPercent(string statName, float percent)
    {
        if (stats.ContainsKey(statName))
        {
            float oldValue = stats[statName];
            stats[statName] *= (1f + percent / 100f);
            Debug.Log($"Player stat modified (percent): {statName} {oldValue} -> {stats[statName]} (+{percent}%)");
        }
        else
        {
            Debug.LogWarning($"Trying to modify unknown stat: {statName}");
        }
    }

    public void AddStat(string statName, float value)
    {
        stats[statName] = value;
        Debug.Log($"Player stat added: {statName} = {value}");
    }

    // Debug-Methode um aktuelle Stats anzuzeigen
    [ContextMenu("Show Current Stats")]
    public void ShowCurrentStats()
    {
        Debug.Log("=== PLAYER STATS ===");
        foreach (var stat in stats)
        {
            Debug.Log($"{stat.Key}: {stat.Value}");
        }
        Debug.Log("==================");
    }

    // Für IDamageable falls später benötigt
    public void Damage(float amount)
    {
        ModifyStatFlat("CurrentHealth", -amount);
        Debug.Log($"Player took {amount} damage. Current health: {GetStat("CurrentHealth")}");
        
        if (GetStat("CurrentHealth") <= 0)
        {
            Debug.Log("Player died!");
            // Hier könnte Game Over Logik stehen
        }
    }
}
