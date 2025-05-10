using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class DungeonEnemyManager : MonoBehaviour
{
    [System.Serializable]
    public class EnemyTypeConfig
    {
        public string enemyName;
        public GameObject enemyPrefab;
        public EnemyStats stats;
        public float spawnWeight = 1f;
        public RoomSpawnPoints.RoomType[] validRoomTypes = { RoomSpawnPoints.RoomType.Normal };
    }

    [Header("References")]
    [SerializeField] GameObject defaultEnemyPrefab; //fallback if no enemies set
    [SerializeField] Transform player;
    [SerializeField] DungeonGenerator dungeonGenerator;
    [SerializeField] PlayerLeveling playerLeveling;

    [Header("Spawn Settings")]
    [SerializeField] EnemyTypeConfig[] enemyTypes;
    [SerializeField] int minEnemiesPerSpawnPoint = 1; 
    [SerializeField] int maxEnemiesPerSpawnPoint = 4; 
    [SerializeField] float activationDistance = 30f;
    [SerializeField] float deactivationDistance = 40f;

    [Header("Boss Settings")]
    [SerializeField] GameObject bossPrefab;
    [SerializeField] EnemyStats bossStats;
    [SerializeField] RoomSpawnPoints.RoomType bossRoomType = RoomSpawnPoints.RoomType.Boss;
    
    [SerializeField] private Transform poolContainer;
    
    private Dictionary<GameObject, ObjectPool> enemyPools = new Dictionary<GameObject, ObjectPool>(); //multiple pools for different enemy types
    private Dictionary<RoomSpawnPoints, List<GameObject>> spawnedEnemiesByRoom = new Dictionary<RoomSpawnPoints, List<GameObject>>();
    private HashSet<RoomSpawnPoints> activeRooms = new HashSet<RoomSpawnPoints>();
    private HashSet<RoomSpawnPoints> populatedRooms = new HashSet<RoomSpawnPoints>(); 
    private RoomSpawnPoints startRoom;
    private bool isInitialized = false;
    private bool isNavMeshReady = false;
    private int totalEnemiesInDungeon = 0;
    private bool hasRegisteredTotalWithPortal = false;
    private int totalSpawnedEnemies = 0;
    private bool isBossDungeon = false;
    private Dictionary<GameObject, GameObject> enemyPrefabLookup = new Dictionary<GameObject, GameObject>();
    private int playerLevel = 1;

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (dungeonGenerator == null)
        {
            dungeonGenerator = FindObjectOfType<DungeonGenerator>();
        }

        //find PlayerLeveling if not assigned
        if (playerLeveling == null && player != null)
        {
            playerLeveling = player.GetComponent<PlayerLeveling>();
            if (playerLeveling == null)
            {
                playerLeveling = player.GetComponentInChildren<PlayerLeveling>();
            }
        }

        //get current player level
        if (playerLeveling != null)
        {
            playerLevel = playerLeveling.currentLevel;
            Debug.Log($"Player level for enemy scaling: {playerLevel}");
        }
        else
        {
            Debug.LogWarning("PlayerLeveling reference not found. Using default level 1 for enemy scaling.");
        }

        if (dungeonGenerator != null)
        {
            dungeonGenerator.OnNavMeshReady += OnNavMeshBaked;
        }
        else
        {
            Debug.LogError("DungeonGenerator reference is missing!");
        }
        
        //container for pooled enemies so they don't just make the scene messy
        if (poolContainer == null)
        {
            GameObject container = new GameObject("EnemyPoolContainer");
            container.transform.position = new Vector3(0, -10000, 0); //far below the world so not accidentally initialized
            poolContainer = container.transform;
        }
    }

    private void OnNavMeshBaked()
    {
        isNavMeshReady = true;
        
        if (DungeonProgressManager.Instance != null && DungeonProgressManager.Instance.isBossDungeonAvailable)
        {
            isBossDungeon = true;
            SpawnBossInBossRoom();
        } 
        else
        {
            //get start room from dungeon generator (no enemies in start room)
            if (dungeonGenerator != null && dungeonGenerator.startRoom != null)
            {
                startRoom = dungeonGenerator.startRoom.GetComponent<RoomSpawnPoints>();
            }
            else
            {
                Debug.LogWarning("Could not identify start room from dungeon generator");
            }
            
            InitializeEnemyPools();
        }
    }

    private void InitializeEnemyPools()
    {   
        if (isInitialized) return;
        
        var allRooms = FindObjectsOfType<RoomSpawnPoints>();
        int totalSpawnPoints = 0;
        
        foreach (var room in allRooms)
        {
            if (room.spawnZones != null)
            {
                totalSpawnPoints += room.spawnZones.Count;
            }
        }
        
        foreach (var enemyType in enemyTypes)
        {
            GameObject prefabToUse = enemyType.enemyPrefab != null ? enemyType.enemyPrefab : defaultEnemyPrefab;
            
            if (prefabToUse == null)
            {
                Debug.LogError($"No prefab assigned for enemy type {enemyType.enemyName}");
                continue;
            }
            
            //pool size based on total spawn points
            int poolSize = Mathf.CeilToInt(totalSpawnPoints * maxEnemiesPerSpawnPoint * enemyType.spawnWeight);
            poolSize = Mathf.Max(poolSize, 10); //at least 10 of each
            
            //create a pool if one doesn't exist for this prefab
            if (!enemyPools.ContainsKey(prefabToUse))
            {
                var pool = new ObjectPool(prefabToUse, poolSize, poolContainer);
                enemyPools[prefabToUse] = pool;
            }
        }
        
        //at least one pool
        if (enemyPools.Count == 0 && defaultEnemyPrefab != null)
        {
            int poolSize = totalSpawnPoints * maxEnemiesPerSpawnPoint;
            var defaultPool = new ObjectPool(defaultEnemyPrefab, poolSize, poolContainer);
            enemyPools[defaultEnemyPrefab] = defaultPool;
        }

        //used for portal activation
        CalculateTotalEnemyCount();

        isInitialized = true;
        enabled = true;
    }

    private void CalculateTotalEnemyCount()
    {
        var allRooms = FindObjectsOfType<RoomSpawnPoints>();
        totalEnemiesInDungeon = 0;
        
        foreach (var room in allRooms)
        {
            //skip the start room if not a boss dungeon
            if (!isBossDungeon && IsStartRoom(room)) 
            {
                continue;
            }
            
            //count enemies by spawn points
            if (room.spawnZones != null && room.spawnZones.Count > 0)
            {
                int spawnPoints = room.spawnZones.Count;
                //divide by 4 so 50% of average enemies. i want player to kill some enemies so they get xp, but not be needed to kill most/all if they dont want to
                int avgEnemiesPerPoint = Mathf.CeilToInt((minEnemiesPerSpawnPoint + maxEnemiesPerSpawnPoint) / 4f);
                totalEnemiesInDungeon += spawnPoints * avgEnemiesPerPoint;
            }
        }
        
        RegisterTotalEnemiesWithPortal();
    }

    private bool IsStartRoom(RoomSpawnPoints room)
    {
        if (startRoom != null && startRoom == room)
        {
            return true;
        }
        
        if (dungeonGenerator != null && dungeonGenerator.startRoom != null)
        {
            RoomSpawnPoints startRoomSpawnPoints = dungeonGenerator.startRoom.GetComponent<RoomSpawnPoints>();
            if (startRoomSpawnPoints == room)
            {
                return true;
            }
        }
        
        return false;
    }

    private void RegisterTotalEnemiesWithPortal()//give the portal the total enemy count
    {
        if (!hasRegisteredTotalWithPortal && TeleportingPortal.instance != null)
        {
            TeleportingPortal.instance.RegisterTotalEnemyCount(totalEnemiesInDungeon);
            hasRegisteredTotalWithPortal = true;
        }
    }

    void Update()
    {
        if (!isNavMeshReady || !isInitialized) return;
        
        RegisterTotalEnemiesWithPortal();
        
        if (player != null)
        {
            UpdateActiveRooms();
        }
    }

    private void UpdateActiveRooms()
    {
        var allRooms = FindObjectsOfType<RoomSpawnPoints>();
        var newActiveRooms = new HashSet<RoomSpawnPoints>();
        
        foreach (var room in allRooms)
        {
            float distance = Vector3.Distance(room.transform.position, player.position);
            
            if (distance <= activationDistance)
            {
                newActiveRooms.Add(room);
                
                if (!activeRooms.Contains(room))
                {
                    //populate rooms that are not already populated, not the start room (except boss), and have spawn zones
                    bool shouldSkipRoom = !isBossDungeon && IsStartRoom(room);
                    
                    if (!populatedRooms.Contains(room) && 
                        !shouldSkipRoom && 
                        room.spawnZones != null &&
                        room.spawnZones.Count > 0)
                    {
                        PopulateRoom(room);
                    }
                    //if room is already populated but was inactive (player went away), reactivate enemies
                    else if (populatedRooms.Contains(room) && spawnedEnemiesByRoom.ContainsKey(room))
                    {
                        ActivateRoomEnemies(room);
                    }
                }
            }
            else if (distance > deactivationDistance)
            {
                //deactivate enemies in rooms that became inactive
                if (activeRooms.Contains(room))
                {
                    DeactivateRoom(room);
                }
            }
        }
        
        activeRooms = newActiveRooms;
    }
    
    private void ActivateRoomEnemies(RoomSpawnPoints room)
    {
        if (spawnedEnemiesByRoom.TryGetValue(room, out var enemies) && enemies.Count > 0)
        {
            int activatedCount = 0;
            foreach (var enemy in enemies.ToList())
            {
                if (enemy != null)
                {
                    enemy.SetActive(true);
                    activatedCount++;
                }
                else
                {
                    //remove null
                    enemies.Remove(enemy);
                }
            }
        }
    }
    
    private void PopulateRoom(RoomSpawnPoints room)
    {
        //dont populate start room in non-boss dungeons
        if (!isBossDungeon && IsStartRoom(room))
        {
            return;
        }
        
        if (!spawnedEnemiesByRoom.ContainsKey(room))
        {
            spawnedEnemiesByRoom[room] = new List<GameObject>();
        }
        
        if (room.spawnZones == null || room.spawnZones.Count == 0)
        {
            Debug.LogWarning($"Room {room.gameObject.name} has no spawn zones");
            return;
        }
        
        var validSpawnZones = new List<Transform>();
        
        foreach (var zone in room.spawnZones)
        {
            if (zone != null)
            {
                validSpawnZones.Add(zone);
            }
        }
        
        if (validSpawnZones.Count == 0)
        {
            Debug.LogWarning($"Room {room.gameObject.name} has no valid spawn zones");
            return;
        }

        int enemiesPlaced = 0;
        
        //populate each spawn zone
        foreach (var zone in validSpawnZones)
        {
            int enemyCount = Random.Range(minEnemiesPerSpawnPoint, maxEnemiesPerSpawnPoint + 1);
            
            for (int i = 0; i < enemyCount; i++)
            {
                if (SpawnEnemyAtZone(room, zone))
                {
                    enemiesPlaced++;
                }
            }
        }
        
        //mark room as populated so no repeats
        populatedRooms.Add(room);
        room.isPopulated = true;
        
        totalSpawnedEnemies += enemiesPlaced;
    }

    private bool SpawnEnemyAtZone(RoomSpawnPoints room, Transform zone)
    {
        var enemyType = GetRandomEnemyForRoomType(room.roomType);
        if (enemyType == null) return false;

        Vector3 spawnPos = GetValidSpawnPosition(zone.position, room.spawnRadius);
        if (spawnPos == Vector3.zero)
        {
            Debug.LogWarning($"Failed to find valid spawn position in {room.gameObject.name}");
            return false;
        }

        //appropriate prefab and pool
        GameObject prefabToUse = enemyType.enemyPrefab != null ? enemyType.enemyPrefab : defaultEnemyPrefab;
        if (prefabToUse == null)
        {
            Debug.LogError("No valid prefab for enemy!");
            return false;
        }

        if (!enemyPools.TryGetValue(prefabToUse, out var pool))
        {
            Debug.LogWarning($"No pool for prefab {prefabToUse.name}");
            return false;
        }

        GameObject enemy = pool.GetObject(spawnPos);
        if (enemy == null)
        {
            Debug.LogWarning($"Enemy pool for {prefabToUse.name} exhausted");
            return false;
        }

        //store which prefab this enemy is from for pooling purposes
        enemyPrefabLookup[enemy] = prefabToUse;
        
        //get scaled stats
        EnemyStats scaledStats = enemyType.stats.GetScaledVersion(playerLevel);
        
        //initialize with the correct SCALED stats and give it a PooledObject component if it doesnt exist
        enemy.GetComponent<EnemyController>().Initialize(scaledStats);
        var pooledObj = enemy.GetComponent<PooledObject>() ?? enemy.AddComponent<PooledObject>();
        pooledObj.SetSpawnManager(this);
        
        spawnedEnemiesByRoom[room].Add(enemy);
        return true;
    }

    private Vector3 GetValidSpawnPosition(Vector3 center, float radius) //finds a valid spawn position on the navmesh
    {
        for (int i = 0; i < 30; i++) //max 30 attempts
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * radius;
            randomPoint.y = center.y;
            
            if (NavMesh.SamplePosition(randomPoint, out var hit, radius, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return Vector3.zero;
    }
    
    private EnemyTypeConfig GetRandomEnemyForRoomType(RoomSpawnPoints.RoomType roomType) //for when we have multiple enemy types
    {
        var validEnemies = enemyTypes
            .Where(e => e.validRoomTypes.Contains(roomType))
            .ToList();

        if (validEnemies.Count == 0) return null;

        float totalWeight = validEnemies.Sum(e => e.spawnWeight);
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var enemy in validEnemies)
        {
            currentWeight += enemy.spawnWeight;
            if (randomValue <= currentWeight)
                return enemy;
        }

        return validEnemies[0];
    }

    private void SpawnBossInBossRoom()
    {
        var allRooms = FindObjectsOfType<RoomSpawnPoints>();
        
        //boss room is the one with the boss spawn type (and only room in the dungeon)
        var bossRoom = allRooms.FirstOrDefault(r => r.roomType == bossRoomType);

        if (bossRoom == null)
        {
            Debug.LogWarning("No boss room found in dungeon!");
            return;
        }

        var validSpawnZones = bossRoom.spawnZones.Where(zone => zone != null).ToList();
        if (validSpawnZones.Count == 0)
        {
            Debug.LogWarning($"Boss room {bossRoom.gameObject.name} has no valid spawn zones");
            return;
        }

        Transform selectedZone = validSpawnZones[Random.Range(0, validSpawnZones.Count)];
        Vector3 spawnPos = GetValidSpawnPosition(selectedZone.position, bossRoom.spawnRadius);
        
        if (spawnPos != Vector3.zero)
        {
            SpawnBossAtPosition(spawnPos);
        }
        else
        {
            Debug.LogWarning("Failed to find valid spawn position for boss!");
        }
    }

    private void SpawnBossAtPosition(Vector3 position)
    {
        GameObject boss = Instantiate(bossPrefab, position, Quaternion.identity);
        var bossController = boss.GetComponent<EnemyController>();
        if (bossController != null)
        {
            //sale boss stats
            EnemyStats scaledBossStats = bossStats.GetScaledVersion(playerLevel);
            bossController.Initialize(scaledBossStats);
        }
    }
    
    private void DeactivateRoom(RoomSpawnPoints room)
    {
        if (spawnedEnemiesByRoom.TryGetValue(room, out var enemies))
        {
            int deactivatedCount = 0;
            foreach (var enemy in enemies.ToList())
            {
                if (enemy != null && enemy.activeSelf)
                {
                    enemy.SetActive(false);
                    enemy.transform.SetParent(poolContainer);
                    deactivatedCount++;
                }
                else if (enemy == null)
                {
                    enemies.Remove(enemy);
                }
            }
            Debug.Log($"Deactivated {deactivatedCount} enemies in {room.gameObject.name}");
        }
    }
    
    public void ReturnEnemyToPool(GameObject enemy)
    {
        if (enemy == null) return;
        
        //find the right pool
        if (enemyPrefabLookup.TryGetValue(enemy, out GameObject prefab) && enemyPools.TryGetValue(prefab, out ObjectPool pool))
        {
            //remove from room tracking
            bool removed = false;
            foreach (var roomEntry in spawnedEnemiesByRoom)
            {
                if (roomEntry.Value.Remove(enemy))
                {
                    removed = true;
                    break;
                }
            }
            
            if (removed)
            {
                totalSpawnedEnemies--;
            }
            
            //remove from lookup before returning to pool
            enemyPrefabLookup.Remove(enemy);
            
            //return to pool
            enemy.transform.SetParent(poolContainer);
            pool.ReturnObject(enemy);
            return;
        }
        Debug.LogWarning($"Failed to find pool for enemy {enemy.name}. Destroying instead.");
        Destroy(enemy);
    }

    private bool IsPositionOnNavMesh(Vector3 position)
    {
        return NavMesh.SamplePosition(position, out _, 0.1f, NavMesh.AllAreas);
    }
}