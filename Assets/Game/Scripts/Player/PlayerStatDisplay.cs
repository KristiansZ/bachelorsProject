using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerStatDisplay : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private TextMeshProUGUI moveSpeedText;
    [SerializeField] private TextMeshProUGUI armourText;
    [SerializeField] private TextMeshProUGUI regenAmountText;
    [SerializeField] private TextMeshProUGUI regenSpeedText;
    [SerializeField] private TextMeshProUGUI globalDamageText;
    public PlayerStatManager statManager;
    private KeyCode toggleStatsKey = KeyCode.Tab;
    
    private void Awake()
    {
        statManager = FindObjectOfType<PlayerStatManager>();
        
        if (statsPanel != null)
            statsPanel.SetActive(false);
    }
    
    private IEnumerator Start()
    {
        while (statManager == null)
        {
            statManager = FindObjectOfType<PlayerStatManager>();
            yield return null; //wait one frame if not found
        }
        
        UpdateStatsDisplay();
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(toggleStatsKey))
        {
            if (statsPanel != null)
                statsPanel.SetActive(!statsPanel.activeSelf);
                
            if (statsPanel.activeSelf)
                UpdateStatsDisplay();
        }
    }
    
    public void UpdateStatsDisplay()
    {
        if (moveSpeedText != null)
            moveSpeedText.text = $"Move Speed: {statManager.currentMoveSpeed:F1}";
            
        if (armourText != null)
            armourText.text = $"Armor: {statManager.armour:F0}";
            
        if (regenAmountText != null)
            regenAmountText.text = $"HP Regen: {statManager.lifeRegenerationAmount:F1}/cycle";
            
        if (regenSpeedText != null)
            regenSpeedText.text = $"Regen Cycle: {statManager.lifeRegenerationInterval:F1}s";
            
        if (globalDamageText != null) //global damage bonus from TomeController
        {
            float damageBonus = TomeController.Instance != null ? 
                TomeController.Instance.globalDamageBonus : 0.0f;
            globalDamageText.text = $"Damage Bonus: {damageBonus * 100:F0}%"; //*100 because multiplier is 0.1 for 10%
        }
    }
}