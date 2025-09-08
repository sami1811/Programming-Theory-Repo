using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private TMP_InputField playerNameField;

    private void Awake()
    {
        DefiningComponents();

        if(warningPanel.activeInHierarchy != false)
        {
            warningPanel.SetActive(false);
        }

        playerName = DataDelivery.Instance.playerName;
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
        if(playerName == string.Empty)
        {
            warningPanel.SetActive(true);
            return;
        }

        if(HasInvalidName(playerName))
        {
            warningPanel.SetActive(true);
            return;
        }

        DataDelivery.Instance.playerName = playerName;
        SceneManager.LoadScene(1);
    }

    public void LeaderboardClicked()
    {
        SceneManager.LoadScene(2);
    }

    public void ExitClicked()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }

    public void BackToMenuClicked()
    {
        SceneManager.LoadScene(0);
    }

    public void CloseWarning()
    {
        warningPanel?.SetActive(false);
    }

    private void DefiningComponents()
    {
        if (warningPanel == null)
        {
            warningPanel = GameObject.Find("Canvas").transform.Find("WarningPanel").gameObject;
        }

        if(playerNameField == null)
        {
            playerNameField = GameObject.Find("Canvas").GetComponentInChildren<TMP_InputField>();
        }
    }
}
