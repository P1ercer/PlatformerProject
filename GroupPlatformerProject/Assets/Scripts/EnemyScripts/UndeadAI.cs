using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UndeadAI : MonoBehaviour
{
    GameObject player;
    public float chaseSpeed = 5.0f;
    public float chaseTriggerDistance = 10f;
    public bool returnHome = true;
    Vector3 home;
    bool isHome = true;
    public bool patrol = true;
    public Vector3 patrolDirection = Vector3.right;  // default to right patrol
    public float patrolDistance = 3f;

    Rigidbody2D rb;
    Vector3 originalScale;

    [Header("Animation Settings")]
    public AnimationClip idleAnimation;
    public AnimationClip walkAnimation;
    private Animation anim; // Uses Unity's Legacy Animation component

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        home = transform.position;
        rb = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;

        // Get or add Animation component
        anim = GetComponent<Animation>();
        if (anim == null)
        {
            anim = gameObject.AddComponent<Animation>();
        }

        // Add clips to Animation component
        if (idleAnimation != null && !anim.GetClip(idleAnimation.name))
            anim.AddClip(idleAnimation, idleAnimation.name);
        if (walkAnimation != null && !anim.GetClip(walkAnimation.name))
            anim.AddClip(walkAnimation, walkAnimation.name);

        // Start idle
        if (idleAnimation != null)
            anim.Play(idleAnimation.name);
    }

    void Update()
    {
        Vector3 chaseDir = player.transform.position - transform.position;
        bool isWalking = false;

        // --- Chase Player ---
        if (chaseDir.magnitude < chaseTriggerDistance)
        {
            chaseDir.Normalize();
            rb.velocity = new Vector2(chaseDir.x * chaseSpeed, rb.velocity.y);
            isHome = false;
            isWalking = true;
        }

        // --- Return Home ---
        else if (returnHome && !isHome)
        {
            Vector3 homeDir = home - transform.position;
            if (homeDir.magnitude > 0.2f)
            {
                homeDir.Normalize();
                rb.velocity = new Vector2(homeDir.x * chaseSpeed, rb.velocity.y);
                isWalking = true;
            }
            else
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
                isHome = true;
                isWalking = false;
            }
        }

        // --- Patrol ---
        else if (patrol)
        {
            Vector3 displacement = transform.position - home;
            if (displacement.magnitude > patrolDistance)
            {
                patrolDirection = -displacement;
            }
            patrolDirection.Normalize();
            rb.velocity = new Vector2(patrolDirection.x * chaseSpeed, rb.velocity.y);
            isWalking = true;
        }

        // --- Idle ---
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            isWalking = false;
        }

        // --- Flip sprite ---
        if (Mathf.Abs(rb.velocity.x) > 0.1f)
        {
            transform.localScale = new Vector3(
                Mathf.Sign(rb.velocity.x) * Mathf.Abs(originalScale.x),
                originalScale.y,
                originalScale.z
            );
        }

        // --- Play animations based on state ---
        if (anim != null)
        {
            if (isWalking && walkAnimation != null && anim.clip != walkAnimation)
                anim.CrossFade(walkAnimation.name);
            else if (!isWalking && idleAnimation != null && anim.clip != idleAnimation)
                anim.CrossFade(idleAnimation.name);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseTriggerDistance);
    }
}
