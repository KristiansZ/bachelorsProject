using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartGame : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button infoButton;
    [SerializeField] private GameObject infoTexts;

    private void Awake()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(LoadSafeSpaceScene);
        }
        else
        {
            Debug.LogError("Start button not assigned in Inspector!");
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
        else
        {
            Debug.LogError("Quit button not assigned in Inspector!");
        }

        if (infoButton != null)
        {
            infoButton.onClick.AddListener(ToggleInfoPanel);
        }
        else
        {
            Debug.LogError("Info button not assigned in Inspector!");
        }

        if (infoTexts != null)
        {
            infoTexts.SetActive(false);
        }
        else
        {
            Debug.LogError("InfoTexts GameObject not assigned in Inspector!");
        }
    }

    public void LoadSafeSpaceScene()
    {
        SceneManager.LoadScene("SafeSpaceScene");
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void ToggleInfoPanel()
    {
        if (infoTexts != null)
        {
            infoTexts.SetActive(!infoTexts.activeSelf);
        }
    }
}