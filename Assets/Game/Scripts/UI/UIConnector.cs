using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using System.Collections;
using System;

public class UIConnector : MonoBehaviour
{
    public static UIConnector Instance { get; private set; }
    
    public PlayerLeveling playerLeveling;
    public PlayerController playerController;
    public TomeController tomeController;
    public CinemachineCamera virtualCam;
    
    [Header("UI References")]
    private GameObject canvas;
    private PauseMenu pauseMenu;
    private Camera mainCamera;
    
    //public pausemenu for PlayerController to access
    public GameObject pauseMenuPanel { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    IEnumerator Start()
    {
        yield return new WaitUntil(() => PersistentObjectManager.Instance != null);
        yield return null; //wait one frame to make sure everything is initialized
        ConnectAllSystems();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConnectAllSystems();
    }

    void ConnectAllSystems()
    {
        canvas = GetPersistentObject("CanvasScreenSpace");
        if (canvas == null) return;

        ConnectCameraSystems();
        ConnectPlayerSystems();
        ConnectTomeSystems();
        ConnectUIElements();
    }

    #region Camera Systems
    
    void ConnectCameraSystems()
    {
        GameObject camObj = GetPersistentObject("MainCamera");
        if (camObj != null)
        {
            mainCamera = camObj.GetComponent<Camera>();
        }

        GameObject cinemachineObj = GetPersistentObject("CinemachineCamera");
        if (cinemachineObj != null)
        {
            virtualCam = cinemachineObj.GetComponent<CinemachineCamera>();
            
            if (!cinemachineObj.TryGetComponent<CinemachineBrain>(out _))
            {
                cinemachineObj.AddComponent<CinemachineBrain>();
            }
        }
    }
    
    #endregion

    #region Player Systems
    
    void ConnectPlayerSystems()
    {
        playerController = FindObjectOfType<PlayerController>();
        playerLeveling = FindObjectOfType<PlayerLeveling>();
        
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            //connect player to camera
            if (virtualCam != null)
            {
                virtualCam.Follow = player.transform;
                
                if (playerController != null)
                {
                    playerController.CameraTransform = virtualCam.transform;
                }
            }
            
            //connect player leveling systems
            if (playerLeveling != null)
            {
                ConnectPlayerLevelingUI();
                playerLeveling.playerStats = FindObjectOfType<PlayerStatManager>();
                playerLeveling.tomeController = tomeController;
            }
        }
    }
    
    void ConnectPlayerLevelingUI()
    {
        if (playerLeveling == null) return;
        
        playerLeveling.experienceText = GetUITextComponent("XPText");
        playerLeveling.experienceBar = GetUIComponent<RectTransform>("XPBarFill");
        playerLeveling.upgradeText = GetUITextComponent("LevelUpText");
        
        GameObject upgradePanel = FindInCanvas("UpgradePanel");
        if (upgradePanel != null)
        {
            playerLeveling.upgradeSelectionUI = upgradePanel.GetComponent<UpgradeSelectionUI>();
            
            //setup background
            Transform parentTransform = upgradePanel.transform.parent;
            GameObject background = parentTransform?.Find("Background")?.gameObject;
            if (background != null && playerLeveling.upgradeSelectionUI != null)
            {
                playerLeveling.upgradeSelectionUI.background = background;
            }
        }
    }
    
    #endregion

    #region Tome Systems
    
    void ConnectTomeSystems()
    {
        tomeController = FindObjectOfType<TomeController>();
        if (tomeController == null) return;
        
        //tome display
        tomeController.tomeDisplay = FindObjectOfType<TomeDisplay>();
        
        //tome inventory
        GameObject tomeMenu = FindInCanvas("TomeMenu");
        if (tomeMenu != null)
        {
            tomeController.tomeInventoryParent = tomeMenu.GetComponent<RectTransform>();
        }
        
        //tome assignment menu
        TomeAssignmentMenu assignmentMenu = FindObjectOfType<TomeAssignmentMenu>();
        tomeController.tomeAssignmentMenu = assignmentMenu;
        
        //player's tome inventory to assignment menu
        if (assignmentMenu != null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            GameObject tomeManagerObject = player?.transform.Find("TomeManager")?.gameObject;
            
            if (tomeManagerObject != null)
            {
                TomeInventory inventory = tomeManagerObject.GetComponent<TomeInventory>();
                if (inventory != null)
                {
                    assignmentMenu.tomeInventory = inventory;
                }
            }
        }
    }
    
