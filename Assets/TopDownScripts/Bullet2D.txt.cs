using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet3D : MonoBehaviour
{
    public float speed = 22f;
    public int damage = 25;
    public float lifeTime = 2f;
    public ObjectPool returnPool;

    Rigidbody rb;
    float lifeTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null) rb.useGravity = false;
    }

    void OnEnable()
    {
        lifeTimer = 0f;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    public void Launch(Vector3 direction)
    {
        direction = direction.normalized;
        if (rb != null) rb.linearVelocity = direction * speed;
    }

    void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime) Return();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            var e = other.GetComponent<EnemySeek3D>();
            if (e != null) e.TakeDamage(damage);
            Return();
        }
        else if (other.CompareTag("Obstacle"))
        {
            Return();
        }
    }

    void Return()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        if (returnPool != null) returnPool.ReturnToPool(gameObject);
        else Destroy(gameObject);
    }
}
