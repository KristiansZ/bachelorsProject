using UnityEngine;

public class DungeonLoader : MonoBehaviour
{
    public static DungeonLoader Instance;
    public DungeonData currentDungeonData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}