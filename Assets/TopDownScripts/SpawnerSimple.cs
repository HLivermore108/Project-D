using UnityEngine;

public class SpawnerSimple3D : MonoBehaviour
{
    [Header("Pools")]
    public ObjectPool enemyPool;

    [Header("Spawn timing")]
    public float spawnInterval = 1.2f;
    public float spawnDistance = 12f;       // horizontal radius from player
    public int maxEnemies = 30;             // cap for active enemies

    [Header("Ground snapping")]
    public LayerMask groundLayer = ~0;      // which layers count as ground (default = everything)
    public float raycastHeight = 50f;       // how high above spawn to raycast down from
    public float groundYFallback = 0.5f;    // fallback Y if ray misses

    [Header("Overlap avoidance")]
    public float spawnRadius = 0.5f;        // approx enemy collider radius
    public int maxPlacementAttempts = 6;    // attempts around a candidate to find free spot
    public float placementNudge = 0.6f;     // random nudge range for retries

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

        // check active enemy count to respect maxEnemies
        int activeCount = 0;
        foreach (Transform child in enemyPool.transform)
        {
            if (child.gameObject.activeSelf) activeCount++;
        }
        if (activeCount >= maxEnemies) return;

        // choose base candidate position around player on XZ
        Vector2 rnd = Random.insideUnitCircle.normalized;
        Vector3 candidate = player.position + new Vector3(rnd.x, 0f, rnd.y) * spawnDistance;

        // snap candidate to ground (single attempt)
        Vector3 rayStart = candidate + Vector3.up * raycastHeight;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayer))
        {
            candidate.y = hit.point.y + 0.05f;
        }
        else
        {
            candidate.y = groundYFallback;
        }

        // try multiple placement attempts to avoid overlaps
        bool placed = false;
        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            Vector3 tryPos = candidate;
            if (attempt > 0)
            {
                tryPos += new Vector3(Random.Range(-placementNudge, placementNudge), 0f, Random.Range(-placementNudge, placementNudge));
                // resnap small vertical differences
                Vector3 rs = tryPos + Vector3.up * raycastHeight;
                if (Physics.Raycast(rs, Vector3.down, out RaycastHit hit2, raycastHeight * 2f, groundLayer))
                {
                    tryPos.y = hit2.point.y + 0.05f;
                }
                else
                {
                    tryPos.y = groundYFallback;
                }
            }

            // overlap check
            Collider[] overlap = Physics.OverlapSphere(tryPos, spawnRadius);
            bool anyBlocking = false;
            foreach (var c in overlap)
            {
                if (c == null) continue;
                if (c.isTrigger) continue; // ignore triggers
                // if collider belongs to an active pool child (rare), ignore that; otherwise treat as blocking
                anyBlocking = true;
                break;
            }

            if (!anyBlocking)
            {
                var go = enemyPool.Get(tryPos, Quaternion.identity);
                placed = true;
                break;
            }
        }

        if (!placed)
        {
            // fallback: spawn at candidate but lift it slightly to reduce initial intersections
            Vector3 fallback = candidate;
            fallback.y += 0.5f;
            enemyPool.Get(fallback, Quaternion.identity);
        }
    }

    // optional gizmo to visualize spawn radius in editor
    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position + new Vector3(0f, 0f, 0f), spawnDistance);
        }
    }
}
