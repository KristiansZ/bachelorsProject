using UnityEngine;
using System.Collections.Generic;

public class Room : MonoBehaviour
{
    public ConnectionPoint[] connectionPoints;
    [HideInInspector] public RoomSpawnPoints spawnPoints;
    private void Awake()
    {
        connectionPoints = GetComponentsInChildren<ConnectionPoint>();
        spawnPoints = GetComponent<RoomSpawnPoints>();
        if (spawnPoints == null)
            spawnPoints = gameObject.AddComponent<RoomSpawnPoints>();
    }

    // returns a random available connection point
    public ConnectionPoint GetAvailableConnection()
    {
        List<ConnectionPoint> availablePoints = new List<ConnectionPoint>();
        foreach (var point in connectionPoints)
        {
            if (!point.isConnected)
                availablePoints.Add(point);
        }

        return availablePoints.Count > 0 ? 
            availablePoints[Random.Range(0, availablePoints.Count)] : 
            null;
    }

    // returns all available connection points for room/hallway
    public List<ConnectionPoint> GetAllAvailableConnections()
    {
        List<ConnectionPoint> availablePoints = new List<ConnectionPoint>();
        foreach (var point in connectionPoints)
        {
            if (!point.isConnected)
                availablePoints.Add(point);
        }
        return availablePoints;
    }

    public int GetAvailableConnectionCount()
    {
        int count = 0;
        foreach (var point in connectionPoints)
        {
            if (!point.isConnected)
                count++;
        }
        return count;
    }
}