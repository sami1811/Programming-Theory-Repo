using UnityEngine;
using System.Text.RegularExpressions;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Warning Panel Settings")]
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private TMP_InputField playerNameField;
    
    
    private void Awake()
    {
        DisableWarning();
    }

    private void Start()
    {
        if(DataManager.Instance)
        {
            PlayerName = DataManager.Instance.playerName;
        }
        else
        {
#if UNITY_EDITOR
            Debug.Log("Data Manager is null.");
#endif
        }
    }

    private string PlayerName
    {
        get => playerNameField.text;
        set => playerNameField.text = value;
    }

    private static bool HasInvalidName(string input)
    {
        var hasNumber = Regex.IsMatch(input, @"\d");
        var hasCharacter = Regex.IsMatch(input, @"[^a-zA-Z\s]");

        return hasNumber || hasCharacter;
    }

    public void StartClicked()
    {
        if(PlayerName == string.Empty || HasInvalidName(PlayerName))
        {
            warningPanel?.SetActive(true);
            return;
        }

        DataManager.Instance.playerName = PlayerName;
        SceneController.LoadGame();
    }

    public void LeaderboardClicked()
    {
        SceneController.LoadLeaderboard();
    }

    public void ExitClicked()
    {
        SceneController.QuitGame();
    }
    
    public void DisableWarning()
    {
        if (warningPanel)
        {
            warningPanel.SetActive(false);
        }
        else
        {
#if UNITY_EDITOR
            Debug.Log("Warning Panel is null.");
#endif
        }
    }
}
