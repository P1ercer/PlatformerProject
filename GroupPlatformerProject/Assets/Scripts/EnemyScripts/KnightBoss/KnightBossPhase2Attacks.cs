using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(KnightBossMovement))]
public class KnightBossPhase2Attacks : MonoBehaviour
{
    [System.Serializable]
    public class Attack
    {
        public string name;
        public float damage;
        public float range;
        public float duration;
        [HideInInspector] public float nextUseTime;

        [Header("🔥 Burn Effect")]
        public float burnDuration;
        public float burnTickDamage;

        [Header("🎞 Animation")]
        [Tooltip("Trigger name or AnimationClip for this attack")]
       [HideInInspector] public string animationTrigger; // 👈 assign the trigger name in Inspector
        public AnimationClip animationClip; // optional, if you want direct clip playback
    }

    [System.Serializable]
    public class PhysicalAttack
    {
        public string name;
        public float damage;
        public float range;
        public float duration;
        [HideInInspector] public float nextUseTime;

        [Header("🎞 Animation")]
        [Tooltip("Trigger name or AnimationClip for this physical attack")]
        [HideInInspector] public string animationTrigger; // 👈 assign manually per attack
        public AnimationClip animationClip;
    }

    [Header("Phase 2 Attacks")]
    public Attack flamingSlam;
    public Attack infernalTorrent;
    public PhysicalAttack groundBreaker;
    public Attack moltenEruption;
    public Attack openingRanged;
    public PhysicalAttack basicAttack;

    [Header("Attack Cooldowns")]
    public float flamingSlamCooldown = 5f;
    public float infernalTorrentCooldown = 8f;
    public float groundBreakerCooldown = 10f;
    public float moltenEruptionCooldown = 12f;

    [Header("Projectile Prefab (Opening Ranged)")]
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    [Tooltip("Multiplier to adjust the projectile speed in the editor (1 = normal speed)")]
    public float projectileSpeedMultiplier = 1f;

    [Header("Hitbox Settings")]
    public LayerMask playerLayer;
    public float slamRadius = 2.5f;
    public float eruptionRadius = 4f;
    public Vector2 groundBreakerSize = new Vector2(3f, 1.2f);
    public float basicAttackRange = 2f;

    private Rigidbody2D rb;
    private KnightBossMovement movement;
    private Animator anim;
    private Transform player;
    private bool hasUsedOpeningRanged = false;
    private List<(Vector3 pos, float size, bool box, float time)> debugHits = new();

