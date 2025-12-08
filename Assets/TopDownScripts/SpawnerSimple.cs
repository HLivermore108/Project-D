using UnityEngine;

public class SpawnerSimple : MonoBehaviour
{
    public ObjectPool enemyPool;
    public float spawnInterval = 1.2f;
    public float spawnDistance = 12f;
    public int maxEnemies = 30;
    public LayerMask groundLayer = ~0;
    public float raycastHeight = 50f;
    public float groundYFallback = 0.5f;

    float timer;
    Transform player;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
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

        // Count active enemies
        int activeCount = 0;
        foreach (Transform child in enemyPool.transform)
        {
            if (child.gameObject.activeSelf) activeCount++;
        }
        if (activeCount >= maxEnemies) return;

        // Pick random angle around player
        Vector2 rnd = Random.insideUnitCircle.normalized;
        Vector3 candidate = player.position + new Vector3(rnd.x, 0f, rnd.y) * spawnDistance;

        // Snap to ground
        Vector3 rayStart = candidate + Vector3.up * raycastHeight;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayer))
        {
            candidate.y = hit.point.y + 0.05f;
        }
        else
        {
            candidate.y = groundYFallback;
        }

        enemyPool.Get(candidate, Quaternion.identity);
    }
}
