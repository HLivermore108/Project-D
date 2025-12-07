using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    [Header("References")]
    private ScoringHealth scoringHealth;

    [Header("Sound Effects")]
    public AudioSource collectibleSound;
    public AudioSource hitSound;

    private void Awake()
    {
        // Try to find ScoringHealth in scene
        scoringHealth = FindFirstObjectByType<ScoringHealth>();
        if (scoringHealth == null)
            Debug.LogError("No ScoringHealth component found in scene! Make sure one exists.");
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleCollision(other.gameObject);
    }

    private void HandleCollision(GameObject obj)
    {
        if (scoringHealth == null)
            return;

        if (obj.CompareTag("Obstacle"))
        {
            Debug.Log("Hit an obstacle: " + obj.name);
            scoringHealth.TakeDamage(20);

            if (hitSound != null)
                hitSound.Play();
        }
        else if (obj.CompareTag("Collectible"))
        {
            Debug.Log("Collected: " + obj.name);
            scoringHealth.AddScore(10);

            if (collectibleSound != null)
                collectibleSound.Play();

            Destroy(obj);
        }
    }
}
