using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class NetworkEnemy : NetworkBehaviour
{
    [Header("Movement")]
    public float speed = 3f;
    public float rotationSpeed = 10f;

    [Header("Combat")]
    public int maxHealth = 50;
    public int contactDamage = 10;
    public int scoreValue = 1;

    [Header("Avoidance")]
    public float neighborRadius = 1.2f;
    public float repulsionStrength = 2.0f;
    public LayerMask allyLayer = 1 << 8;
    public float obstacleDetectDistance = 1.2f;
    public float obstacleAvoidStrength = 4.0f;
    public LayerMask obstacleLayers = ~0;
    public float jitterStrength = 0.25f;

    private Rigidbody rb;
    private int currentHealth;
    private float seed;
    private ulong lastAttackerClientId = NetworkManager.ServerClientId;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = !IsServer;
        rb.detectCollisions = IsServer;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (IsServer)
        {
            currentHealth = maxHealth;
            seed = Random.value * 1000f;
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer || rb == null)
            return;

        NetworkPlayerController2D target = FindTarget();
        if (target == null)
            return;

        Vector3 desired = target.transform.position - transform.position;
        desired.y = 0f;
        if (desired.sqrMagnitude < 0.0001f)
            return;

        desired.Normalize();
        Vector3 moveDir = (desired + GetRepulsion() + GetObstacleAvoidance() + GetJitter()).normalized;
        if (moveDir.sqrMagnitude < 0.0001f)
            moveDir = desired;

        rb.MovePosition(rb.position + moveDir * speed * Time.fixedDeltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), rotationSpeed * Time.fixedDeltaTime);
    }

    public void TakeDamageServer(int damage, ulong attackerClientId)
    {
        if (!IsServer)
            return;

        lastAttackerClientId = attackerClientId;
        currentHealth -= Mathf.Max(0, damage);
        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        NetworkPlayerController2D attacker = NetworkPlayerController2D.FindByClientId(lastAttackerClientId);
        if (attacker != null)
            attacker.AddScoreServer(scoreValue);

        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer)
            return;

        NetworkPlayerController2D player = collision.collider.GetComponentInParent<NetworkPlayerController2D>();
        if (player != null)
            player.TakeDamageServer(contactDamage);
    }

    private NetworkPlayerController2D FindTarget()
    {
        NetworkPlayerController2D closest = null;
        float closestDistance = float.MaxValue;
        NetworkPlayerController2D[] players = FindObjectsByType<NetworkPlayerController2D>(FindObjectsSortMode.None);

        foreach (NetworkPlayerController2D player in players)
        {
            if (player.IsEliminated.Value)
                continue;

            float distance = (player.transform.position - transform.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = player;
            }
        }

        return closest;
    }

    private Vector3 GetRepulsion()
    {
        Vector3 repulsion = Vector3.zero;
        Collider[] hits = Physics.OverlapSphere(transform.position, neighborRadius, allyLayer);
        foreach (Collider col in hits)
        {
            if (!col || col.gameObject == gameObject || col.isTrigger)
                continue;

            Vector3 toOther = transform.position - col.transform.position;
            toOther.y = 0f;
            float dist = toOther.magnitude;
            if (dist > 0.001f)
                repulsion += toOther.normalized * (repulsionStrength / Mathf.Max(0.25f, dist));
        }

        float maxRepelMag = repulsionStrength * 1.5f;
        return repulsion.magnitude > maxRepelMag ? repulsion.normalized * maxRepelMag : repulsion;
    }

    private Vector3 GetObstacleAvoidance()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (!Physics.Raycast(transform.position + Vector3.up * 0.2f, forward, out RaycastHit hit, obstacleDetectDistance, obstacleLayers))
            return Vector3.zero;

        Vector3 steer = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;
        Vector3 obstacleAvoid = steer * obstacleAvoidStrength;
        return obstacleAvoid.magnitude > obstacleAvoidStrength * 1.5f
            ? obstacleAvoid.normalized * obstacleAvoidStrength * 1.5f
            : obstacleAvoid;
    }

    private Vector3 GetJitter()
    {
        float t = Time.time * 1.5f + seed;
        Vector2 n = new Vector2(Mathf.PerlinNoise(t, seed) - 0.5f, Mathf.PerlinNoise(seed, t) - 0.5f);
        return new Vector3(n.x, 0f, n.y) * jitterStrength;
    }
}
