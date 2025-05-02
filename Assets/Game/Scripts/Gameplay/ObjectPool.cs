using System.Collections.Generic;
using UnityEngine;

public class ObjectPool //for pooling GameObjects for reuse
{
    private GameObject prefab;
    private Queue<GameObject> availableObjects = new Queue<GameObject>();
    private Transform parent;

    //constructor to initialize the pool with a prefab and an optional initial size
    public ObjectPool(GameObject prefab, int initialSize = 0, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            var obj = CreateNewObject();
            availableObjects.Enqueue(obj);
        }
    }

    private GameObject CreateNewObject()
    {
        var obj = GameObject.Instantiate(prefab, parent);
        obj.SetActive(false);
        return obj;
    }

    public GameObject GetObject(Vector3 position)
    {
        GameObject obj;

        if (availableObjects.Count > 0)
        {
            obj = availableObjects.Dequeue();
        }
        else
        {
            obj = CreateNewObject();
        }

        obj.transform.position = position;
        obj.SetActive(true);
        return obj;
    }

    public void ReturnObject(GameObject obj)
    {
        obj.SetActive(false);
        availableObjects.Enqueue(obj);
    }

    public IEnumerable<GameObject> GetAllPooledObjects()
    {
        return availableObjects;
    }
}
