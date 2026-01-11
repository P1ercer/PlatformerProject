using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MageAi : MonoBehaviour
{
    [Header("Bullet Settings")]
    public GameObject prefab;          // Bullet prefab
    public float shootSpeed = 10f;     // Bullet speed
    public float bulletLifetime = 2f;  // Lifetime of bullets

    [Header("AI Settings")]
    public float shootDelay = 0.5f;          // Time between shots
    public float shootTriggerDistance = 5f;  // Distance to start shooting

    [Header("Combat Settings")]
    public int damage = 10;   // <<< Editable damage

    [Header("Animation Settings")]
    [Tooltip("Animator component for playing shoot animation.")]
    public Animator animator;

    [Tooltip("Trigger name for shoot animation.")]
    private string shootTrigger = "Shoot";

    private float timer = 0f;
    private GameObject player;

    // Track bullets manually
    private List<GameObject> bullets = new List<GameObject>();
    private List<float> bulletTimers = new List<float>();

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        // Auto-assign animator if missing
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (player == null || prefab == null) return;

        timer += Time.deltaTime;

        // Shoot at player if in range
        Vector3 shootDir = player.transform.position - transform.position;
        if (shootDir.magnitude < shootTriggerDistance && timer >= shootDelay)
        {
            Shoot(shootDir);
            PlayShootAnimation();  // Play animation when shooting
            timer = 0f;
        }

        // Update bullets
        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            GameObject bullet = bullets[i];
            if (bullet == null)
            {
                bullets.RemoveAt(i);
                bulletTimers.RemoveAt(i);
                continue;
            }

            // Update bullet timer
            bulletTimers[i] += Time.deltaTime;
            if (bulletTimers[i] >= bulletLifetime)
            {
                Destroy(bullet);
                bullets.RemoveAt(i);
                bulletTimers.RemoveAt(i);
                continue;
            }

            // Check for collision with player, wall, or obstacle
            Collider2D hit = Physics2D.OverlapCircle(bullet.transform.position, 0.1f);
            if (hit != null)
            {
                if (hit.CompareTag("Player"))
                {
                    // apply damage here
                    // hit.GetComponent<PlayerHealth>().TakeDamage(damage);

                }

                if (hit.CompareTag("Player") || hit.CompareTag("Wall") || hit.CompareTag("Obstacle"))
                {
                    Destroy(bullet);
                    bullets.RemoveAt(i);
                    bulletTimers.RemoveAt(i);
                }
            }
        }
    }

    void Shoot(Vector3 direction)
    {
        direction.Normalize();

        GameObject bullet = Instantiate(prefab, transform.position, Quaternion.identity);

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = bullet.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
        }
        rb.velocity = direction * shootSpeed;

        Collider2D col = bullet.GetComponent<Collider2D>();
        if (col == null)
        {
            CircleCollider2D circle = bullet.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
        }

        bullets.Add(bullet);
        bulletTimers.Add(0f);
    }

    // ---------------------------------------
    // Animation Helper
    // ---------------------------------------
    void PlayShootAnimation()
    {
        if (animator == null || string.IsNullOrEmpty(shootTrigger)) return;

        animator.SetTrigger(shootTrigger);
    }
}
