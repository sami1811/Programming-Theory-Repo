using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static class SceneNames
    {
        public const string menuScene = "MainMenu";
        public const string gameScene = "GameScene";
        public const string leaderboardScene = "LeaderboardScene";
    }

    public static SceneController instance;

    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void LoadMainMenu()
    {
        SceneManager.LoadScene(SceneNames.menuScene);
    }

    public static void LoadGame()
    {
        SceneManager.LoadScene(SceneNames.gameScene);
    }

    public static void LoadLeaderboard()
    {
        SceneManager.LoadScene(SceneNames.leaderboardScene);
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
