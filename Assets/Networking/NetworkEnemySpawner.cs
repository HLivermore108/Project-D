using Unity.Netcode;
using UnityEngine;

public class NetworkEnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 1.5f;
    public int maxEnemies = 20;
    public float spawnRadius = 14f;

    private float timer;

    private void Update()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer || enemyPrefab == null)
            return;

        timer += Time.deltaTime;
        if (timer < spawnInterval || CountEnemies() >= maxEnemies)
            return;

        timer = 0f;
        SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        Vector2 circle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 spawnPosition = new Vector3(circle.x, 1f, circle.y);
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        enemy.GetComponent<NetworkObject>().Spawn();
    }

    private int CountEnemies()
    {
        return FindObjectsByType<NetworkEnemy>(FindObjectsSortMode.None).Length;
    }
}
