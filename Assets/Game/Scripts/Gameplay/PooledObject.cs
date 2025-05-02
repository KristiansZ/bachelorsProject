using UnityEngine;

public class PooledObject : MonoBehaviour
{
    private DungeonEnemyManager spawnManager;
    private bool isReturning = false;

    public void SetSpawnManager(DungeonEnemyManager manager)
    {
        spawnManager = manager;
    }

    public void ReturnToPool()
    {
        if (isReturning) return;
        isReturning = true;
        
        if (spawnManager != null)
        {
            spawnManager.ReturnEnemyToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        isReturning = false;
    }
    
    private void OnDisable()
    {
        isReturning = false;
    }
}