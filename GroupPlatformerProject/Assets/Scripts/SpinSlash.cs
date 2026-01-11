using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinSlash : MonoBehaviour
{
    [Header("Spin Settings")]
    public float spinDuration = 1f;
    public float damageInterval = 0.2f;
    public float attackRadius = 1f;
    public int damage = 10;
    public float manaCost = 25f;

    // -------------------------------
    // Animation Fields
    // -------------------------------
    [Header("Animation Settings")]
    [Tooltip("Animator component for playing the spin animation.")]
    public Animator animator;

    [Tooltip("Trigger name for spin animation (optional).")]
    private string spinTrigger = "Spin";

    [Tooltip("Optional animation clip to play instead of a trigger.")]
    public AnimationClip spinAnimationClip;

    private bool isSpinning = false;
    private float spinTimer = 0f;
    private float damageTimer = 0f;

    private PlayerController playerController;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        if (playerController == null)
            Debug.LogWarning("PlayerController component not found!");

        // Auto-assign animator if missing
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && !isSpinning)
        {
            if (playerController != null && playerController.currentMana >= manaCost)
            {
                playerController.SpendMana(manaCost);
                playerController.UpdateManaUI();
                StartSpin();

                PlaySpinAnimation();
            }
            else
            {
                Debug.Log("Not enough mana to perform Spin Slash!");
            }
        }

        if (isSpinning)
        {
            Spin();
        }
    }

    void StartSpin()
    {
        isSpinning = true;
        spinTimer = spinDuration;
        damageTimer = 0f;
        Debug.Log("Spin started!");
    }

    void Spin()
    {
        damageTimer -= Time.deltaTime;
        if (damageTimer <= 0f)
        {
            DetectAndDamageEnemies();
            damageTimer = damageInterval;
        }

        spinTimer -= Time.deltaTime;
        if (spinTimer <= 0f)
        {
            isSpinning = false;
            Debug.Log("Spin ended!");
        }
    }

    void DetectAndDamageEnemies()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRadius);

        if (hits.Length == 0)
        {
            Debug.Log("No enemies in range.");
            return;
        }

        foreach (Collider2D collider in hits)
        {
            if (collider.CompareTag("Enemy") || collider.CompareTag("KnightBoss"))
            {
                float directionToEnemy = collider.transform.position.x - transform.position.x;

                if (directionToEnemy != 0)
                    DealDamage(collider);
            }
        }
    }

    void DealDamage(Collider2D enemyCollider)
    {
        //just copy and paste the same bit but change the health script name
        EnemyHealth enemyHealth = enemyCollider.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
            Debug.Log($"Dealt {damage} damage to {enemyCollider.name}");
        }

        KnightHealth knightHealth = enemyCollider.GetComponent<KnightHealth>();
        if (knightHealth != null)
        {
            knightHealth.TakeDamage(damage);
            Debug.Log($"Dealt {damage} damage to {enemyCollider.name}");
        }
    }

    // ---------------------------------------
    // Animation Helper
    // ---------------------------------------
    void PlaySpinAnimation()
    {
        if (animator == null) return;

        if (!string.IsNullOrEmpty(spinTrigger))
        {
            animator.SetTrigger(spinTrigger);
        }
        else if (spinAnimationClip != null)
        {
            //animator.Play(spinAnimationClip.name);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}
