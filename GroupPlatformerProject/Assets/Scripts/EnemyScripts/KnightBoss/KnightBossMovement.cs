using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class KnightBossMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float baseChaseSpeed = 5f;
    public float baseRunAwaySpeed = 4f;
    public float detectionRange = 10f;
    public float attackRange = 2f;

    [Header("Dash Settings")]
    public float dashDistance = 1f;
    public float dashCooldown = 3f;
    public float dashDuration = 0.2f;

    [Header("Phase Settings")]
    public bool isPhase2 = false; // Set true when Phase 2 starts

    private Rigidbody2D rb;
    private KnightBossPhase1Attacks p1Attacks;
    private KnightBossPhase2Attacks p2Attacks;

    private bool isRunningAway = false;
    private bool isDashing = false;
    private float runAwayTimer = 0f;
    private float idleTimer = 0f;
    private float dashTimer = 0f;
    private Vector2 dashDirection;
    private Vector2 randomOffset = Vector2.zero;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        p1Attacks = GetComponent<KnightBossPhase1Attacks>();
        p2Attacks = GetComponent<KnightBossPhase2Attacks>();
    }

    private void Update()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        Transform playerTransform = playerObj.transform;

        // Timers
        if (idleTimer > 0)
        {
            idleTimer -= Time.deltaTime;
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        if (dashTimer > 0)
            dashTimer -= Time.deltaTime;

        if (isPhase2)
            Phase2Movement(playerTransform);
        else
            Phase1Movement(playerTransform);

        if (runAwayTimer > 0)
            runAwayTimer -= Time.deltaTime;
    }

    private void Phase1Movement(Transform playerTransform)
    {
        // Original random movement and attack logic from Phase 1
        Vector2 direction = (playerTransform.position - transform.position);
        float distance = direction.magnitude;

        // Random speed variation
        float chaseSpeed = baseChaseSpeed * Random.Range(0.8f, 1.2f);

        // Occasionally add random sideways movement
        if (Random.Range(0f, 1f) < 0.01f)
            randomOffset = new Vector2(Random.Range(-1f, 1f), 0);

        Vector2 moveDir = direction.normalized + randomOffset;
        moveDir.Normalize();

        if (p1Attacks != null && !p1Attacks.isAttacking && !isRunningAway)
            rb.velocity = new Vector2(moveDir.x * chaseSpeed, rb.velocity.y);

        // Attack decisions
        if (distance <= attackRange && p1Attacks != null && !p1Attacks.isAttacking)
        {
            int decision = Random.Range(0, 10);
            if (decision >= 8)
                StartRunningAway();
            else
                p1Attacks.TryRandomAttack(playerTransform);
        }
    }

    private void Phase2Movement(Transform playerTransform)
    {
        Vector2 direction = (playerTransform.position - transform.position);
        float distance = direction.magnitude;

        // === Dash behavior ===
        if (!isDashing && dashTimer <= 0f && Random.Range(0f, 1f) < 0.02f)
        {
            StartCoroutine(DashTowardsPlayer(direction));
            return;
        }

        // === Normal chase ===
        float chaseSpeed = baseChaseSpeed * 1.5f; // Faster in phase 2
        direction.Normalize();

        // Add slight strafing for unpredictability
        direction.x += Random.Range(-0.2f, 0.2f);
        direction.Normalize();

        rb.velocity = new Vector2(direction.x * chaseSpeed, rb.velocity.y);

        // Randomly trigger Phase 2 abilities
        if (p2Attacks != null && !p2Attacks.isAttacking && Random.Range(0f, 1f) < 0.03f)
        {
            p2Attacks.TryRandomAttack(playerTransform);
        }
    }

    private IEnumerator DashTowardsPlayer(Vector2 direction)
    {
        isDashing = true;
        dashTimer = dashCooldown;

        Vector2 dashDir = direction.normalized;
        float dashSpeed = (dashDistance / dashDuration);

        float timer = 0f;
        while (timer < dashDuration)
        {
            rb.velocity = dashDir * dashSpeed;
            timer += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
    }

    private void StartRunningAway()
    {
        isRunningAway = true;
        runAwayTimer = Random.Range(1f, 2f);
    }

    private void RunAway(Transform playerTransform)
    {
        if (runAwayTimer <= 0)
        {
            isRunningAway = false;
            return;
        }

        float runSpeed = baseRunAwaySpeed * Random.Range(0.8f, 1.2f);
        Vector2 direction = (transform.position - playerTransform.position).normalized;

        // Add slight random strafing
        direction.x += Random.Range(-0.3f, 0.3f);
        direction.Normalize();

        rb.velocity = new Vector2(direction.x * runSpeed, rb.velocity.y);
    }

}
