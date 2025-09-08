using UnityEditor.Search;
using UnityEngine;

public class DataDelivery : MonoBehaviour
{
    public static DataDelivery Instance;

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
    }
}
