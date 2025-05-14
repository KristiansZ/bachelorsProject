using System.Collections.Generic;
using UnityEngine;

public class LampPlacer : MonoBehaviour
{
    [Header("Lamp Settings")]
    public List<GameObject> lampPrefabs;
    public float minDistanceBetweenLamps = 2f;
    public float wallOffset = 0.5f;
    public int maxLampsPerRoom = 4;
    public int maxLampsPerHallway = 2;
    public float placementProbability = 0.8f;
    
    [Header("Placement Adjustments")]
    public float heightOffset = 0.0f;
    public bool snapToGround = true;
    public float maxGroundCheckDistance = 3f;
    public LayerMask floorLayerMask = -1;
    
    [Header("Validity Checks")] //checks for ground
    public int meshValidationRays = 5;
    public float meshValidationRadius = 0.5f;
    public float minRayHitPercentage = 0.6f;
    
    [Header("Light Colour Settings")]
    public Color defaultLampColour = new Color(1.0f, 0.5f, 0.0f, 1.0f);
    public Color activatedLampColour = Color.green;
    public float playerDetectionRadius = 5.0f;
    public string playerTag = "Player";
    
    private DungeonGenerator dungeonGenerator;
    private List<GameObject> spawnedLamps = new List<GameObject>();
    
    private void Start()
    {
        dungeonGenerator = GetComponent<DungeonGenerator>();
        if (dungeonGenerator != null)
        {
            dungeonGenerator.OnNavMeshReady += PlaceLampsInDungeon;
        }
        else
        {
            Debug.LogError("LampPlacer requires a DungeonGenerator component on the same GameObject.");
        }
    }
    
    public void PlaceLampsInDungeon()
    {
        
        foreach (Room room in dungeonGenerator.spawnedRooms)
        {
            PlaceLampsInRoom(room, maxLampsPerRoom);
        }
        
        foreach (Room hallway in dungeonGenerator.spawnedHallways)
        {
            PlaceLampsInRoom(hallway, maxLampsPerHallway);
        }
    }
    
    private void PlaceLampsInRoom(Room room, int maxLamps)
    {
        if (room == null || lampPrefabs.Count == 0) return;
        
        Bounds roomBounds = GetRoomBounds(room.gameObject);
        Vector3 roomCenter = roomBounds.center;
        Vector3 roomExtents = roomBounds.extents;
        
        int lampsToPlace = Random.Range(1, maxLamps + 1);
        
        List<Vector3> lampPositions = new List<Vector3>();
        
        int maxAttempts = lampsToPlace * 20; //limit attempt so no infinite loop
        int attempts = 0;
        
        while (lampPositions.Count < lampsToPlace && attempts < maxAttempts)
        {
            attempts++;
            
            if (Random.value > placementProbability) continue;
            
            Vector3 position;
            Vector3 wallDirection = Vector3.zero;
            
            
            int side = Random.Range(0, 4);
            
            if (side == 0)
            {
                position = roomCenter + new Vector3(
                    Random.Range(-roomExtents.x + wallOffset, roomExtents.x - wallOffset),
                    0,
                    roomExtents.z - wallOffset
                );
                wallDirection = Vector3.back;
            }
            else if (side == 1)
            {
                position = roomCenter + new Vector3(
                    roomExtents.x - wallOffset,
                    0,
                    Random.Range(-roomExtents.z + wallOffset, roomExtents.z - wallOffset)
                );
                wallDirection = Vector3.left;
            }
            else if (side == 2)
            {
                position = roomCenter + new Vector3(
                    Random.Range(-roomExtents.x + wallOffset, roomExtents.x - wallOffset),
                    0,
                    -roomExtents.z + wallOffset
                );
                wallDirection = Vector3.forward;
            }
            else
            {
                position = roomCenter + new Vector3(
                    -roomExtents.x + wallOffset,
                    0,
                    Random.Range(-roomExtents.z + wallOffset, roomExtents.z - wallOffset)
                );
                wallDirection = Vector3.right;
            }
            
            //check if position is valid (not too close to other lamps and has floor mesh)
            if (IsValidLampPosition(position, lampPositions, room) && HasValidFloorMesh(position))
            {
                lampPositions.Add(position);
                PlaceLamp(position, wallDirection, room.transform);
            }
        }
    }
    
