using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject obstaclePrefab;
    public GameObject collectiblePrefab;
    public GameObject treePrefab;

    [Header("Spawn Settings")]
    public float obstacleSpawnRateMin = 1.5f;
    public float obstacleSpawnRateMax = 3f;
    public float collectibleSpawnRateMin = 2f;
    public float collectibleSpawnRateMax = 4f;
    public float treeSpawnRateMin = 1f;
    public float treeSpawnRateMax = 2f;

    [Header("Spawn Positions")]
    public float spawnZ = 50f;
    public float laneWidth = 3f;
    public float xOffsetRange = 0.5f;

    [Header("Y Heights")]
    public float obstacleY = 0.5f;
    public float collectibleY = 2f;
    public float treeY = 0f;

    private float nextObstacleTime;
    private float nextCollectibleTime;
    private float nextTreeTime;

    void Start()
    {
        // Initialize random spawn times
        nextObstacleTime = Time.time + Random.Range(obstacleSpawnRateMin, obstacleSpawnRateMax);
        nextCollectibleTime = Time.time + Random.Range(collectibleSpawnRateMin, collectibleSpawnRateMax);
        nextTreeTime = Time.time + Random.Range(treeSpawnRateMin, treeSpawnRateMax);

        PreventOverlap();
    }

    void Update()
    {
        float currentTime = Time.time;

        // Spawn obstacles
        if (currentTime >= nextObstacleTime)
        {
            SpawnObstacle();
            nextObstacleTime = currentTime + Random.Range(obstacleSpawnRateMin, obstacleSpawnRateMax);
            PreventOverlap();
        }

        // Spawn collectibles
        if (currentTime >= nextCollectibleTime)
        {
            SpawnCollectible();
            nextCollectibleTime = currentTime + Random.Range(collectibleSpawnRateMin, collectibleSpawnRateMax);
            PreventOverlap();
        }

        // Spawn trees
        if (currentTime >= nextTreeTime)
        {
            SpawnTree();
            nextTreeTime = currentTime + Random.Range(treeSpawnRateMin, treeSpawnRateMax);
            PreventOverlap();
        }
    }

    void PreventOverlap()
    {
        // Make sure spawn times aren't within 0.1 seconds of each other
        if (Mathf.Abs(nextObstacleTime - nextCollectibleTime) < 0.1f)
            nextCollectibleTime += 0.3f;
        if (Mathf.Abs(nextObstacleTime - nextTreeTime) < 0.1f)
            nextTreeTime += 0.3f;
        if (Mathf.Abs(nextCollectibleTime - nextTreeTime) < 0.1f)
            nextTreeTime += 0.3f;
    }

    void SpawnObstacle()
    {
        int lane = Random.Range(-1, 2);
        float xOffset = Random.Range(-xOffsetRange, xOffsetRange);
        Vector3 pos = new Vector3(lane * laneWidth + xOffset, obstacleY, spawnZ);
        Instantiate(obstaclePrefab, pos, Quaternion.identity);
    }

    void SpawnCollectible()
    {
        int lane = Random.Range(-1, 2);
        float xOffset = Random.Range(-xOffsetRange, xOffsetRange);
        Vector3 pos = new Vector3(lane * laneWidth + xOffset, collectibleY, spawnZ);
        Instantiate(collectiblePrefab, pos, Quaternion.identity);
    }

    void SpawnTree()
    {
        // Trees typically appear off to the side of the road
        float side = Random.value > 0.5f ? 1 : -1; // Left or right side
        float xOffset = side * (laneWidth * 2 + Random.Range(0, 1f)); // offset further out
        Vector3 pos = new Vector3(xOffset, treeY, spawnZ);
        Instantiate(treePrefab, pos, Quaternion.identity);
    }
}
