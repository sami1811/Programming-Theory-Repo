using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Rules Panel Settings")]
    [SerializeField] private TMP_Text playerIntro;
    [SerializeField] private GameObject rulesPanel;
    [SerializeField] private GameObject menuButton;

    [Header("Upgrade Panel Settings")]
    [SerializeField] private GameObject upgradePanel;
    
    private UpgradeUIManager _upgradeManager;
    
    private void Awake()
    {
        if (!upgradePanel)
        {
#if UNITY_EDITOR
            Debug.LogError("Upgrade Panel is null!");
#endif
        }
        
        if(!rulesPanel.activeInHierarchy || menuButton.activeInHierarchy)
        {
            menuButton.SetActive(false);
            rulesPanel.SetActive(true);
        }

        _upgradeManager = upgradePanel?.GetComponent<UpgradeUIManager>(); 
        _upgradeManager?.upgradePercentage.gameObject.SetActive(false);
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
        rulesPanel?.SetActive(false);
        menuButton?.SetActive(true);
        _upgradeManager?.upgradePercentage.gameObject.SetActive(true);
    }
    
    public void EnableUpgradeSelection()
    {
        upgradePanel.SetActive(true);
    }
    
    public void DisableUpgradeSelection()
    {
        upgradePanel.SetActive(false);
    }
}
