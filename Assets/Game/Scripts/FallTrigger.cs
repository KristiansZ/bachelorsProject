using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FallTrigger : MonoBehaviour
{
    public string loseSceneName = "LoseMenuScene";
    
    public float sceneLoadDelay = 0.5f;
    
    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            PlayerFell(other.gameObject);
        }
    }

    private void PlayerFell(GameObject player)
    {
        if (sceneLoadDelay > 0)
        {
            StartCoroutine(LoadLoseSceneWithDelay());
        }
        else
        {
            LoadLoseScene();
        }
    }

    private IEnumerator LoadLoseSceneWithDelay()
    {
        yield return new WaitForSeconds(sceneLoadDelay);
        LoadLoseScene();
    }

    private void LoadLoseScene()
    {
        SceneManager.LoadScene(loseSceneName);
    }
}