using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Transform firePoint;
    public ObjectPool bulletPool;
    public float fireRate = 0.15f;
    public int maxHealth = 100;

    Rigidbody2D rb;
    Vector2 moveInput;
    float fireTimer;
    int currentHealth;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
    }

    void Update()
    {
        // Movement input
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        // Aim: rotate sprite towards mouse (optional: set a separate turret sprite to rotate)
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 aimDir = (mouseWorld - transform.position);
        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Shooting
        fireTimer += Time.deltaTime;
        if (Input.GetMouseButton(0) && fireTimer >= fireRate)
        {
            Shoot();
            fireTimer = 0f;
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    void Shoot()
    {
        if (bulletPool == null || firePoint == null) return;
        var b = bulletPool.Get(firePoint.position, firePoint.rotation);
        var bullet = b.GetComponent<Bullet2D>();
        if (bullet != null) bullet.Launch(transform.right); // transform.right points to angle 0 of rotation
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
        // basic: disable player; expand with respawn or game over screen
        gameObject.SetActive(false);
        Debug.Log("Player died - prototype over");
    }
}
