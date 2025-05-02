using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class UpgradeSelectionUI : MonoBehaviour
{
    [SerializeField] public GameObject panelRoot;
    [SerializeField] public GameObject background;
    [SerializeField] public GameObject screenTint;

    [SerializeField] private Button[] upgradeButtons;
    [SerializeField] private TextMeshProUGUI[] buttonTexts;

    private List<PlayerLeveling.TomeUpgrade> currentUpgrades;
    private PlayerLeveling playerLevelingRef;

    private void Start()
    {
        panelRoot.SetActive(false);
        background.SetActive(false);
        if (screenTint != null)
            screenTint.SetActive(false);
    }

    public void ShowUpgradeSelection(List<PlayerLeveling.TomeUpgrade> upgrades, PlayerLeveling levelingSystem)
{
    currentUpgrades = upgrades;
    playerLevelingRef = levelingSystem;

    Time.timeScale = 0f;

    panelRoot.SetActive(true);
    if (screenTint != null)
        screenTint.SetActive(true);
    background.SetActive(true);

    //set up upgrade buttons
    for (int i = 0; i < upgradeButtons.Length; i++)
    {
        if (i < upgrades.Count)
        {
            upgradeButtons[i].gameObject.SetActive(true);
            buttonTexts[i].text = upgrades[i].upgradeName;

            int buttonIndex = i;
            upgradeButtons[i].onClick.RemoveAllListeners();
            upgradeButtons[i].onClick.AddListener(() => SelectUpgrade(buttonIndex));
        }
        else
        {
            upgradeButtons[i].gameObject.SetActive(false);
        }
    }
}

    private void SelectUpgrade(int index)
    {
        //apply the upgrade
        playerLevelingRef.ApplyUpgrade(currentUpgrades[index]);

        panelRoot.SetActive(false);
        if (screenTint != null)
            screenTint.SetActive(false);
        background.SetActive(false);

        Time.timeScale = 1f;
    }
}