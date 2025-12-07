using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemySeek2D : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3f;
    public float rotationSpeed = 10f;

    [Header("Combat")]
    public int maxHealth = 50;
    public int contactDamage = 10;

    [Header("Avoidance")]
    public float neighborRadius = 1.2f;
    public float repulsionStrength = 2.0f;
    public LayerMask allyLayer = 1 << 8; // set to your Enemy layer

    [Header("Obstacle Avoidance")]
    public float obstacleDetectDistance = 1.2f;
    public float obstacleAvoidStrength = 4.0f;
    public LayerMask obstacleLayers = ~0;

    [Header("Jitter")]
    public float jitterStrength = 0.25f;

    [Header("Pooling & Score")]
    public ObjectPool returnPool;
    public int scoreValue = 1;

    private Transform player;
    private Rigidbody rb;
    private int currentHealth;
    private float seed;
    private ScoringHealth scoringHealth;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        seed = Random.value * 1000f;

#if UNITY_2023_2_OR_NEWER
        scoringHealth = FindFirstObjectByType<ScoringHealth>();
#else
        scoringHealth = FindObjectOfType<ScoringHealth>();
#endif
    }

    void OnEnable()
    {
        currentHealth = maxHealth;

        var p = GameObject.FindGameObjectWithTag("Player");
        player = (p != null) ? p.transform : null;

        // Reset velocity and snap to ground
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (Physics.Raycast(transform.position + Vector3.up * 1f, Vector3.down, out RaycastHit h, 5f))
        {
            Vector3 pos = transform.position;
            pos.y = h.point.y + 0.05f;
            rb.position = pos;
            transform.position = pos;
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;

        // 1) Desired direction toward player (XZ)
        Vector3 desired = player.position - transform.position;
        desired.y = 0f;
        if (desired.sqrMagnitude < 0.0001f) return;
        desired.Normalize();

        // 2) Repulsion from nearby allies
        Vector3 repulsion = Vector3.zero;
        Collider[] hits = Physics.OverlapSphere(transform.position, neighborRadius, allyLayer);
        foreach (Collider col in hits)
        {
            if (!col || col.gameObject == gameObject || col.isTrigger) continue;

            Vector3 toOther = transform.position - col.transform.position;
            toOther.y = 0f;
            float dist = toOther.magnitude;
            if (dist > 0.001f)
            {
                repulsion += toOther.normalized * (repulsionStrength / Mathf.Max(0.25f, dist));
            }
        }

        float maxRepelMag = repulsionStrength * 1.5f;
        if (repulsion.magnitude > maxRepelMag)
            repulsion = repulsion.normalized * maxRepelMag;

        // 3) Obstacle avoidance
        Vector3 forward = transform.forward;
        forward.y = 0f;
        Vector3 obstacleAvoid = Vector3.zero;
        if (Physics.Raycast(transform.position + Vector3.up * 0.2f, forward, out RaycastHit hit, obstacleDetectDistance, obstacleLayers))
        {
            Vector3 steer = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;
            obstacleAvoid = steer * obstacleAvoidStrength;
        }
        if (obstacleAvoid.magnitude > obstacleAvoidStrength * 1.5f)
            obstacleAvoid = obstacleAvoid.normalized * obstacleAvoidStrength * 1.5f;

        // 4) Jitter
        float t = Time.time * 1.5f + seed;
        Vector2 n = new Vector2(Mathf.PerlinNoise(t, seed) - 0.5f, Mathf.PerlinNoise(seed, t) - 0.5f);
        Vector3 jitter = new Vector3(n.x, 0f, n.y) * jitterStrength;

        // 5) Combine forces
        Vector3 moveDir = desired + repulsion + obstacleAvoid + jitter;
        moveDir.y = 0f;
        if (moveDir.sqrMagnitude > 0.0001f)
            moveDir.Normalize();
        else
            moveDir = desired;

        // Limit per-frame displacement
        float maxStep = speed * 2.2f * Time.fixedDeltaTime;
        Vector3 attemptedMove = moveDir * speed * Time.fixedDeltaTime;
        if (attemptedMove.magnitude > maxStep)
            attemptedMove = attemptedMove.normalized * maxStep;

        Vector3 newPos = rb.position + attemptedMove;
        rb.MovePosition(newPos);

        // Rotate toward movement direction
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    public void TakeDamage(int dmg)
    {
        currentHealth -= dmg;
        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (scoringHealth == null)
        {
#if UNITY_2023_2_OR_NEWER
            scoringHealth = FindFirstObjectByType<ScoringHealth>();
#else
            scoringHealth = FindObjectOfType<ScoringHealth>();
#endif
        }

        if (scoringHealth != null)
            scoringHealth.AddScore(scoreValue);

        if (returnPool != null)
            returnPool.ReturnToPool(gameObject);
        else
            Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            var pc = collision.collider.GetComponent<PlayerController2D>();
            if (pc != null) pc.TakeDamage(contactDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, neighborRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * obstacleDetectDistance);
    }
}
