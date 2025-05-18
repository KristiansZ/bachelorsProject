using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameEnd : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button quitButton;

    [Header("Scene Settings")]
    [SerializeField] private string sceneToReload = "SafeSpaceScene";

    private void Start()
    {
        DestroyAllDontDestroyOnLoadObjects(); //destroy persistent objects so if click play again, they are created again

        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(ReloadScene);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        fadeGroup.alpha = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        fadeGroup.alpha = 1f;
    }

    private void ReloadScene()
    {
        SceneManager.LoadScene(sceneToReload);
    }

    private void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    private void DestroyAllDontDestroyOnLoadObjects()
    {
        GameObject temp = new GameObject("TempSceneLoader");
        DontDestroyOnLoad(temp);

        Scene dontDestroyScene = temp.scene;
        Destroy(temp);

        GameObject[] rootObjects = dontDestroyScene.GetRootGameObjects();

        foreach (GameObject obj in rootObjects)
        {
            if (obj.GetComponent<MusicManager>() == null)
            {
                Destroy(obj);
            }
        }
    }
}
