using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using KinematicCharacterController;
using TMPro;

public class TeleportingPortal : MonoBehaviour
{
    [Header("Portal Configuration")]
    [SerializeField] private string safeSceneName = "SafeSpaceScene";
    [SerializeField] private string dungeonSceneName = "DungeonScene";
    [SerializeField] private string winMenuSceneName = "WinMenuScene";
    [SerializeField] private Vector3 safeScenePosition;
    [SerializeField] private Vector3 dungeonScenePosition = new Vector3(0, 2, 0);

    [Header("Enemy Kill Tracking")]
    [SerializeField] private float requiredKillPercentage = 0.75f; //75% required
    public TextMeshProUGUI enemyKillText;

    [Header("Portal State")]
    [SerializeField] public DungeonSelector dungeonSelector;
    
    [Header("Portal Recall")]
    [SerializeField] private KeyCode recallKey = KeyCode.T;

    public static TeleportingPortal instance;

    private bool playerInRange = false;
    private bool returnedFromDungeon = false;
    private static bool isTransitioning = false;
    private bool isPortalActive = false;

    private int totalEnemies = 0;
    private int killedEnemies = 0;
    private bool isBossDungeon = false;

    private string currentScene;
    private Vector3 lastPlayerPosition;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            currentScene = SceneManager.GetActiveScene().name;
            if (currentScene == safeSceneName)
            {
                safeScenePosition = transform.position;
                Light[] lights = GetComponentsInChildren<Light>();
                foreach (Light light in lights)
                {
                    light.enabled = false;
                }
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        SetPortalVisibility(false);
        
        if (DungeonProgressManager.Instance != null)
        {
            isBossDungeon = DungeonProgressManager.Instance.IsBossDungeonAvailable(); //true if boss dungeon
        }
    }

    void OnDestroy() //cleanup
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void Update()
    {
        currentScene = SceneManager.GetActiveScene().name;
        bool isSafeSpace = currentScene == safeSceneName;

        //check for portal teleport interaction
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !isTransitioning)
        {
            if ((isSafeSpace && dungeonSelector != null && dungeonSelector.HasChosenDungeon()) ||
                (!isSafeSpace && isPortalActive))
            {
                StartCoroutine(FadeTeleport());
            }
        }

        if (Input.GetKeyDown(recallKey) && !isTransitioning && !isSafeSpace && isPortalActive)
        {
            RecallPortalToPlayer();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentScene = scene.name;

        if (currentScene == safeSceneName)
        {
            transform.position = safeScenePosition;
            if (returnedFromDungeon)
            {
                SetPortalVisibility(false);
                ResetDungeonData();
            }
        }
        else if (currentScene == dungeonSceneName)
        {
            transform.position = dungeonScenePosition;
            
            //check whether isBossDungeon is true or false now
            if (DungeonProgressManager.Instance != null)
            {
                isBossDungeon = DungeonProgressManager.Instance.IsBossDungeonAvailable();
            }
        }

        if (enemyKillText != null)
        {
            enemyKillText.gameObject.SetActive(currentScene == dungeonSceneName);
        }

        isTransitioning = false;
    }

    public void RegisterTotalEnemyCount(int actualSpawnedCount)
    {
        totalEnemies = actualSpawnedCount;
        killedEnemies = 0;
        UpdateEnemyKillUI();
    }

    public void EnemyKilled()
    {
        killedEnemies++;

        if (isBossDungeon)
        {
            ActivatePortal();
        }

        UpdateEnemyKillUI();
        UpdatePortalState();
    }

    private void UpdatePortalState()
    {
        if (totalEnemies <= 0) return;

        bool shouldActivate = isBossDungeon ? 
            killedEnemies > 0 : 
            ((float)killedEnemies / totalEnemies) >= requiredKillPercentage;

        if (shouldActivate && !isPortalActive)
        {
            ActivatePortal();
        }
    }

    private void ActivatePortal()
    {
        isPortalActive = true;
        SetPortalVisibility(true);
    }

