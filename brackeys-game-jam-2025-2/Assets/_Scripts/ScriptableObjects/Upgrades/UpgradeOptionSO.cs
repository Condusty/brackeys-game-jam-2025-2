using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Upgrade", menuName = "Upgrades/UpgradeOption")]
public class UpgradeOptionSO : ScriptableObject
{
    [Header("Upgrade Info")]
    public string upgradeName;
    [TextArea(3, 5)]
    public string description;
    public Sprite upgradeIcon;
    
    [Header("Stat Modifications")]
    public List<StatModification> statModifications = new List<StatModification>();
    
    [Header("Display Settings")]
    public Color upgradeColor = Color.white;
    public bool isRare = false;
    
    [Header("Rarity Settings")]
    [Range(0f, 100f)]
    [Tooltip("Chance for this upgrade to appear (0-100%). Higher = more common")]
    public float spawnWeight = 50f;
    
    [Header("Prerequisites")]
    [Tooltip("Upgrades that must be taken before this one becomes available")]
    public List<UpgradeOptionSO> requiredUpgrades = new List<UpgradeOptionSO>();
    
    [Header("Exclusions")]
    [Tooltip("Upgrades that cannot be taken together with this one")]
    public List<UpgradeOptionSO> exclusiveUpgrades = new List<UpgradeOptionSO>();
    
    /// <summary>
    /// Converts this ScriptableObject to the UpgradeOption class used by the UpgradeManager
    /// </summary>
    public UpgradeOption ToUpgradeOption()
    {
        UpgradeOption option = new UpgradeOption();
        option.upgradeName = upgradeName;
        option.description = description;
        option.upgradeIcon = upgradeIcon;
        option.statModifications = new List<StatModification>(statModifications);
        option.upgradeColor = upgradeColor;
        option.isRare = isRare;
        
        return option;
    }
    
    /// <summary>
    /// Validates the upgrade configuration and shows warnings in the console
    /// </summary>
    [ContextMenu("Validate Upgrade")]
    public void ValidateUpgrade()
    {
        bool isValid = true;
        
        if (string.IsNullOrEmpty(upgradeName))
        {
            Debug.LogWarning($"Upgrade '{name}': Upgrade name is empty!", this);
            isValid = false;
        }
        
        if (string.IsNullOrEmpty(description))
        {
            Debug.LogWarning($"Upgrade '{upgradeName}': Description is empty!", this);
            isValid = false;
        }
        
        if (statModifications.Count == 0)
        {
            Debug.LogWarning($"Upgrade '{upgradeName}': No stat modifications defined!", this);
            isValid = false;
        }
        
        foreach (var mod in statModifications)
        {
            if (string.IsNullOrEmpty(mod.statName))
            {
                Debug.LogWarning($"Upgrade '{upgradeName}': Stat modification has empty stat name!", this);
                isValid = false;
            }
            
            if (mod.value == 0f)
            {
                Debug.LogWarning($"Upgrade '{upgradeName}': Stat modification for '{mod.statName}' has value of 0!", this);
            }
        }
        
        // Check for circular dependencies in prerequisites
        if (HasCircularDependency())
        {
            Debug.LogError($"Upgrade '{upgradeName}': Circular dependency detected in prerequisites!", this);
            isValid = false;
        }
        
        if (isValid)
        {
            Debug.Log($"Upgrade '{upgradeName}': Validation passed!", this);
        }
    }
    
    private bool HasCircularDependency()
    {
        HashSet<UpgradeOptionSO> visited = new HashSet<UpgradeOptionSO>();
        return CheckCircularDependency(this, visited);
    }
    
    private bool CheckCircularDependency(UpgradeOptionSO upgrade, HashSet<UpgradeOptionSO> visited)
    {
        if (visited.Contains(upgrade))
            return true;
            
        visited.Add(upgrade);
        
        foreach (var prerequisite in upgrade.requiredUpgrades)
        {
            if (prerequisite != null && CheckCircularDependency(prerequisite, visited))
                return true;
        }
        
        visited.Remove(upgrade);
        return false;
    }
}