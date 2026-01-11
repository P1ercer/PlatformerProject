using System.Collections;
using UnityEngine;

public class PlatformerAnimScript : MonoBehaviour
{
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool isSlamming = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (isSlamming) return; // Don't override slam animation

        float moveX = Input.GetAxis("Horizontal");
        float moveY = rb.velocity.y;

        animator.SetFloat("x", moveX);
        animator.SetFloat("y", moveY);

        // Flip sprite based on direction
        if (moveX < 0)
        {
            sr.flipX = true;
            animator.SetBool("Flip", true);
        }
        else if (moveX > 0)
        {
            sr.flipX = false;
            animator.SetBool("Flip", false);
        }
    }

    public void PlaySlamAnimation()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        isSlamming = true;

        // Force override into Slam state
        if (animator.HasState(0, Animator.StringToHash("Slam")))
        {
            animator.CrossFadeInFixedTime("Slam", 0.05f);
            Debug.Log("Playing Slam animation.");
        }
        else
        {
            Debug.LogWarning("Animator does not have a state named 'Slam'!");
        }

        // Auto unlock after 0.8s (or adjust to match your clip length)
        StartCoroutine(EndSlamAfter(0.8f));
    }

    private IEnumerator EndSlamAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        isSlamming = false;
    }
}
