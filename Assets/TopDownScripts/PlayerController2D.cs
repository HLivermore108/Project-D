using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;

    [Header("Shooting")]
    public Transform firePoint;
    public ObjectPool bulletPool;
    public float fireRate = 0.12f;

    [Header("Game Manager")]
    public ScoringHealth scoringHealth;   // drag your ScoringHealth object here

    [Header("Audio")]
    public AudioSource audioSource;       // one AudioSource on the player
    public AudioClip shootSound;          // sound when shooting
    public AudioClip hurtSound;           // sound when hit

    [Header("Hit Flash")]
    public Renderer playerRenderer;       // mesh renderer of your player model
    public Color flashColor = Color.red;
    public float flashDuration = 0.1f;

    private Rigidbody rb;
    private Vector3 moveInput;
    private float fireTimer;
    private Color originalColor;
    private bool hasOriginalColor = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (scoringHealth == null)
        {
#if UNITY_2023_2_OR_NEWER
            scoringHealth = FindFirstObjectByType<ScoringHealth>();
#else
            scoringHealth = FindObjectOfType<ScoringHealth>();
#endif
        }

        if (playerRenderer != null)
        {
            originalColor = playerRenderer.material.color;
            hasOriginalColor = true;
        }
    }

    void Update()
    {
        // Movement input (XZ)
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
        Vector3 newPos = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPos);
    }

    private void Shoot()
    {
        if (bulletPool == null || firePoint == null) return;

        // Play shoot sound
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        GameObject b = bulletPool.Get(firePoint.position, firePoint.rotation);
        var bullet = b.GetComponent<Bullet3D>();
        if (bullet != null)
        {
            bullet.Launch(firePoint.forward);
        }
    }

    public void TakeDamage(int dmg)
    {
        // Play hurt sound
        if (audioSource != null && hurtSound != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }

        // Flash red
        StartCoroutine(FlashDamage());

        // Talk to ScoringHealth for actual HP logic
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
            Debug.LogWarning("ScoringHealth not found; damage applied visually but not tracked.");
        }
    }

    private IEnumerator FlashDamage()
    {
        if (playerRenderer == null || !hasOriginalColor)
            yield break;

        // Set to flash color
        playerRenderer.material.color = flashColor;

        // Wait a short time
        yield return new WaitForSeconds(flashDuration);

        // Restore original color
        playerRenderer.material.color = originalColor;
    }
}
