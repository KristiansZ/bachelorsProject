using UnityEngine;
using System.Collections.Generic;

public class PersistentObjectManager : MonoBehaviour
{
    public static PersistentObjectManager Instance { get; private set; }

    [Header("Prefabs To Make Persistent")]
    public GameObject mainCameraPrefab;
    public GameObject cinemachineCameraPrefab;
    public GameObject canvasPrefab;
    public GameObject uiManagerPrefab;
    public GameObject playerStatManagerPrefab;
    public GameObject teleportingPortalPrefab;
    public GameObject dungeonProgressManagerPrefab;
    public GameObject pauseManagerPrefab;
    private Dictionary<string, GameObject> persistentObjects = new Dictionary<string, GameObject>();
    private bool initialized = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); //make this object persistent
        InitializePersistentObjects();
    }

    private void InitializePersistentObjects()
    {
        if (initialized) return;

        CreatePersistent("MainCamera", mainCameraPrefab);
        CreatePersistent("CinemachineCamera", cinemachineCameraPrefab);
        CreatePersistent("CanvasScreenSpace", canvasPrefab);
        CreatePersistent("UIManager", uiManagerPrefab);
        CreatePersistent("PlayerStatManager", playerStatManagerPrefab);
        CreatePersistent("TeleportingPortal", teleportingPortalPrefab);
        CreatePersistent("DungeonProgressManager", dungeonProgressManagerPrefab);
        CreatePersistent("PauseManager", pauseManagerPrefab);

        initialized = true;
    }

    private void CreatePersistent(string name, GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"Prefab for {name} not assigned!");
            return;
        }

        GameObject obj = Instantiate(prefab);
        obj.name = "PERSISTENT_" + name;
        DontDestroyOnLoad(obj);
        persistentObjects[name] = obj;
    }

    public GameObject GetPersistentObject(string name)
    {
        return persistentObjects.ContainsKey(name) ? persistentObjects[name] : null;
    }
}