    private bool IsValidLampPosition(Vector3 position, List<Vector3> existingPositions, Room room)
    {
        foreach (Vector3 existingPos in existingPositions) //lamp distance validation
        {
            if (Vector3.Distance(position, existingPos) < minDistanceBetweenLamps)
                return false;
        }
        
        foreach (ConnectionPoint connection in room.connectionPoints) //validate against connections (no lamps inside "doorways")
        {
            if (connection == null) continue;
            
            if (Vector3.Distance(new Vector3(position.x, 0, position.z), 
                               new Vector3(connection.transform.position.x, 0, connection.transform.position.z)) 
                < minDistanceBetweenLamps * 1.5f)
                return false;
        }
        
        return true;
    }
    
    private bool HasValidFloorMesh(Vector3 position)
    {
        int hitsCount = 0;
        
        for (int i = 0; i < meshValidationRays; i++) //cast rays to check for mesh
        {
            float angle = i * (2 * Mathf.PI / meshValidationRays);
            float x = Mathf.Cos(angle) * meshValidationRadius;
            float z = Mathf.Sin(angle) * meshValidationRadius;
            
            Vector3 checkPos = position + new Vector3(x, 0, z);
            Vector3 rayStart = checkPos + Vector3.up * maxGroundCheckDistance;
            
            if (Physics.Raycast(rayStart, Vector3.down, maxGroundCheckDistance * 2, floorLayerMask))
            {
                hitsCount++;
            }
        }
        
        float hitPercentage = (float)hitsCount / meshValidationRays;
        
        //check if center hit is valid
        bool centerHit = Physics.Raycast(
            position + Vector3.up * maxGroundCheckDistance, 
            Vector3.down, 
            maxGroundCheckDistance * 2, 
            floorLayerMask
        );
        
        //must hit center && meet minimum hit percentage threshold for point to be considered valid
        return centerHit && (hitPercentage >= minRayHitPercentage);
    }
    
    private void PlaceLamp(Vector3 position, Vector3 wallDirection, Transform parent)
    {
        //select random lamp prefab (might have more lamp prefabs later on)
        GameObject lampPrefab = lampPrefabs[Random.Range(0, lampPrefabs.Count)];
        
        if (snapToGround) //try to get lamp to be on the ground
        {
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * maxGroundCheckDistance, Vector3.down, out hit, 
                maxGroundCheckDistance * 2, floorLayerMask))
            {
                position.y = hit.point.y + heightOffset;
            }
            else
            {
                position.y += heightOffset;
            }
        }
        else
        {
            position.y += heightOffset;
        }
        
        GameObject lamp = Instantiate(lampPrefab, position, Quaternion.identity, parent);
        
        //face the lamp towards centre of the room (right now not necessary, but might be useful later if not symmetrical lamps) 
        if (wallDirection != Vector3.zero)
        {
            lamp.transform.rotation = Quaternion.LookRotation(wallDirection, Vector3.up);
        }
        else //no wall direction, just random rotation
        {
            lamp.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        }
        
        //proximity controller component to handle color change
        LampProximityController proximityController = lamp.AddComponent<LampProximityController>();
        if (proximityController != null)
        {
            proximityController.defaultColour = defaultLampColour;
            proximityController.activatedColour = activatedLampColour;
            proximityController.detectionRadius = playerDetectionRadius;
            proximityController.playerTag = playerTag;
            
            Light lampLight = lamp.GetComponentInChildren<Light>();
            if (lampLight != null)
            {
                proximityController.lampLight = lampLight;
                lampLight.color = defaultLampColour;
            }
        }
        
        spawnedLamps.Add(lamp);
    }
    
    private Bounds GetRoomBounds(GameObject roomObject)
    {
        Renderer[] renderers = roomObject.GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0)
        {
            return new Bounds(roomObject.transform.position, Vector3.one * 5f);
        }
        
        Bounds bounds = renderers[0].bounds;
        
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        
        bounds.size = bounds.size * 0.9f; //reduce bounds a bit (somehow was easier to work with than distance from walls)
        
        return bounds;
    }
}