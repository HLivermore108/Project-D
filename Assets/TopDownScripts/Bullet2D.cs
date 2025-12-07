using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet2D : MonoBehaviour
{
    public float speed = 12f;
    public int damage = 25;
    public float lifeTime = 2f;
    public ObjectPool returnPool;

    Rigidbody2D rb;
    float lifeTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        lifeTimer = 0f;
    }

    public void Launch(Vector2 direction)
    {
        direction = direction.normalized;
        rb.linearVelocity = direction * speed;
    }

    void FixedUpdate()
    {
        // nothing else here; physics handles movement
    }

    void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime) Return();
    }

    void OnTriggerEnter2D(Collider2D other)
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

    void Return()
    {
        rb.linearVelocity = Vector2.zero;
        if (returnPool != null) returnPool.ReturnToPool(this.gameObject);
        else Destroy(gameObject);
    }
}
