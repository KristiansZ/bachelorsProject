using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class RoomSpawnPoints : MonoBehaviour
{
    public List<Transform> spawnZones = new List<Transform>();
    [Tooltip("Radius around each spawn zone to place enemies")]
    public float spawnRadius = 3f;
    public bool isPopulated = false;
    public RoomType roomType = RoomType.Normal;
    
    public enum RoomType
    {
        Normal,
        Hallway,
        Boss
    }
    
    public void AutoPopulateChildren()
    {
        spawnZones.Clear();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("SpawnPoint"))
            {
                spawnZones.Add(child);
            }
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        foreach (Transform zone in spawnZones)
        {
            if (zone != null)
            {
                Gizmos.DrawWireSphere(zone.position, spawnRadius);
            }
        }
    }
}