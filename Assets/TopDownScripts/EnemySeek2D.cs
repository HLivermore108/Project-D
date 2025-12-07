using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemySeek2D : MonoBehaviour
{
    public float speed = 2.5f;
    public int maxHealth = 50;
    public int contactDamage = 10;
    public ObjectPool returnPool; // to return to pool on death
    Transform player;
    Rigidbody2D rb;
    int currentHealth;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        currentHealth = maxHealth;
        // cache player reference (assumes tag "Player")
        var p = GameObject.FindGameObjectWithTag("Player");
        player = (p != null) ? p.transform : null;
    }

    void FixedUpdate()
    {
        if (player == null) return;
        Vector2 dir = (player.position - transform.position).normalized;
        Vector2 newPos = rb.position + dir * speed * Time.fixedDeltaTime;
        rb.MovePosition(newPos);

        // Optional: rotate sprite to face player
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void TakeDamage(int dmg)
    {
        currentHealth -= dmg;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // play VFX/sfx here if desired
        if (returnPool != null) returnPool.ReturnToPool(this.gameObject);
        else Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            var pc = collision.collider.GetComponent<PlayerController2D>();
            if (pc != null) pc.TakeDamage(contactDamage);
            // knockback or short delay could be added
        }
    }
}
