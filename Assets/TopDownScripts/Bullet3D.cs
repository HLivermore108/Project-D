using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet3D : MonoBehaviour
{
    public float speed = 22f;
    public int damage = 25;
    public float lifeTime = 2f;
    public ObjectPool returnPool;

    private Rigidbody rb;
    private float lifeTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void OnEnable()
    {
        lifeTimer = 0f;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void Launch(Vector3 direction)
    {
        if (rb == null) return;

        direction.Normalize();
        rb.linearVelocity = direction * speed;
    }

    void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
        {
            Return();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            var e = other.GetComponent<EnemySeek2D>();
            if (e != null) e.TakeDamage(damage);
            Return();
        }
        else if (other.CompareTag("Obstacle"))
        {
            Return();
        }
    }

    private void Return()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (returnPool != null)
            returnPool.ReturnToPool(gameObject);
        else
            Destroy(gameObject);
    }
}
