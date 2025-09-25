using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public string playerName;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        QualitySettings.vSyncCount = 0; // disable VSync so targetFrameRate applies

#if UNITY_EDITOR
        Application.targetFrameRate = -1; // uncapped in editor
#elif UNITY_STANDALONE
        Application.targetFrameRate = 120; // cap for PC
#elif UNITY_ANDROID || UNITY_IOS
        Application.targetFrameRate = 60; // mobile default
#else
        Application.targetFrameRate = 60; // fallback
#endif
    }
}
