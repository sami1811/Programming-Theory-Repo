using TMPro;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    [SerializeField] private TMP_Text playerIntro;
    [SerializeField] private GameObject rulesPanel;
    [SerializeField] private GameObject menuButton;

    private void Awake()
    {
        if(!rulesPanel.activeInHierarchy && menuButton.activeInHierarchy)
        {
            menuButton.SetActive(false);
            rulesPanel.SetActive(true);
        }
    }

    private void Start()
    {
        //playerIntro.text = "Hi " + DataManager.Instance.playerName; 
    }

    public void MenuClicked()
    {
        SceneController.LoadMainMenu();
    }

    public void DisableRulesPanel()
    {
        rulesPanel.SetActive(false);
        menuButton.SetActive(true);
    }
}
