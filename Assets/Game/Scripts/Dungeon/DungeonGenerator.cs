using System.Collections.Generic;
using System.Collections;
using UnityEngine.AI;
using UnityEngine;
using System.Linq;

public class DungeonGenerator : MonoBehaviour
{
    public List<GameObject> roomPrefabs;
    public GameObject bossRoomPrefab;
    public List<GameObject> hallwayPrefabs;
    public int roomCount = 10;
    public GameObject wallCapPrefab;
    public NavMeshSurface navMeshSurface;

    [HideInInspector]
    public List<Room> spawnedRooms = new();
    [HideInInspector]
    public List<Room> spawnedHallways = new();
    [HideInInspector]
    public List<GameObject> allSpawnedObjects = new();
    public Room startRoom;

    public delegate void NavMeshReadyDelegate();
    [SerializeField] public event NavMeshReadyDelegate OnNavMeshReady;

    void Start()
    {
        var data = DungeonLoader.Instance?.currentDungeonData;
        if (data != null)
        {
            roomCount = data.roomCount;
            TryGenerateDungeon();
        }
        else
        {
            roomCount = 20;
            TryGenerateDungeon();
        }
    }

    void TryGenerateDungeon(int maxAttempts = 10)
    {
        if (DungeonProgressManager.Instance != null && DungeonProgressManager.Instance.IsBossDungeonAvailable())
        {
            GenerateBossDungeon();
            StartCoroutine(BuildNavMeshAfterDelay(0.2f));
            return;
        }
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            GenerateDungeon();
            
            if (spawnedRooms.Count + spawnedHallways.Count >= roomCount * 0.7f) //atleast 70% of rooms got generated
            {
                Debug.Log(attempt);
                StartCoroutine(BuildNavMeshAfterDelay(0.2f));
                return;
            }
            ClearDungeon();
        }
        
