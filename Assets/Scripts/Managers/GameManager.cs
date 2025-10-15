using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Over Settings")]
    [SerializeField] private GameObject cubeObject;
    
    private HealthSystem _cubeHealth;
    private bool _isGameOver;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!cubeObject)
        {
            cubeObject = GameObject.Find("Cube");

            if (!cubeObject)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[Game Manager] Assign Cube in the inspector");
#endif
            }
        }
        
        _isGameOver = false;
        _cubeHealth =  cubeObject?.GetComponent<HealthSystem>();
    }

    public void GameOver()
    {
        
    }
}
