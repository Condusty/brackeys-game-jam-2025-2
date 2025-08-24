using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Linq;

[Serializable]
public enum StatModificationType
{
    FlatAdditive,     // +5 Damage
    Percentage,       // +20% Damage  
    FlatSubtractive,  // -2 Health
    PercentageNegative // -10% Speed
}

[Serializable]
public class StatModification
{
    public string statName;
    public float value;
    public StatModificationType modificationType;
    
    public StatModification(string statName, float value, StatModificationType modificationType = StatModificationType.FlatAdditive)
    {
        this.statName = statName;
        this.value = value;
        this.modificationType = modificationType;
    }
}

[Serializable]
public class UpgradeOption
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
}

public interface IPlayerStats
{
    float GetStat(string statName);
    void SetStat(string statName, float value);
    void ModifyStatFlat(string statName, float value);
    void ModifyStatPercent(string statName, float percent);
    void AddStat(string statName, float value);
}

public class UpgradeManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas upgradeCanvas;
    [SerializeField] private Transform upgradeButtonParent;
    [SerializeField] private Button upgradeButtonPrefab;
    
    [Header("Upgrade Settings")]
    [SerializeField] private List<UpgradeOptionSO> availableUpgradesSO = new List<UpgradeOptionSO>();
    [SerializeField] private List<UpgradeOption> availableUpgrades = new List<UpgradeOption>(); // Legacy support
    [SerializeField] private int upgradeChoices = 3;
    [SerializeField] private bool allowDuplicateUpgrades = false;
    [SerializeField] private bool useWeightedSelection = true;
    
    [Header("Audio/Effects")]
    [SerializeField] private AudioSource upgradeSound;
    [SerializeField] private GameObject upgradeParticleEffect;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;
    
    // Singleton pattern
    public static UpgradeManager Instance { get; private set; }
    
    // Events
    public System.Action<UpgradeOption> OnUpgradeSelected;
    public System.Action OnUpgradeMenuOpened;
    public System.Action OnUpgradeMenuClosed;
    
    // Private variables
    private List<UpgradeOption> selectedUpgradeOptions = new List<UpgradeOption>();
    private List<UpgradeOptionSO> alreadyTakenUpgradesSO = new List<UpgradeOptionSO>();
    private List<UpgradeOption> alreadyTakenUpgrades = new List<UpgradeOption>(); // Legacy support
    private List<Button> currentUpgradeButtons = new List<Button>();
    private IPlayerStats playerStats;
    private bool isUpgradeMenuOpen = false;
    private float originalTimeScale;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Initialize
        if (upgradeCanvas != null)
            upgradeCanvas.gameObject.SetActive(false);
    }

    private void Start()
    {
        // Subscribe to all enemy spawners
        SubscribeToEnemySpawners();
        
        // Find player stats component
        FindPlayerStats();
        
        // Validate all ScriptableObject upgrades
        ValidateUpgrades();
        
        int totalUpgrades = availableUpgradesSO.Count + availableUpgrades.Count;
        if (showDebugLog)
        {
            Debug.Log($"UpgradeManager initialized with {totalUpgrades} available upgrades ({availableUpgradesSO.Count} ScriptableObjects, {availableUpgrades.Count} legacy)");
        }
    }

    private void ValidateUpgrades()
    {
        foreach (var upgradeSO in availableUpgradesSO)
        {
            if (upgradeSO != null)
            {
                upgradeSO.ValidateUpgrade();
            }
        }
    }

    private void SubscribeToEnemySpawners()
    {
        EnemySpawner[] spawners = FindObjectsOfType<EnemySpawner>();
        foreach (EnemySpawner spawner in spawners)
        {
            spawner.OnAllEnemiesDead += OnEnemiesDefeated;
        }
        
        if (showDebugLog)
        {
            Debug.Log($"Subscribed to {spawners.Length} enemy spawners");
        }
    }

    private void FindPlayerStats()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerStats = player.GetComponent<IPlayerStats>();
            if (playerStats == null && showDebugLog)
            {
                Debug.LogWarning("Player found but no IPlayerStats component detected. Make sure your player implements IPlayerStats interface.");
            }
        }
        else if (showDebugLog)
        {
            Debug.LogWarning("No player with 'Player' tag found!");
        }
    }

    private void OnEnemiesDefeated(EnemySpawner spawner)
    {
        if (showDebugLog)
        {
            Debug.Log($"All enemies defeated from spawner: {spawner.name}. Opening upgrade menu.");
        }
        
        ShowUpgradeMenu();
    }

    public void ShowUpgradeMenu()
    {
        if (isUpgradeMenuOpen)
            return;
            
        isUpgradeMenuOpen = true;
        
        // Pause game
        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        
        // Generate upgrade options
        GenerateUpgradeOptions();
        
        // Show UI
        if (upgradeCanvas != null)
        {
            upgradeCanvas.gameObject.SetActive(true);
        }
        
        OnUpgradeMenuOpened?.Invoke();
        
        if (showDebugLog)
        {
            Debug.Log("Upgrade menu opened. Game paused.");
        }
    }

    private void GenerateUpgradeOptions()
    {
        selectedUpgradeOptions.Clear();
        ClearCurrentButtons();
        
        // Get available upgrades from ScriptableObjects
        List<UpgradeOptionSO> availablePoolSO = GetAvailableUpgradesSO();
        
        // Get available upgrades from legacy list
        List<UpgradeOption> availablePoolLegacy = new List<UpgradeOption>(availableUpgrades);
        if (!allowDuplicateUpgrades)
        {
            availablePoolLegacy.RemoveAll(upgrade => alreadyTakenUpgrades.Contains(upgrade));
        }
        
        // Convert ScriptableObjects to UpgradeOptions and combine with legacy
        List<UpgradeOption> combinedPool = new List<UpgradeOption>();
        
        // Add ScriptableObject upgrades
        foreach (var upgradeSO in availablePoolSO)
        {
            combinedPool.Add(upgradeSO.ToUpgradeOption());
        }
        
        // Add legacy upgrades
        combinedPool.AddRange(availablePoolLegacy);
        
        // Select upgrades based on method
        if (useWeightedSelection && availablePoolSO.Count > 0)
        {
            selectedUpgradeOptions = SelectWeightedUpgrades(availablePoolSO, availablePoolLegacy);
        }
        else
        {
            selectedUpgradeOptions = SelectRandomUpgrades(combinedPool);
        }
        
        // Create UI buttons
        CreateUpgradeButtons();
    }

    private List<UpgradeOptionSO> GetAvailableUpgradesSO()
    {
        List<UpgradeOptionSO> availablePool = new List<UpgradeOptionSO>();
        
        foreach (var upgradeSO in availableUpgradesSO)
        {
            if (upgradeSO == null) continue;
            
            // Check if already taken (if duplicates not allowed)
            if (!allowDuplicateUpgrades && alreadyTakenUpgradesSO.Contains(upgradeSO))
                continue;
            
            // Check prerequisites
            if (!ArePrerequisitesMet(upgradeSO))
                continue;
            
            // Check exclusions
            if (HasExclusiveConflict(upgradeSO))
                continue;
            
            availablePool.Add(upgradeSO);
        }
        
        return availablePool;
    }

    private bool ArePrerequisitesMet(UpgradeOptionSO upgrade)
    {
        foreach (var prerequisite in upgrade.requiredUpgrades)
        {
            if (prerequisite != null && !alreadyTakenUpgradesSO.Contains(prerequisite))
                return false;
        }
        return true;
    }

    private bool HasExclusiveConflict(UpgradeOptionSO upgrade)
    {
        foreach (var exclusive in upgrade.exclusiveUpgrades)
        {
            if (exclusive != null && alreadyTakenUpgradesSO.Contains(exclusive))
                return true;
        }
        return false;
    }

    private List<UpgradeOption> SelectWeightedUpgrades(List<UpgradeOptionSO> availablePoolSO, List<UpgradeOption> availablePoolLegacy)
    {
        List<UpgradeOption> selected = new List<UpgradeOption>();
        List<UpgradeOptionSO> tempPool = new List<UpgradeOptionSO>(availablePoolSO);
        
        int choicesToGenerate = Mathf.Min(upgradeChoices, tempPool.Count + availablePoolLegacy.Count);
        
        for (int i = 0; i < choicesToGenerate; i++)
        {
            if (tempPool.Count == 0 && availablePoolLegacy.Count == 0)
                break;
            
            // Decide whether to pick from SO pool or legacy pool
            bool pickFromSO = tempPool.Count > 0 && (availablePoolLegacy.Count == 0 || UnityEngine.Random.value > 0.5f);
            
            if (pickFromSO)
            {
                UpgradeOptionSO selectedSO = SelectWeightedUpgrade(tempPool);
                selected.Add(selectedSO.ToUpgradeOption());
                
                if (!allowDuplicateUpgrades)
                    tempPool.Remove(selectedSO);
            }
            else if (availablePoolLegacy.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, availablePoolLegacy.Count);
                selected.Add(availablePoolLegacy[randomIndex]);
                
                if (!allowDuplicateUpgrades)
                    availablePoolLegacy.RemoveAt(randomIndex);
            }
        }
        
        return selected;
    }

    private UpgradeOptionSO SelectWeightedUpgrade(List<UpgradeOptionSO> pool)
    {
        float totalWeight = pool.Sum(upgrade => upgrade.spawnWeight);
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        
        float currentWeight = 0f;
        foreach (var upgrade in pool)
        {
            currentWeight += upgrade.spawnWeight;
            if (randomValue <= currentWeight)
                return upgrade;
        }
        
        return pool[pool.Count - 1]; // Fallback
    }

    private List<UpgradeOption> SelectRandomUpgrades(List<UpgradeOption> combinedPool)
    {
        List<UpgradeOption> selected = new List<UpgradeOption>();
        List<UpgradeOption> tempPool = new List<UpgradeOption>(combinedPool);
        
        int choicesToGenerate = Mathf.Min(upgradeChoices, tempPool.Count);
        
        for (int i = 0; i < choicesToGenerate; i++)
        {
            if (tempPool.Count == 0)
                break;
                
            int randomIndex = UnityEngine.Random.Range(0, tempPool.Count);
            selected.Add(tempPool[randomIndex]);
            
            if (!allowDuplicateUpgrades)
                tempPool.RemoveAt(randomIndex);
        }
        
        return selected;
    }

    private void CreateUpgradeButtons()
    {
        if (upgradeButtonPrefab == null || upgradeButtonParent == null)
        {
            Debug.LogError("Upgrade button prefab or parent not assigned!");
            return;
        }
        
        for (int i = 0; i < selectedUpgradeOptions.Count; i++)
        {
            UpgradeOption upgrade = selectedUpgradeOptions[i];
            Button upgradeButton = Instantiate(upgradeButtonPrefab, upgradeButtonParent);
            
            // Setup button
            SetupUpgradeButton(upgradeButton, upgrade, i);
            currentUpgradeButtons.Add(upgradeButton);
        }
    }

    private void SetupUpgradeButton(Button button, UpgradeOption upgrade, int index)
    {
        // Set button text and icon
        Text buttonText = button.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = $"{upgrade.upgradeName}\n{upgrade.description}";
            buttonText.color = upgrade.upgradeColor;
        }
        
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null && upgrade.upgradeIcon != null)
        {
            buttonImage.sprite = upgrade.upgradeIcon;
        }
        
        // Special styling for rare upgrades
        if (upgrade.isRare)
        {
            // Add glow effect or special border
            buttonImage.color = Color.yellow;
        }
        
        // Add click listener
        button.onClick.AddListener(() => SelectUpgrade(upgrade));
    }

    public void SelectUpgrade(UpgradeOption upgrade)
    {
        if (!isUpgradeMenuOpen)
            return;
            
        // Apply upgrade to player
        ApplyUpgradeToPlayer(upgrade);
        
        // Track taken upgrade
        if (!allowDuplicateUpgrades)
        {
            // Find corresponding ScriptableObject
            UpgradeOptionSO correspondingSO = availableUpgradesSO.FirstOrDefault(so => 
                so != null && so.upgradeName == upgrade.upgradeName);
            
            if (correspondingSO != null)
            {
                alreadyTakenUpgradesSO.Add(correspondingSO);
            }
            else
            {
                alreadyTakenUpgrades.Add(upgrade);
            }
        }
        
        // Play effects
        PlayUpgradeEffects();
        
        // Fire event
        OnUpgradeSelected?.Invoke(upgrade);
        
        // Close menu
        CloseUpgradeMenu();
        
        if (showDebugLog)
        {
            Debug.Log($"Upgrade selected: {upgrade.upgradeName}");
        }
    }

    private void ApplyUpgradeToPlayer(UpgradeOption upgrade)
    {
        if (playerStats == null)
        {
            Debug.LogError("Cannot apply upgrade: Player stats not found!");
            return;
        }
        
        foreach (StatModification mod in upgrade.statModifications)
        {
            ApplyStatModification(mod);
        }
    }

    private void ApplyStatModification(StatModification mod)
    {
        float currentValue = playerStats.GetStat(mod.statName);
        float newValue = currentValue;
        
        switch (mod.modificationType)
        {
            case StatModificationType.FlatAdditive:
                newValue = currentValue + mod.value;
                break;
                
            case StatModificationType.Percentage:
                newValue = currentValue * (1f + (mod.value / 100f));
                break;
                
            case StatModificationType.FlatSubtractive:
                newValue = currentValue - mod.value;
                break;
                
            case StatModificationType.PercentageNegative:
                newValue = currentValue * (1f - (mod.value / 100f));
                break;
        }
        
        // Ensure minimum values for certain stats
        if (mod.statName.ToLower().Contains("health") && newValue < 1f)
            newValue = 1f;
        
        playerStats.SetStat(mod.statName, newValue);
        
        if (showDebugLog)
        {
            Debug.Log($"Applied {mod.modificationType}: {mod.statName} {currentValue} -> {newValue} (modifier: {mod.value})");
        }
    }

    private void PlayUpgradeEffects()
    {
        // Play sound
        if (upgradeSound != null)
            upgradeSound.Play();
        
        // Spawn particle effect
        if (upgradeParticleEffect != null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                Instantiate(upgradeParticleEffect, player.transform.position, Quaternion.identity);
            }
        }
    }

    private void CloseUpgradeMenu()
    {
        isUpgradeMenuOpen = false;
        
        // Resume game
        Time.timeScale = originalTimeScale;
        
        // Hide UI
        if (upgradeCanvas != null)
            upgradeCanvas.gameObject.SetActive(false);
        
        // Clean up buttons
        ClearCurrentButtons();
        
        OnUpgradeMenuClosed?.Invoke();
        
        if (showDebugLog)
        {
            Debug.Log("Upgrade menu closed. Game resumed.");
        }
    }

    private void ClearCurrentButtons()
    {
        foreach (Button button in currentUpgradeButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }
        currentUpgradeButtons.Clear();
    }

    // Public utility methods
    public void AddAvailableUpgrade(UpgradeOptionSO upgrade)
    {
        if (upgrade != null && !availableUpgradesSO.Contains(upgrade))
        {
            availableUpgradesSO.Add(upgrade);
        }
    }

    public void AddAvailableUpgrade(UpgradeOption upgrade)
    {
        if (!availableUpgrades.Contains(upgrade))
        {
            availableUpgrades.Add(upgrade);
        }
    }

    public void RemoveAvailableUpgrade(UpgradeOptionSO upgrade)
    {
        availableUpgradesSO.Remove(upgrade);
    }

    public void RemoveAvailableUpgrade(UpgradeOption upgrade)
    {
        availableUpgrades.Remove(upgrade);
    }

    public void ResetTakenUpgrades()
    {
        alreadyTakenUpgradesSO.Clear();
        alreadyTakenUpgrades.Clear();
        if (showDebugLog)
        {
            Debug.Log("Reset all taken upgrades");
        }
    }

    // Manual trigger for testing
    [ContextMenu("Test: Show Upgrade Menu")]
    public void TestShowUpgradeMenu()
    {
        ShowUpgradeMenu();
    }

    [ContextMenu("Test: Close Upgrade Menu")]
    public void TestCloseUpgradeMenu()
    {
        CloseUpgradeMenu();
    }

    [ContextMenu("Test: Validate All Upgrades")]
    public void TestValidateUpgrades()
    {
        ValidateUpgrades();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        EnemySpawner[] spawners = FindObjectsOfType<EnemySpawner>();
        foreach (EnemySpawner spawner in spawners)
        {
            if (spawner != null)
                spawner.OnAllEnemiesDead -= OnEnemiesDefeated;
        }
    }
}
