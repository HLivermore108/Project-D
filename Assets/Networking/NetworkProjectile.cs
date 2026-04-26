using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class NetworkProjectile : NetworkBehaviour
{
    public float speed = 22f;
    public int damage = 25;
    public float lifeTime = 2f;

    private static GameObject projectilePrefab;
    private static bool loggedMissingPrefab;
    private static bool loggedSpawnFailure;

    private Rigidbody rb;
    private ulong ownerClientId;
    private float lifeTimer;

    public static void SetPrefab(GameObject prefab)
    {
        projectilePrefab = prefab;
    }

    public static void Spawn(Vector3 position, Quaternion rotation, ulong shooterClientId)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        if (projectilePrefab == null)
        {
            if (!loggedMissingPrefab)
            {
                Debug.LogError("Cannot shoot: NetworkProjectile prefab is not registered on the server.");
                loggedMissingPrefab = true;
            }

            return;
        }

        GameObject projectileObject = null;
        try
        {
            projectileObject = Instantiate(projectilePrefab, position, rotation);
            NetworkProjectile projectile = projectileObject.GetComponent<NetworkProjectile>();
            NetworkObject networkObject = projectileObject.GetComponent<NetworkObject>();
            projectile.ownerClientId = shooterClientId;
            networkObject.SynchronizeTransform = false;
            networkObject.Spawn();
            projectile.Launch(rotation * Vector3.forward);
        }
        catch (System.Exception ex)
        {
            if (!loggedSpawnFailure)
            {
                Debug.LogError($"Cannot shoot: failed to spawn NetworkProjectile. {ex.Message}");
                loggedSpawnFailure = true;
            }

            if (projectileObject != null)
                Destroy(projectileObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        NetworkObject.SynchronizeTransform = false;
        NetworkTransform transformSync = GetComponent<NetworkTransform>();
        if (transformSync != null)
        {
            transformSync.SyncScaleX = false;
            transformSync.SyncScaleY = false;
            transformSync.SyncScaleZ = false;
        }

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = !IsServer;
        rb.detectCollisions = IsServer;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        if (!IsServer)
            return;

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
            Despawn();
    }

    private void Launch(Vector3 direction)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        rb.linearVelocity = direction.normalized * speed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;

        NetworkEnemy enemy = other.GetComponentInParent<NetworkEnemy>();
        if (enemy != null)
        {
            enemy.TakeDamageServer(damage, ownerClientId);
            Despawn();
            return;
        }

        if (other.CompareTag("Obstacle"))
            Despawn();
    }

    private void Despawn()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }
}
