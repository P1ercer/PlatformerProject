using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public class MercenaryAI : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float detectionRange = 10f;
    public bool patrol = true;
    public Vector3 patrolDirection = Vector3.right;
    public float patrolDistance = 3f;
    public bool returnHome = true;

    [Header("Combat")]
    public float attackRange = 1.5f;
    public int attackDamage = 3;
    public float attackCooldown = 1f;

    [Header("Edge Detection")]
    public float groundCheckDistance = 0.2f;

    [Header("Animation Settings")]
    [Tooltip("Animator component for playing attack animation.")]
    public Animator animator;

    [Tooltip("Trigger name for attack animation.")]
    private string attackTrigger = "Attack";

    private Rigidbody2D rb;
    private Transform player;
    private float lastAttackTime;
    private Vector3 home;
    private bool isHome = true;
    private bool isReturningHome = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        home = transform.position;
        patrolDirection.Normalize();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError("Player not found! Make sure your player has the tag 'Player'.");

        // Auto-assign animator if missing
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            ChasePlayer();
        }
        else if (returnHome && !isHome)
        {
            ReturnHomeSmoothly();
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

    void ChasePlayer()
    {
        Vector3 toPlayer = player.position - transform.position;
        float direction = Mathf.Sign(toPlayer.x);

        if (!IsEdgeAhead())
            rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
        else
            rb.velocity = new Vector2(0, rb.velocity.y);

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            AttackPlayer();
            PlayAttackAnimation(); //  Play attack animation
            lastAttackTime = Time.time;
        }

        isHome = false;
        isReturningHome = false;
    }

    void Patrol()
    {
        Vector3 displacement = transform.position - home;
        float distance = displacement.magnitude;

        if (distance > patrolDistance)
        {
            Vector3 clampedPos = home + displacement.normalized * patrolDistance;
            transform.position = clampedPos;

            patrolDirection = -patrolDirection;
            patrolDirection.Normalize();
        }

        if (!IsEdgeAhead())
            rb.velocity = new Vector2(patrolDirection.x * moveSpeed, rb.velocity.y);
        else
            rb.velocity = new Vector2(0, rb.velocity.y);
    }

    void ReturnHomeSmoothly()
    {
        Vector3 homeDir = home - transform.position;
        float distToHome = homeDir.magnitude;
        float stopThreshold = 0.1f;

        if (distToHome > stopThreshold)
        {
            float speed = moveSpeed;
            if (distToHome < 1f)
                speed = Mathf.Lerp(0, moveSpeed, distToHome / 1f);

            Vector3 moveDir = homeDir.normalized;

            if (!IsEdgeAhead())
                rb.velocity = new Vector2(moveDir.x * speed, rb.velocity.y);
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

    bool IsEdgeAhead()
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(patrolDirection.normalized.x * 0.5f, 0);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance);
        Debug.DrawRay(origin, Vector2.down * groundCheckDistance, Color.red);
        return hit.collider == null;
    }

    void AttackPlayer()
    {
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null && !playerHealth.GetComponent<Powerups>().isInvincible)
        {
            playerHealth.health -= attackDamage;

            if (playerHealth.health < 0) playerHealth.health = 0;
            playerHealth.healthBar.fillAmount = playerHealth.health / playerHealth.maxHealth;

            if (playerHealth.health <= 0)
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);

            Debug.Log($"Mercenary hits player for {attackDamage} damage!");
        }
    }

    // -------------------------------
    // Animation Helper
    // -------------------------------
    void PlayAttackAnimation()
    {
        if (animator == null || string.IsNullOrEmpty(attackTrigger)) return;

        animator.SetTrigger(attackTrigger);
    }
}
