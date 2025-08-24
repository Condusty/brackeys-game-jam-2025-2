using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SceneChanger : MonoBehaviour
{
    [Header("Scene Configuration")]
    [SerializeField] private List<string> levelScenes = new List<string>();
    [SerializeField] private string mainMenuScene = "MainMenu";
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;
    
    // Singleton pattern
    public static SceneChanger Instance { get; private set; }
    
    // Current run tracking
    private int levelsPlayedInRun = 0;
    
    // Properties
    public int LevelsPlayedInRun => levelsPlayedInRun;
    public int TotalLevels => levelScenes.Count;

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
        }
    }

    public void NextLevel()
    {
        if (levelScenes.Count == 0)
        {
            Debug.LogWarning("No level scenes configured!");
            return;
        }
        
        // Get random level scene
        string randomLevel = levelScenes[Random.Range(0, levelScenes.Count)];
        
        // Increment counter
        levelsPlayedInRun++;
        
        if (showDebugLog)
        {
            Debug.Log($"Loading next level: {randomLevel} (Level {levelsPlayedInRun} in this run)");
        }
        
        // Load the scene
        SceneManager.LoadScene(randomLevel);
    }

    public void LoadMainScreen()
    {
        // Reset run counter when returning to main menu
        levelsPlayedInRun = 0;
        
        if (showDebugLog)
        {
            Debug.Log($"Loading main screen: {mainMenuScene}");
        }
        
        SceneManager.LoadScene(mainMenuScene);
    }
}
