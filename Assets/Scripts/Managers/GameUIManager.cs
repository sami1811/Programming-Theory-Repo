using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class GameUIManager : MonoBehaviour
{
    [FormerlySerializedAs("playerStats")]
    [Header("Stats References")]
    [SerializeField] private TMP_Text moveSpeedText;
    [SerializeField] private TMP_Text fireRateText;
    [SerializeField] private TMP_Text damageText;
    
    [Header("Rules Panel Settings")]
    [SerializeField] private TMP_Text playerIntro;
    [SerializeField] private GameObject rulesPanel;
    [SerializeField] private GameObject menuButton;
    
    [Header("Ability Panel Settings")]
    [SerializeField] private GameObject abilityPanel;
    
    private void Awake()
    {
        if (!abilityPanel)
        {
            ErrorLogger("Ability panel is not assigned!");
        }
        
        if(!rulesPanel.activeInHierarchy || menuButton.activeInHierarchy)
        {
            menuButton.SetActive(false);
            rulesPanel.SetActive(true);
        }

        if (abilityPanel.activeInHierarchy)
        {
            abilityPanel.SetActive(false);
        }
    }

    private void Start()
    {
        //playerIntro.text = "Hi " + DataManager.Instance.playerName; 
    }

    private void Update()
    {
        UpgradeUpdate();
    }

    private void UpgradeUpdate()
    {
        if(!StatsSystem.Instance)
            return;

        if (moveSpeedText || fireRateText || damageText)
        {
            moveSpeedText.text = $"Speed:{StatsSystem.Instance.MovementSpeed}/10";
            fireRateText.text = $"FireRate:{StatsSystem.Instance.FireRate}/3";
            damageText.text = $"Damage:{StatsSystem.Instance.Damage}/25";
        }
    }

    public void MenuClicked()
    {
        SceneController.LoadMainMenu();
    }

    public void DisableRulesPanel()
    {
        rulesPanel?.SetActive(false);
        menuButton?.SetActive(true);
    }
    
    public void EnableAbilityPanel()
    {
        abilityPanel.SetActive(true);
    }
    
    public void DisableAbilityPanel()
    {
        abilityPanel.SetActive(false);
    }

    private void ErrorLogger(string message)
    {
#if UNITY_EDITOR
        Debug.LogError($"[GameUIManager] {message}");
#endif
    }
}