    [HideInInspector] public bool isAttacking = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<KnightBossMovement>();
        anim = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    // Helper to play assigned animation
    private void PlayAttackAnimation(string trigger, AnimationClip clip)
    {
        if (anim == null) return;

        if (!string.IsNullOrEmpty(trigger))
        {
            anim.SetTrigger(trigger); // play by trigger
        }
        else if (clip != null)
        {
            anim.Play(clip.name); // play direct clip if specified
        }
    }
    // --------------------------------------------------
    // ATTACK CHOOSER
    // --------------------------------------------------
    public void TryRandomAttack(Transform target)
    {
        if (isAttacking || player == null) return;

        // Opening ranged attack once at start of Phase 2
        if (!hasUsedOpeningRanged)
        {
            hasUsedOpeningRanged = true;
            StartCoroutine(OpeningRangedAttack(openingRanged));
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);
        List<System.Action> possibleAttacks = new List<System.Action>();

        if (Time.time >= flamingSlam.nextUseTime)
            possibleAttacks.Add(() => StartCoroutine(FlamingSlamRoutine(flamingSlam)));

        if (Time.time >= infernalTorrent.nextUseTime)
            possibleAttacks.Add(() => StartCoroutine(InfernalTorrentRoutine(infernalTorrent)));

        if (dist <= 5f && Time.time >= groundBreaker.nextUseTime)
            possibleAttacks.Add(() => StartCoroutine(GroundBreakerRoutine(groundBreaker)));

        if (dist <= 5f && Time.time >= moltenEruption.nextUseTime)
            possibleAttacks.Add(() => StartCoroutine(MoltenEruptionRoutine(moltenEruption)));

        if (possibleAttacks.Count == 0)
        {
            StartCoroutine(BasicAttackRoutine(basicAttack));
            return;
        }

        int choice = Random.Range(0, possibleAttacks.Count);
        possibleAttacks[choice].Invoke();
    }
    // --------------------------------------------------
    // BASIC ATTACK (Fallback)
    // --------------------------------------------------
    private IEnumerator BasicAttackRoutine(PhysicalAttack atk)
    {
        Debug.Log("⚔️ Basic melee attack!");
        isAttacking = true;
        PlayAttackAnimation(atk.animationTrigger, atk.animationClip); // 👈 plays assigned animation

        yield return new WaitForSeconds(0.3f);

        Vector2 dir = player != null ? (player.position - transform.position).normalized : Vector2.right;
        Vector2 hitCenter = (Vector2)transform.position + dir * basicAttackRange * 0.5f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, 1.2f, playerLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth ph = hit.GetComponent<PlayerHealth>();
                if (ph != null)
                    ph.TakeDamage(atk.damage);
            }
        }

        CreateDebugHit(hitCenter, 1.2f, false);
        yield return new WaitForSeconds(0.4f);
        isAttacking = false;
    }

    // --------------------------------------------------
    // FLAMING SLAM
    // --------------------------------------------------
    private IEnumerator FlamingSlamRoutine(Attack atk)
    {
        Debug.Log("Flaming Slam!");
        isAttacking = true;
        atk.nextUseTime = Time.time + flamingSlamCooldown;

        PlayAttackAnimation(atk.animationTrigger, atk.animationClip);

        if (movement != null)
            movement.enabled = false;

        rb.velocity = new Vector2(0, atk.range + 10f);
        yield return new WaitUntil(() => rb.velocity.y <= 0);

        Vector2 diveDir = player != null ? (player.position - transform.position).normalized : Vector2.down;
        rb.velocity = diveDir * (atk.range + 8f);

        bool hitGround = false;
        while (!hitGround)
        {
            if (movementGrounded())
                hitGround = true;

            Collider2D hit = Physics2D.OverlapCircle(transform.position, 1.5f, playerLayer);
            if (hit != null && hit.CompareTag("Player"))
            {
                PlayerHealth ph = hit.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(atk.damage);
                    if (atk.burnDuration > 0)
                    {
                        FireDebuff burn = hit.GetComponent<FireDebuff>() ?? hit.gameObject.AddComponent<FireDebuff>();
                        burn.ApplyBurn(atk.burnDuration, atk.burnTickDamage);
                    }
                }

                CreateDebugHit(transform.position, 1.5f, false);
                break;
            }
            yield return null;
        }

        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(0.4f);

        if (movement != null)
            movement.enabled = true;

        isAttacking = false;
    }

    // --------------------------------------------------
    // INFERNAL TORRENT
    // --------------------------------------------------
    private IEnumerator InfernalTorrentRoutine(Attack atk)
    {
        Debug.Log("🔥 Infernal Torrent!");
        isAttacking = true;
        atk.nextUseTime = Time.time + infernalTorrentCooldown;

        PlayAttackAnimation(atk.animationTrigger, atk.animationClip);

        float elapsed = 0f;
        while (elapsed < atk.duration)
        {
            if (player != null)
                MoveTowardsPlayer(2.2f);

            DoAOEDamage(transform.position, 1.6f, atk);
            CreateDebugHit(transform.position, 1.6f, false);

            yield return new WaitForSeconds(0.4f);
            elapsed += 0.4f;
        }

        rb.velocity = Vector2.zero;
        isAttacking = false;
    }

    // --------------------------------------------------
    // GROUND BREAKER
    // --------------------------------------------------
    private IEnumerator GroundBreakerRoutine(PhysicalAttack atk)
    {
        Debug.Log("💥 Ground Breaker!");
        isAttacking = true;
        atk.nextUseTime = Time.time + groundBreakerCooldown;

        PlayAttackAnimation(atk.animationTrigger, atk.animationClip);

        rb.velocity = new Vector2(0, 8f);
        yield return new WaitUntil(() => rb.velocity.y <= 0 && movementGrounded());

        Vector2 boxCenter = (Vector2)transform.position + Vector2.down * 0.5f;
        DoBoxDamage_NoBurn(boxCenter, groundBreakerSize, atk);
        CreateDebugHit(boxCenter, groundBreakerSize.x, true);

        yield return new WaitForSeconds(0.6f);
        isAttacking = false;
    }

    // --------------------------------------------------
    // MOLTEN ERUPTION
    // --------------------------------------------------
    private IEnumerator MoltenEruptionRoutine(Attack atk)
    {
        Debug.Log("🌋 Molten Eruption!");
        isAttacking = true;
        atk.nextUseTime = Time.time + moltenEruptionCooldown;

        PlayAttackAnimation(atk.animationTrigger, atk.animationClip);

        yield return new WaitForSeconds(atk.duration * 0.4f);
        DoAOEDamage(transform.position, eruptionRadius, atk);
        CreateDebugHit(transform.position, eruptionRadius, false);

        yield return new WaitForSeconds(atk.duration * 0.6f);
        isAttacking = false;
    }

    // --------------------------------------------------
    // OPENING RANGED
    // --------------------------------------------------
    private IEnumerator OpeningRangedAttack(Attack atk)
    {
        Debug.Log("💨 Opening Ranged Projectile!");
        isAttacking = true;

        PlayAttackAnimation(atk.animationTrigger, atk.animationClip);

        yield return new WaitForSeconds(1f);

        if (projectilePrefab != null && projectileSpawnPoint != null && player != null)
        {
            Vector2 dir = (player.position - projectileSpawnPoint.position).normalized;
            GameObject proj = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
            Rigidbody2D prb = proj.GetComponent<Rigidbody2D>();
            if (prb != null)
                prb.velocity = dir * atk.range * projectileSpeedMultiplier;

            StartCoroutine(DestroyProjectileAfterHit(proj, atk));
        }

        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
    }

    private IEnumerator DestroyProjectileAfterHit(GameObject proj, Attack atk)
    {
        float maxLifetime = 5f;
        float elapsed = 0f;

        while (proj != null && elapsed < maxLifetime)
        {
            Collider2D hit = Physics2D.OverlapCircle(proj.transform.position, 0.3f, playerLayer);
            if (hit != null && hit.CompareTag("Player"))
            {
                PlayerHealth ph = hit.GetComponent<PlayerHealth>();
                if (ph != null) ph.TakeDamage(atk.damage);

                if (atk.burnDuration > 0)
                {
                    FireDebuff burn = hit.GetComponent<FireDebuff>() ?? hit.gameObject.AddComponent<FireDebuff>();
                    burn.ApplyBurn(atk.burnDuration, atk.burnTickDamage);
                }

                Destroy(proj);
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (proj != null) Destroy(proj);
    }

    // --------------------------------------------------
    // DAMAGE HELPERS
    // --------------------------------------------------
    private void DoAOEDamage(Vector2 center, float radius, Attack atk)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, playerLayer);
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if (ph == null) continue;

            ph.TakeDamage(atk.damage);

            if (atk.burnDuration > 0)
            {
                FireDebuff burn = hit.GetComponent<FireDebuff>() ?? hit.gameObject.AddComponent<FireDebuff>();
                burn.ApplyBurn(atk.burnDuration, atk.burnTickDamage);
            }
        }
    }

    private void DoBoxDamage_NoBurn(Vector2 center, Vector2 size, PhysicalAttack atk)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, playerLayer);
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if (ph == null) continue;
            ph.TakeDamage(atk.damage);
        }
    }

    private bool movementGrounded()
    {
        return Physics2D.Raycast(transform.position, Vector2.down, 1.2f, LayerMask.GetMask("Ground"));
    }

    private void MoveTowardsPlayer(float speed)
    {
        if (player == null) return;
        Vector2 dir = (player.position - transform.position).normalized;
        rb.velocity = new Vector2(dir.x * speed, rb.velocity.y);
    }

    // --------------------------------------------------
    // DEBUG VISUALS (unchanged)
    // --------------------------------------------------
    private void CreateDebugHit(Vector3 pos, float size, bool box)
    {
        debugHits.Add((pos, size, box, Time.time + 0.5f));
    }

    private void Update() => debugHits.RemoveAll(d => Time.time > d.time);

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (debugHits == null) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f); // orange tint
        foreach (var hit in debugHits)
        {
            if (hit.box)
            {
                // Draw a rectangle for box-type hitboxes
                Gizmos.DrawWireCube(hit.pos, new Vector3(hit.size, hit.size * 0.4f, 0f));
            }
            else
            {
                // Draw a circle for radial hitboxes
                Gizmos.DrawWireSphere(hit.pos, hit.size);
            }
        }
    }
#endif

}
