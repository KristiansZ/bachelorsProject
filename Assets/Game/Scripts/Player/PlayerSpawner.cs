using UnityEngine;

public class PlayerSpawner : MonoBehaviour //place and dont destroy player prefab in the scene
{
    public GameObject playerPrefab;
    private static GameObject playerInstance;

    void Awake()
    {
        if (playerInstance == null)
        {
            playerInstance = Instantiate(playerPrefab, transform.position, Quaternion.identity);
            DontDestroyOnLoad(playerInstance);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}