using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartGame : MonoBehaviour
{
    [SerializeField] private Button startButton;

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
    }

    public void LoadSafeSpaceScene()
    {
        SceneManager.LoadScene("SafeSpaceScene");
    }
}