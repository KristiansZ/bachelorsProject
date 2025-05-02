using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class DungeonEnemyManager : MonoBehaviour
{
    [System.Serializable]
    public class EnemyTypeConfig
    {
        public EnemyStats stats;
        public float spawnWeight = 1f;
        public RoomSpawnPoints.RoomType[] validRoomTypes = { RoomSpawnPoints.RoomType.Normal };
    }

    [Header("References")]
    [SerializeField] GameObject enemyPrefab;
    [SerializeField] Transform player;
    [SerializeField] DungeonGenerator dungeonGenerator;

    [Header("Spawn Settings")]
    [SerializeField] EnemyTypeConfig[] enemyTypes;
    [SerializeField] int minEnemiesPerRoom = 2;
    [SerializeField] int maxEnemiesPerRoom = 5;
    [SerializeField] float activationDistance = 30f;
    [SerializeField] float deactivationDistance = 40f;

    [Header("Boss Settings")]
    [SerializeField] GameObject bossPrefab;
    [SerializeField] EnemyStats bossStats;
    [SerializeField] RoomSpawnPoints.RoomType bossRoomType = RoomSpawnPoints.RoomType.Boss;
    
    [SerializeField] private Transform poolContainer;
    
    private ObjectPool enemyPool;
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

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (dungeonGenerator == null)
        {
            dungeonGenerator = FindObjectOfType<DungeonGenerator>();
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
            if (dungeonGenerator != null && dungeonGenerator.lastSpawnRoom != null)
            {
                startRoom = dungeonGenerator.lastSpawnRoom.GetComponent<RoomSpawnPoints>();
            }
            else
            {
                Debug.LogWarning("Could not identify start room from dungeon generator");
            }
            
            InitializeEnemyPool();
        }
    }

    private void InitializeEnemyPool()
    {   
        if (isInitialized) return;
        
        //create a pool of enemies based on the number of rooms and max enemies per room
        int roomCount = FindObjectsOfType<RoomSpawnPoints>().Length;
        int poolSize = roomCount * maxEnemiesPerRoom;
        
        enemyPool = new ObjectPool(enemyPrefab, 0, poolContainer);
        
        //initialize pooled enemies
        foreach (var obj in enemyPool.GetAllPooledObjects())
        {
            var pooledObj = obj.GetComponent<PooledObject>() ?? obj.AddComponent<PooledObject>();
            pooledObj.SetSpawnManager(this);
            obj.transform.SetParent(poolContainer);
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
            
            // Count enemies in each room that has spawn zones
            if (room.spawnZones != null && room.spawnZones.Count > 0)
            {
                totalEnemiesInDungeon += Random.Range(minEnemiesPerRoom, maxEnemiesPerRoom + 1);
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
        
        if (dungeonGenerator != null && dungeonGenerator.lastSpawnRoom != null)
        {
            RoomSpawnPoints lastRoomSpawnPoints = dungeonGenerator.lastSpawnRoom.GetComponent<RoomSpawnPoints>();
            if (lastRoomSpawnPoints == room)
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

        int enemyCount = Random.Range(minEnemiesPerRoom, maxEnemiesPerRoom + 1);
        
        if (room.spawnZones == null || room.spawnZones.Count == 0)
        {
            Debug.LogWarning($"Room {room.gameObject.name} has no spawn zones");
            return;
        }
        
        var availableZones = new List<Transform>();
        
        foreach (var zone in room.spawnZones)
        {
            if (zone != null)
            {
                availableZones.Add(zone);
            }
        }
        
        if (availableZones.Count == 0)
        {
            Debug.LogWarning($"Room {room.gameObject.name} has no valid spawn zones");
            return;
        }

        var selectedZones = new List<Transform>();

        //rooms can have multiple spawn zones, randomly select a few to use
        int zonesToUse = Mathf.Min(Random.Range(1, availableZones.Count + 1), availableZones.Count);
        for (int i = 0; i < zonesToUse; i++)
        {
            int randomIndex = Random.Range(0, availableZones.Count);
            selectedZones.Add(availableZones[randomIndex]);
            availableZones.RemoveAt(randomIndex);
        }

        int remainingEnemies = enemyCount;
        int enemiesPlaced = 0;
        
        foreach (var zone in selectedZones) //spawn enemies in selected zones
        {
            if (remainingEnemies <= 0) break;
            
            int enemiesForZone = Mathf.CeilToInt((float)remainingEnemies / selectedZones.Count);
            enemiesForZone = Mathf.Min(enemiesForZone, remainingEnemies);
            remainingEnemies -= enemiesForZone;

            for (int i = 0; i < enemiesForZone; i++)
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

        GameObject enemy = enemyPool.GetObject(spawnPos);
        if (enemy == null)
        {
            Debug.LogWarning("Enemy pool exhausted");
            return false;
        }

        enemy.GetComponent<EnemyController>().Initialize(enemyType.stats);
        
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

        if (bossRoom.spawnZones == null || bossRoom.spawnZones.Count == 0)
        {
            Debug.LogWarning($"Boss room {bossRoom.gameObject.name} has no spawn zones, using room center instead");

            Vector3 spawnPosition = bossRoom.transform.position; //fallback spawnpoint in case no spawn zones
            
            if (IsPositionOnNavMesh(spawnPosition))
            {
                SpawnBossAtPosition(spawnPosition);
            }
            else
            {
                Debug.LogWarning("Failed to spawn boss on valid NavMesh position!");
            }
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
            bossController.Initialize(bossStats);
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
        if (enemyPool == null) return;
        
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
        enemy.transform.SetParent(poolContainer);
        enemyPool.ReturnObject(enemy);
    }

    private bool IsPositionOnNavMesh(Vector3 position)
    {
        return NavMesh.SamplePosition(position, out _, 0.1f, NavMesh.AllAreas);
    }
}