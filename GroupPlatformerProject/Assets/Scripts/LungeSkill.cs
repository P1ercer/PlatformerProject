using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LungeSkill : MonoBehaviour
{
    [Header("Lunge Settings")]
    public float lungeDistance = 5f;
    public float lungeSpeed = 20f;
    public float lungeDamage = 30f;
    public float lungeManaCost = 30f;
    public float lungeCooldown = 4f;

    [Header("Jump Settings")]
    public float jumpForce = 8f;

    [Header("References")]
    public GameObject lungeHitbox;  // Assign in Inspector

    // -------------------------------
    // Animation Fields
    // -------------------------------
    [Header("Animation Settings")]
    [Tooltip("Animator component for playing the lunge animation.")]
    public Animator animator;

    [Tooltip("Trigger name for lunge animation (optional).")]
    private string lungeTrigger = "Lunge";

    [Tooltip("Optional animation clip to play instead of a trigger.")]
    public AnimationClip lungeAnimationClip;

    // -------------------------------
    // Private Fields
    // -------------------------------
    private bool isLunging = false;
    private float lungeCooldownTimer = 0f;
    private Vector2 lungeTargetPosition;
    private Rigidbody2D rb;
    private PlayerController playerController;
    private bool facingRight = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();

        if (playerController == null)
            Debug.LogError("LungeSkill requires PlayerController component on the same GameObject.");

        if (lungeHitbox != null)
            lungeHitbox.SetActive(false);
        else
            Debug.LogWarning("LungeHitbox reference not set in LungeSkill.");

        // Auto-assign animator if missing
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (lungeCooldownTimer > 0)
            lungeCooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.LeftShift) && !isLunging && lungeCooldownTimer <= 0)
            TryStartLunge();
    }

    void TryStartLunge()
    {
        if (playerController == null) return;

        if (playerController.currentMana >= lungeManaCost)
        {
            playerController.SpendMana(lungeManaCost);
            playerController.UpdateManaUI();
            StartLunge();
        }
        else
        {
            Debug.Log("Not enough mana to lunge!");
        }
    }

    void StartLunge()
    {
        isLunging = true;
        lungeCooldownTimer = lungeCooldown;
        facingRight = playerController.transform.localScale.x >= 0;

        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        lungeTargetPosition = (Vector2)transform.position + direction * lungeDistance;

        if (lungeHitbox != null)
            lungeHitbox.SetActive(true);

        PlayLungeAnimation();
    }

    void FixedUpdate()
    {
        if (isLunging)
            PerformLunge();
    }

    void PerformLunge()
    {
        Vector2 currentPosition = rb.position;
        Vector2 newPosition = Vector2.MoveTowards(currentPosition, lungeTargetPosition, lungeSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);

        if (Vector2.Distance(newPosition, lungeTargetPosition) < 0.1f)
        {
            isLunging = false;

            if (lungeHitbox != null)
                lungeHitbox.SetActive(false);

            // Small jump after lunge finishes
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    public void ApplyLungeDamage(Collider2D target)
    {
        //just copy and paste the same bit but change the health script name
        EnemyHealth enemy = target.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage((int)lungeDamage);
            Debug.Log($"LungeSkill applied {lungeDamage} damage to {enemy.name}");
            return;
        }

        KnightHealth KnightBoss = target.GetComponent<KnightHealth>();
        if (KnightBoss != null)
        {
            KnightBoss.TakeDamage((int)lungeDamage);
            Debug.Log($"LungeSkill applied {lungeDamage} damage to {KnightBoss.name}");
        }
    }

    // ---------------------------------------
    // Animation Helper
    // ---------------------------------------
    void PlayLungeAnimation()
    {
        if (animator == null) return;

        if (!string.IsNullOrEmpty(lungeTrigger))
        {
            animator.SetTrigger(lungeTrigger);
        }
        else if (lungeAnimationClip != null)
        {
            animator.Play(lungeAnimationClip.name);
        }
    }
}
