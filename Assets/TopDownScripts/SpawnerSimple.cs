using UnityEngine;

public class SpawnerSimple : MonoBehaviour
{
    public ObjectPool enemyPool;
    public float spawnInterval = 1.2f;
    public float spawnDistance = 10f; // distance from player to spawn
    public int maxEnemies = 20;

    float timer;
    Transform player;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        player = (p != null) ? p.transform : null;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            TrySpawn();
        }
    }

    void TrySpawn()
    {
        if (enemyPool == null || player == null) return;
        // quick count active enemies (optional)
        // spawn at random angle at distance
        Vector2 spawnPos = (Vector2)player.position + Random.insideUnitCircle.normalized * spawnDistance;
        var go = enemyPool.Get(spawnPos, Quaternion.identity);
        // if you need to initialize enemy speed/health here, get component and set values
    }
}
