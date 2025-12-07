using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public GameObject prefab;
    public int initialSize = 20;
    private Queue<GameObject> pool = new Queue<GameObject>();

    void Awake()
    {
        for (int i = 0; i < initialSize; i++) AddOne();
    }

    GameObject AddOne()
    {
        var go = Instantiate(prefab, transform);
        go.SetActive(false);
        pool.Enqueue(go);
        return go;
    }

    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        if (pool.Count == 0) AddOne();
        var go = pool.Dequeue();
        go.transform.position = position;
        go.transform.rotation = rotation;
        go.SetActive(true);
        return go;
    }

    public void ReturnToPool(GameObject go)
    {
        go.SetActive(false);
        pool.Enqueue(go);
    }
}
