using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneNames
{
    public const string MenuScene = "MainMenu";
    public const string GameScene = "GameScene";
    public const string LeaderboardScene = "LeaderboardScene";
}

public class SceneController : MonoBehaviour
{
    public static SceneController Instance  { get; private set; }
    
    private void Awake()
    {
        if(Instance&& Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void LoadMainMenu()
    {
        SceneManager.LoadScene(SceneNames.MenuScene);
    }

    public static void LoadGame()
    {
        SceneManager.LoadScene(SceneNames.GameScene);
    }

    public static void LoadLeaderboard()
    {
        SceneManager.LoadScene(SceneNames.LeaderboardScene);
    }

    public static void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }
}
