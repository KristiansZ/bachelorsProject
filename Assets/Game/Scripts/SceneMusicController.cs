using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneMusicController : MonoBehaviour
{
    public MusicManager.SceneType sceneType;

    void Start()
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetScene(sceneType);
            
            if (sceneType == MusicManager.SceneType.Dungeon)
            {
                StartCoroutine(CheckForBossDungeonAfterGeneration());
            }
        }
        else
        {
            Debug.LogWarning("MusicManager instance not found");
        }
    }
    
    private IEnumerator CheckForBossDungeonAfterGeneration()
    {
        DungeonGenerator dungeonGen = FindObjectOfType<DungeonGenerator>();
        if (dungeonGen != null)
        {
            dungeonGen.OnNavMeshReady += OnNavMeshReady;
            
            yield return new WaitForSeconds(1.5f); //fallback
            MusicManager.Instance.CheckForBossDungeon();
        }
    }
    
    private void OnNavMeshReady()
    {
        MusicManager.Instance.CheckForBossDungeon();
        
        //unsubscribe to avoid multiple calls
        DungeonGenerator dungeonGen = FindObjectOfType<DungeonGenerator>();
        if (dungeonGen != null)
        {
            dungeonGen.OnNavMeshReady -= OnNavMeshReady;
        }
    }
}