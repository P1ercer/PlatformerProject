using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class SlamSkill : MonoBehaviour
{
    [Header("Slam Settings")]
    public int slamDamage = 30;
    public float slamRadius = 2.0f;
    public float slamManaCost = 40f;
    public float slamPushForce = 20f;

    [Header("Animation Settings")]
    public Animator animator;
    public AnimationClip slamAnimationClip;

    private PlayerController playerController;
    private Rigidbody2D rb;
    private PlatformerAnimScript animScript;

    private PlayableGraph playableGraph;

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        animScript = GetComponent<PlatformerAnimScript>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            TrySlam();
    }

    void TrySlam()
    {
        if (playerController == null || slamAnimationClip == null) return;

        if (playerController.currentMana >= slamManaCost)
        {
            PlaySlamAnimation();
            Slam();
            playerController.SpendMana(slamManaCost);
            playerController.UpdateManaUI();
        }
    }

    void PlaySlamAnimation()
    {
        if (animator == null) return;

        // Pause normal animation updates
        if (animScript != null)
            animScript.enabled = false;

        // Create a PlayableGraph to play the clip directly
        playableGraph = PlayableGraph.Create("SlamPlayableGraph");
        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);
        var clipPlayable = AnimationClipPlayable.Create(playableGraph, slamAnimationClip);
        playableOutput.SetSourcePlayable(clipPlayable);

        playableGraph.Play();

        StartCoroutine(ReenableAnimScriptAfter(slamAnimationClip.length));
    }

    private IEnumerator ReenableAnimScriptAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (animScript != null)
            animScript.enabled = true;

        if (playableGraph.IsValid())
            playableGraph.Destroy();
    }

    void Slam()
    {
        Vector2 slamCenter = (Vector2)transform.position + Vector2.down * 1f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(slamCenter, slamRadius);

        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(slamDamage);
                continue;
            }

            KnightHealth boss = hit.GetComponent<KnightHealth>();
            if (boss != null)
            {
                boss.TakeDamage(slamDamage);
                Debug.Log($"SlamSkill dealt {slamDamage} damage to {boss.name}");
            }
        }

        if (rb != null)
            rb.velocity = new Vector2(rb.velocity.x, -slamPushForce);

        Debug.Log("Slam activated!");
    }
}
