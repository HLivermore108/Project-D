using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public GameObject prefab;
    public int initialSize = 20;

    private readonly Queue<GameObject> pool = new Queue<GameObject>();

    void Awake()
    {
        for (int i = 0; i < initialSize; i++)
        {
            AddOne();
        }
    }

    private GameObject AddOne()
    {
        GameObject go = Instantiate(prefab, transform);
        go.SetActive(false);

        // Auto-set returnPool for known components
        var b3 = go.GetComponent<Bullet3D>();
        if (b3 != null) b3.returnPool = this;

        var e3 = go.GetComponent<EnemySeek2D>(); // your enemy script, now 3D
        if (e3 != null) e3.returnPool = this;

        pool.Enqueue(go);
        return go;
    }

    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        if (pool.Count == 0)
            AddOne();

        GameObject go = pool.Dequeue();
        go.transform.position = position;
        go.transform.rotation = rotation;
        go.SetActive(true);

        // Reset 3D rigidbody if present
        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;      // modern API (no warning)
            rb.angularVelocity = Vector3.zero;
        }

        return go;
    }

    public void ReturnToPool(GameObject go)
    {
        if (!go) return;

        go.SetActive(false);
        go.transform.SetParent(transform, false);
        pool.Enqueue(go);
    }
}
