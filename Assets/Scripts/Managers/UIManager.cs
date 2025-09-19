using UnityEngine;
using System.Text.RegularExpressions;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private TMP_InputField playerNameField;

    private void Awake()
    {
        DisableWarning();
    }

    private void Start()
    {
        if(DataManager.Instance != null)
        {
            playerName = DataManager.Instance.playerName;
        }
        else
        {
            Debug.Log("Data Manager is null.");
        }
    }

    public string playerName
    {
        get
        {
            return playerNameField.text;
        }
        private set
        {
            playerNameField.text = value;
        }
    }

    private bool HasInvalidName(string input)
    {
        bool hasNumber = Regex.IsMatch(input, @"\d");
        bool hasCharacter = Regex.IsMatch(input, @"[^a-zA-Z\s]");

        return hasNumber || hasCharacter;
    }

    public void StartClicked()
    {
        if(playerName == string.Empty || HasInvalidName(playerName))
        {
            warningPanel.SetActive(true);
            return;
        }

        DataManager.Instance.playerName = playerName;
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
        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }

        else
            Debug.Log("Warning Panel is null.");
    }
}
