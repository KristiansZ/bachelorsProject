using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class DungeonSelector : MonoBehaviour
{
    public DungeonOption[] dungeonOptions;
    public GameObject menuUI;
    public GameObject background;
    public TMP_Text[] optionTexts;
    public TeleportingPortal portal;

    private Transform player;
    private bool menuOpen = false;
    private bool hasChosenDungeon = false;
    private bool playerInRange = false;
    private int[] selectedIndices = new int[3];

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        menuUI.SetActive(false);
        background.SetActive(false);
        SelectRandomDungeons();
        SetupMenu();
    }
    
    void Update()
    {
        if (playerInRange && !hasChosenDungeon && Input.GetKeyDown(KeyCode.E) && !menuOpen)
        {
            OpenMenu();
        }
    }

    void SelectRandomDungeons()
    {
        //boss dungeon selection
        if (DungeonProgressManager.Instance.IsBossDungeonAvailable())
        {
            for (int i = 0; i < dungeonOptions.Length; i++)
            {
                if (dungeonOptions[i].isBossDungeon)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        selectedIndices[j] = i;
                    }
                    return;
                }
            }
            Debug.LogWarning("No boss dungeon found in dungeon options!");
            return;
        }

        //normal dungeon selection
        if (dungeonOptions.Length <= 3)
        {
            for (int i = 0; i < dungeonOptions.Length; i++)
            {
                selectedIndices[i] = i;
            }
            return;
        }

        List<int> availableIndices = new List<int>();
        for (int i = 0; i < dungeonOptions.Length; i++)
        {
            //exclude boss dungeons from regular selection
            if (!dungeonOptions[i].isBossDungeon)
            {
                availableIndices.Add(i);
            }
        }

        for (int i = 0; i < 3; i++)
        {
            int randomIndex = Random.Range(0, availableIndices.Count);
            selectedIndices[i] = availableIndices[randomIndex];
            availableIndices.RemoveAt(randomIndex);
        }
    }

    public void SetPlayerInRange(bool inRange)
    {
        playerInRange = inRange;
        if (!inRange && menuOpen) CloseMenu();
    }

    void SetupMenu()
    {
        foreach (var optionText in optionTexts)
        {
            optionText.text = "Dungeon Option";
        }

        //boss dungeons
        if (DungeonProgressManager.Instance.IsBossDungeonAvailable())
        {
            for (int i = 0; i < optionTexts.Length; i++)
            {
                if (selectedIndices[i] < dungeonOptions.Length)
                {
                    DungeonOption option = dungeonOptions[selectedIndices[i]];
                    optionTexts[i].text = $"{option.dungeonName}\nBOSS DUNGEON";
                }
            }
            return;
        }

        //normal dungeons
        for (int i = 0; i < optionTexts.Length; i++)
        {
            if (i < 3 && i < selectedIndices.Length && selectedIndices[i] < dungeonOptions.Length)
            {
                DungeonOption option = dungeonOptions[selectedIndices[i]];
                optionTexts[i].text = $"{option.dungeonName}\nUpgrade: {option.upgradeName}";
            }
        }
    }

    public void ChooseDungeon(int buttonIndex)
    {
        if (buttonIndex < 0 || buttonIndex >= selectedIndices.Length || hasChosenDungeon) return;
        
        int dungeonIndex = selectedIndices[buttonIndex];
        if (dungeonIndex >= dungeonOptions.Length) return;

        DungeonOption selected = dungeonOptions[dungeonIndex];

        DungeonLoader.Instance.currentDungeonData = new DungeonData
        {
            dungeonName = selected.dungeonName,
            upgradeName = selected.upgradeName,
            upgradeType = selected.upgradeType,
            upgradeValue = selected.upgradeValue,
            roomCount = Random.Range(selected.roomCountMin, selected.roomCountMax + 1)
        };

        hasChosenDungeon = true;
        CloseMenu();
        
        if (portal != null) portal.OnDungeonSelected();
    }

    public DungeonOption GetSelectedDungeon()
    {
        if (!hasChosenDungeon || DungeonLoader.Instance?.currentDungeonData == null)
            return null;
        
        foreach (var option in dungeonOptions)
        {
            if (option.dungeonName == DungeonLoader.Instance.currentDungeonData.dungeonName)
            {
                return option;
            }
        }
        return null;
    }

    void OpenMenu()
    {
        SelectRandomDungeons();
        SetupMenu();
        menuOpen = true;
        menuUI.SetActive(true);
        background.SetActive(true);
        Time.timeScale = 0f;
    }

    void CloseMenu()
    {
        menuOpen = false;
        menuUI.SetActive(false);
        background.SetActive(false);
        Time.timeScale = 1f;
    }
    
    public bool HasChosenDungeon() => hasChosenDungeon;
    
    public void ResetDungeonChoice()
    {
        hasChosenDungeon = false;
        if (DungeonLoader.Instance != null) DungeonLoader.Instance.currentDungeonData = null;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) SetPlayerInRange(true);
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) SetPlayerInRange(false);
    }
}