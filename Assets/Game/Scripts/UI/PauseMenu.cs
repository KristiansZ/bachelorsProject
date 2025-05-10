using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;

    [Header("Settings")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    private bool isPaused = false;
    public static PauseMenu Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        
        InitializeUI();
    }
    
    public void InitializeUI()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        
        if (isPaused)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    private void PauseGame()
    {
        pauseMenuPanel.SetActive(true);
        isPaused = true;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        
        isPaused = false;
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public bool IsPaused => isPaused;
    
    //setters for UI elements (for use in UIConnector) also listeners for buttons in case InitializeUI didnt set
    public void SetPauseMenuPanel(GameObject panel)
    {
        pauseMenuPanel = panel;
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }
    
    public void SetResumeButton(Button button)
    {
        resumeButton = button;
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }
    }
    
    public void SetQuitButton(Button button)
    {
        quitButton = button;
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }
    }
}