    #endregion

    #region UI Elements
    
    void ConnectUIElements()
    {
        ConnectPortalUI();
        ConnectPauseMenuUI();
        ConnectDungeonSelectorUI();
    }
    
    void ConnectPortalUI()
    {
        TeleportingPortal portal = FindObjectOfType<TeleportingPortal>();
        if (portal == null) return;
        
        portal.enemyKillText = GetUITextComponent("EnemyKillText");
    }
    
    void ConnectPauseMenuUI()
    {
        pauseMenu = FindObjectOfType<PauseMenu>();
        if (pauseMenu == null) return;
        
        GameObject pausePanel = FindInCanvas("PauseMenuPanel");
        if (pausePanel == null) return;
        
        //reference to pause panel for public access (playerController)
        pauseMenuPanel = pausePanel;
        
        //UI elements
        Button resumeButton = GetUIComponent<Button>(pausePanel, "ResumeButton");
        Button quitButton = GetUIComponent<Button>(pausePanel, "QuitGameButton");
        
        //references for pause menu
        pauseMenu.SetPauseMenuPanel(pausePanel);
        if (resumeButton != null) pauseMenu.SetResumeButton(resumeButton);
        if (quitButton != null) pauseMenu.SetQuitButton(quitButton);
        
        pauseMenu.InitializeUI();
    }
    
    void ConnectDungeonSelectorUI()
    {
        DungeonSelector selector = FindObjectOfType<DungeonSelector>();
        TeleportingPortal portal = FindObjectOfType<TeleportingPortal>();
        
        if (selector == null || portal == null) return;
        
        selector.portal = portal;
        portal.dungeonSelector = selector;
        
        //UI elements
        GameObject menu = FindInCanvas("DungeonMenu");
        if (menu == null) return;
        
        selector.menuUI = menu;
        selector.optionTexts = menu.GetComponentsInChildren<TMP_Text>();
        
        Transform parentTransform = menu.transform.parent;
        if (parentTransform != null)
        {
            GameObject background = parentTransform.Find("Background")?.gameObject;
            if (background != null) selector.background = background;
        }
        
        ConnectDungeonButtons(menu, selector);
    }
    
    void ConnectDungeonButtons(GameObject menu, DungeonSelector selector)
    {
        for (int i = 0; i < 3; i++)
        {
            Button button = GetUIComponent<Button>(menu, $"SelectButton{i}");
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                int index = i;
                button.onClick.AddListener(() => selector.ChooseDungeon(index));
            }
        }
    }
    
    #endregion

    #region Utility Methods
    
    private GameObject GetPersistentObject(string name)
    {
        return PersistentObjectManager.Instance?.GetPersistentObject(name);
    }
    
    //find object in canvas
    private GameObject FindInCanvas(string name)
    {
        return FindInHierarchy(canvas, name);
    }
    
    //find object in hierarchy
    private GameObject FindInHierarchy(GameObject parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;
        
        foreach (Transform child in parent.transform)
        {
            GameObject found = FindInHierarchy(child.gameObject, name);
            if (found != null) return found;
        }
        
        return null;
    }
    
    //get component from canvas
    private T GetUIComponent<T>(string objectName) where T : Component
    {
        GameObject obj = FindInCanvas(objectName);
        return obj != null ? obj.GetComponent<T>() : null;
    }
    
    //get component from specific parent
    private T GetUIComponent<T>(GameObject parent, string objectName) where T : Component
    {
        GameObject obj = FindInHierarchy(parent, objectName);
        return obj != null ? obj.GetComponent<T>() : null;
    }
    
    //get TMP_Text component
    private TextMeshProUGUI GetUITextComponent(string objectName)
    {
        return GetUIComponent<TextMeshProUGUI>(objectName);
    }
    #endregion

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}