    private void UpdateEnemyKillUI()
    {
        if (enemyKillText == null || totalEnemies <= 0) return;

        if (isBossDungeon)
        {
            enemyKillText.text = killedEnemies > 0 ? "Portal Active" : "Defeat the Boss";
            return;
        }

        float killPercentage = (float)killedEnemies / totalEnemies;
        float activationPercentage = Mathf.Min(1.0f, killPercentage / requiredKillPercentage) * 100f;
        int portalActivationDisplay = Mathf.RoundToInt(activationPercentage);
            
        string portalText = $"Portal Activation: {portalActivationDisplay}%";
        if (isPortalActive)
        {
            portalText += " (Press T to call portal)";
        }
        
        enemyKillText.text = portalText;
    }

    private void RecallPortalToPlayer()
    {
        if (!isPortalActive || currentScene == safeSceneName) return;

        //find the player
        KinematicCharacterMotor player = FindObjectOfType<KinematicCharacterMotor>();
        if (player == null) return;

        //get player position and move the portal
        Vector3 newPosition = player.transform.position;
        newPosition.y = player.transform.position.y;
        transform.position = newPosition;
    }

    private IEnumerator FadeTeleport()
    {
        isTransitioning = true;

        ScreenFade fader = FindObjectOfType<ScreenFade>();
        if (fader != null)
            yield return fader.FadeOut();

        bool goingToWinMenu = currentScene == dungeonSceneName && isBossDungeon; //to fade to black and not go to SafeScene
        
        yield return StartCoroutine(TeleportPlayer());

        if (fader != null && !goingToWinMenu) //skip fade-in when going to win menu
            yield return fader.FadeIn();
    }

    private IEnumerator TeleportPlayer()
    {
        if (currentScene == dungeonSceneName && isBossDungeon && isPortalActive) //for leaving boss dungeon
        {
            if (DungeonProgressManager.Instance != null)
            {
                DungeonProgressManager.Instance.SetBossDungeonCompleted();
        
                SceneManager.LoadScene(winMenuSceneName);
            }
            yield break;
        }

        KinematicCharacterMotor characterMotor = FindObjectOfType<KinematicCharacterMotor>();
        if (characterMotor == null)
        {
            Debug.LogError("Kinematic character motor not found!");
            isTransitioning = false;
            yield break;
        }

        lastPlayerPosition = characterMotor.transform.position;
        string targetScene = (currentScene == safeSceneName) ? dungeonSceneName : safeSceneName;

        if (targetScene == safeSceneName)
        {
            returnedFromDungeon = true;
            
            if (DungeonProgressManager.Instance != null) //regular dungeon completed
            {
                DungeonProgressManager.Instance.CompleteDungeon(dungeonSelector.GetSelectedDungeon());
            }
        }

        Vector3 targetPosition;
        if (targetScene == safeSceneName)
        {
            Vector3 approachDirection = (lastPlayerPosition - transform.position).normalized;
            targetPosition = safeScenePosition + approachDirection * 2f;
            targetPosition.y = safeScenePosition.y;
        }
        else
        {
            targetPosition = dungeonScenePosition + new Vector3(0, 0, 2f);
        }

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(targetScene); //load scene before getting teleported
        yield return loadOp;

        yield return new WaitForSeconds(0.1f);

        characterMotor = FindObjectOfType<KinematicCharacterMotor>();
        if (characterMotor == null)
        {
            Debug.LogError("Character motor not found after scene load!");
            isTransitioning = false;
            yield break;
        }

        characterMotor.SetPosition(targetPosition);
        characterMotor.transform.position = targetPosition;
        characterMotor.enabled = false;
        characterMotor.transform.position = targetPosition;
        characterMotor.enabled = true;

        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();

        isTransitioning = false;
    }

    private void ResetDungeonData()
    {
        totalEnemies = 0;
        killedEnemies = 0;
        isPortalActive = false;
        returnedFromDungeon = false;
        isBossDungeon = false;

        if (dungeonSelector != null)
        {
            dungeonSelector.ResetDungeonChoice();
        }
    }

    public void SetPortalVisibility(bool visible)
    {
        Collider portalCollider = GetComponent<Collider>();
        if (portalCollider != null)
            portalCollider.enabled = visible;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = visible;
        }

        Light[] lights = GetComponentsInChildren<Light>();
        foreach (Light light in lights)
        {
            light.enabled = visible;
        }
    }

    //when dungeon is selected in dungeon selector.
    public void OnDungeonSelected()
    {
        SetPortalVisibility(true);
    }
}