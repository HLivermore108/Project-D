using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController2D : MonoBehaviour
{
    public float moveSpeed = 6f;
    public Transform firePoint;
    public ObjectPool bulletPool;
    public float fireRate = 0.12f;

    public ScoringHealth scoringHealth; // hook up in Inspector or auto-find

    private Rigidbody rb;
    private Vector3 moveInput;
    private float fireTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Auto-assign ScoringHealth if not set
        if (scoringHealth == null)
        {
#if UNITY_2023_2_OR_NEWER
            scoringHealth = FindFirstObjectByType<ScoringHealth>();
#else
            scoringHealth = FindObjectOfType<ScoringHealth>();
#endif
        }
    }

    void Update()
    {
        // Movement input on XZ
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(h, 0f, v).normalized;

        // Aim with mouse using raycast to ground
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Vector3 lookDir = hit.point - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        fireTimer += Time.deltaTime;
        if (Input.GetMouseButton(0) && fireTimer >= fireRate)
        {
            Shoot();
            fireTimer = 0f;
        }
    }

    void FixedUpdate()
    {
        Vector3 newPos = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPos);
    }

    private void Shoot()
    {
        if (bulletPool == null || firePoint == null) return;

        GameObject b = bulletPool.Get(firePoint.position, firePoint.rotation);
        var bullet = b.GetComponent<Bullet3D>();
        if (bullet != null)
        {
            // shoot along firePoint forward
            bullet.Launch(firePoint.forward);
        }
    }

    public void TakeDamage(int dmg)
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
        {
            scoringHealth.TakeDamage(dmg);
            if (scoringHealth.IsGameOver())
            {
                gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning("ScoringHealth not found; damage ignored.");
        }
    }
}