        Debug.LogWarning($"Failed to generate a suitable dungeon after {maxAttempts} attempts.");
    }

    public void ClearDungeon()
    {
        foreach (GameObject obj in allSpawnedObjects)
            if (obj != null) Destroy(obj);
        
        spawnedRooms.Clear();
        spawnedHallways.Clear();
        allSpawnedObjects.Clear();
    }

    void GenerateDungeon()
    {
        GameObject startRoomObj = Instantiate(roomPrefabs[Random.Range(0, roomPrefabs.Count)], Vector3.zero, Quaternion.identity);
        startRoom = startRoomObj.GetComponent<Room>();
        startRoom.spawnPoints.roomType = RoomSpawnPoints.RoomType.Normal;
        spawnedRooms.Add(startRoom);
        allSpawnedObjects.Add(startRoomObj);

        int currentRoomCount = 1;
        int attempts = 0;
        int maxAttempts = roomCount * 5;
        
        while (currentRoomCount < roomCount && attempts < maxAttempts)
        {
            attempts++;
            
            List<Room> roomsWithConnections = spawnedRooms.FindAll(room => room.GetAvailableConnectionCount() > 0);
            if (roomsWithConnections.Count == 0) break;
                
            Room existingRoom = roomsWithConnections[Random.Range(0, roomsWithConnections.Count)];
            ConnectionPoint fromPoint = existingRoom.GetAvailableConnection();
            if (fromPoint == null) continue;

            GameObject hallwayObj = Instantiate(hallwayPrefabs[Random.Range(0, hallwayPrefabs.Count)]);
            Room hallway = hallwayObj.GetComponent<Room>();
            hallway.spawnPoints.roomType = RoomSpawnPoints.RoomType.Hallway;
            
            List<ConnectionPoint> hallwayConnectionPoints = hallway.GetAllAvailableConnections();
            if (hallwayConnectionPoints.Count == 0)
            {
                Destroy(hallwayObj);
                continue;
            }

            ConnectionPoint hallwayEntryPoint = hallwayConnectionPoints[0];
            AlignRoom(fromPoint.transform, hallwayEntryPoint.transform, hallwayObj);
            
            if (IsObjectOverlapping(hallwayObj))
            {
                Destroy(hallwayObj);
                continue;
            }

            fromPoint.isConnected = true;
            hallwayEntryPoint.isConnected = true;
            hallwayConnectionPoints.RemoveAt(0);
            
            spawnedHallways.Add(hallway);
            allSpawnedObjects.Add(hallwayObj);

            foreach (ConnectionPoint hallwayExitPoint in hallwayConnectionPoints)
            {
                if (currentRoomCount >= roomCount) break;
                
                GameObject newRoomObj = Instantiate(roomPrefabs[Random.Range(0, roomPrefabs.Count)]);
                Room newRoom = newRoomObj.GetComponent<Room>();
                newRoom.spawnPoints.roomType = (currentRoomCount == roomCount - 1) ? 
                    RoomSpawnPoints.RoomType.Boss : RoomSpawnPoints.RoomType.Normal;
                
                ConnectionPoint roomEntryPoint = newRoom.GetAvailableConnection();
                if (roomEntryPoint == null)
                {
                    Destroy(newRoomObj);
                    continue;
                }

                AlignRoom(hallwayExitPoint.transform, roomEntryPoint.transform, newRoomObj);
                
                if (IsObjectOverlapping(newRoomObj))
                {
                    Destroy(newRoomObj);
                    continue;
                }

                hallwayExitPoint.isConnected = true;
                roomEntryPoint.isConnected = true;
                
                spawnedRooms.Add(newRoom);
                allSpawnedObjects.Add(newRoomObj);
                currentRoomCount++;
            }
        }
        
        CapUnusedConnections();
    }

    void GenerateBossDungeon()
    {
        ClearDungeon();
        
        if (bossRoomPrefab == null)
        {
            Debug.LogError("Boss Room Prefab is not assigned in DungeonGenerator");
            return;
        }
        
        GameObject bossRoomObj = Instantiate(bossRoomPrefab, Vector3.zero, Quaternion.identity);
        Room bossRoom = bossRoomObj.GetComponent<Room>();
        if (bossRoom == null)
        {
            Debug.LogError("Boss room prefab does not have a Room component attached");
            Destroy(bossRoomObj);
            return;
        }

        bossRoom.spawnPoints.roomType = RoomSpawnPoints.RoomType.Boss;
        spawnedRooms.Add(bossRoom);
        allSpawnedObjects.Add(bossRoomObj);
        startRoom = bossRoom;
    }

    void AlignRoom(Transform from, Transform to, GameObject room)
    {
        Vector3 fromForwardFlat = new Vector3(from.forward.x, 0, from.forward.z).normalized;
        Vector3 toForwardFlat = new Vector3(to.forward.x, 0, to.forward.z).normalized;
        
        Quaternion targetRotation = Quaternion.LookRotation(-fromForwardFlat, Vector3.up);
        Quaternion currentToRotation = Quaternion.LookRotation(toForwardFlat, Vector3.up);
        
        room.transform.rotation = targetRotation * Quaternion.Inverse(currentToRotation) * room.transform.rotation;
        room.transform.position += from.position - to.position;
    }

    IEnumerator BuildNavMeshAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (navMeshSurface == null)
        {
            Debug.LogError("NavMeshSurface is not assigned.");
            yield break;
        }

        navMeshSurface.BuildNavMesh();
        yield return new WaitUntil(() => NavMesh.CalculateTriangulation().indices.Length > 0);
        OnNavMeshReady?.Invoke();
    }

    public bool IsObjectOverlapping(GameObject newObject)
    {
        Bounds newBounds = GetObjectBounds(newObject);
        
        //check against existing rooms and hallways
        foreach (var existingObj in spawnedRooms)
        {
            if (existingObj == null || existingObj.gameObject == null) continue;
            Bounds existingBounds = GetObjectBounds(existingObj.gameObject);
            existingBounds.Expand(0.5f);
            if (newBounds.Intersects(existingBounds)) return true;
        }
        
        foreach (var hallway in spawnedHallways)
        {
            if (hallway == null || hallway.gameObject == null) continue;
            Bounds hallwayBounds = GetObjectBounds(hallway.gameObject);
            hallwayBounds.Expand(0.5f);
            if (newBounds.Intersects(hallwayBounds)) return true;
        }
        
        return false;
    }

    public Bounds GetObjectBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0)
            return new Bounds(obj.transform.position, Vector3.one);
        
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        
        bounds.size *= 0.95f; //so the rooms actually place, even if connector is a bit inside bounds
        return bounds;
    }

    void CapUnusedConnections()
    {
        List<ConnectionPoint> allUnusedPoints = new List<ConnectionPoint>();
        
        foreach (Room room in spawnedRooms)//collect all unused points from rooms and hallways
        {
            allUnusedPoints.AddRange(room.connectionPoints.Where(p => !p.isConnected));
        }
        foreach (Room hallway in spawnedHallways)
        {
            allUnusedPoints.AddRange(hallway.connectionPoints.Where(p => !p.isConnected));
        }

        for (int i = 0; i < allUnusedPoints.Count; i++)//check for nearby points (in case of loops)
        {
            ConnectionPoint currentPoint = allUnusedPoints[i];

            for (int j = i + 1; j < allUnusedPoints.Count; j++)//find nearby points in other rooms/hallways
            {
                ConnectionPoint otherPoint = allUnusedPoints[j];

                float distance = Vector3.Distance(
                    currentPoint.transform.position, 
                    otherPoint.transform.position
                );

                if (distance < 1f)//if points are close, mark both as connected
                {
                    currentPoint.isConnected = true;
                    otherPoint.isConnected = true;
                    break;
                }
            }
        }

        //cap unused connections
        foreach (Room room in spawnedRooms)
        {
            CapRoomConnections(room);
        }
        foreach (Room hallway in spawnedHallways)
        {
            CapRoomConnections(hallway);
        }
    }

    void CapRoomConnections(Room room)
    {
        if (room == null) return;

        foreach (ConnectionPoint point in room.connectionPoints)
        {
            if (!point.isConnected)
            {
                GameObject wall = Instantiate(
                    wallCapPrefab,
                    point.transform.position,
                    Quaternion.LookRotation(point.transform.forward, Vector3.up)
                );

                wall.transform.position -= wall.transform.up * 0.1f;
                wall.transform.SetParent(room.transform);
                allSpawnedObjects.Add(wall);
            }
        }
    }
}