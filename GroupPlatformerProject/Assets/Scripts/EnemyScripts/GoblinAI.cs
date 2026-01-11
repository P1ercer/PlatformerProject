using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoblinAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float chaseTriggerDistance = 5f;
    public bool returnHome = true;

    [Header("Patrol Settings")]
    public bool patrol = true;
    public Vector3 patrolDirection = Vector3.right;
    public float patrolDistance = 3f;
    public float groundCheckDistance = 0.5f;

    [Header("Attack Settings")]
    public int damage = 10;
    public float attackRange = 1f; // Distance in front of goblin to hit
    public LayerMask enemyLayers;
    public float attackCooldown = 1f;

    [Header("Animation Settings")]
    [Tooltip("Animator component for playing attack animation.")]
    public Animator animator;

    [Tooltip("Trigger name for attack animation.")]
    private string attackTrigger = "Attack";

    private GameObject player;
    private Rigidbody2D rb;
    private Vector3 home;
    private bool isHome = true;
    private bool isReturningHome = false;
    private float lastAttackTime = -Mathf.Infinity;
    private int facingDirection = 1; // 1 = right, -1 = left

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        rb = GetComponent<Rigidbody2D>();
        home = transform.position;

        rb.freezeRotation = true;
        rb.gravityScale = 1f;

        patrolDirection.Normalize();
        facingDirection = (int)Mathf.Sign(patrolDirection.x);

        // Auto-assign animator if missing
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Update facing direction based on movement
        if (rb.velocity.x != 0)
            facingDirection = (int)Mathf.Sign(rb.velocity.x);

        if (player == null)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        Vector3 toPlayer = player.transform.position - transform.position;
        float distanceToPlayer = toPlayer.magnitude;

        // Attack if player is in front and within range
        if (distanceToPlayer <= attackRange && Mathf.Sign(toPlayer.x) == facingDirection && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
            PlayAttackAnimation(); // Play attack animation
            lastAttackTime = Time.time;
            rb.velocity = new Vector2(0, rb.velocity.y); // Stop moving during attack
            return;
        }

        // Chase player if close enough
        if (distanceToPlayer < chaseTriggerDistance)
        {
            ChasePlayer(toPlayer);
        }
        else if (returnHome && !isHome)
        {
            ReturnHome();
        }
        else if (patrol && isHome)
        {
            Patrol();
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    void Attack()
    {
        // Hitbox is in front of goblin based on facing direction
        Vector2 hitboxCenter = (Vector2)transform.position + Vector2.right * facingDirection * (attackRange / 2);
        Vector2 hitboxSize = new Vector2(attackRange, 1f); // height of 1 unit

        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(hitboxCenter, hitboxSize, 0f, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
        }
    }

    void PlayAttackAnimation()
    {
        if (animator == null || string.IsNullOrEmpty(attackTrigger)) return;

        animator.SetTrigger(attackTrigger);
    }

    void ChasePlayer(Vector3 toPlayer)
    {
        if (IsGroundAhead(toPlayer.normalized.x))
            rb.velocity = new Vector2(toPlayer.normalized.x * moveSpeed, rb.velocity.y);
        else
            rb.velocity = new Vector2(0, rb.velocity.y);

        isHome = false;
        isReturningHome = false;
    }

    void ReturnHome()
    {
        Vector3 homeDir = home - transform.position;
        float distToHome = homeDir.magnitude;

        if (distToHome > 0.1f)
        {
            if (IsGroundAhead(homeDir.normalized.x))
                rb.velocity = new Vector2(homeDir.normalized.x * moveSpeed, rb.velocity.y);
            else
                rb.velocity = new Vector2(0, rb.velocity.y);

            isReturningHome = true;
            isHome = false;
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            isHome = true;
            isReturningHome = false;
            transform.position = home;
        }
    }

    void Patrol()
    {
        Vector3 displacement = transform.position - home;
        float distance = displacement.magnitude;
        float buffer = 0.2f;

        if (distance > patrolDistance + buffer)
        {
            Vector3 clampedPos = home + displacement.normalized * patrolDistance;
            transform.position = clampedPos;
            patrolDirection = -patrolDirection;
            patrolDirection.Normalize();
        }

        if (IsGroundAhead(patrolDirection.x))
            rb.velocity = new Vector2(patrolDirection.x * moveSpeed, rb.velocity.y);
        else
            rb.velocity = new Vector2(0, rb.velocity.y);
    }

    bool IsGroundAhead(float dir)
    {
        Vector2 origin = (Vector2)transform.position + Vector2.down * 0.1f;
        Vector2 direction = Vector2.right * Mathf.Sign(dir);

        RaycastHit2D hit = Physics2D.Raycast(origin + direction * 0.3f, Vector2.down, groundCheckDistance);

        return hit.collider != null;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw attack hitbox in front of goblin
        Gizmos.color = Color.red;
        Vector2 hitboxCenter = (Vector2)transform.position + Vector2.right * facingDirection * (attackRange / 2);
        Vector2 hitboxSize = new Vector2(attackRange, 1f);
        Gizmos.DrawWireCube(hitboxCenter, hitboxSize);
    }
}
