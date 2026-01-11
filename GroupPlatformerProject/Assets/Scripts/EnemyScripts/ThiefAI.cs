using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ThiefAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float chaseSpeed = 3f;
    public float chaseTriggerDistance = 5f;
    public bool returnHome = true;

    [Header("Patrol Settings")]
    public bool patrol = true;
    public Vector3 patrolDirection = Vector3.right;
    public float patrolDistance = 3f;
    public float groundCheckDistance = 0.5f;

    [Header("Stealing Settings")]
    public int coinsToSteal = 2;
    public float damageToPlayer = 1f;
    public float stealDistance = 1.5f;
    public float stealCooldown = 5f;

    [Header("Coin Drop Settings")]
    public GameObject coinPrefab;
    public float coinDropSpread = 0.5f;

    [Header("Animation Settings")]
    public Animator attackAnimator;   // For damaging player
    public Animator stealAnimator;    // For stealing coins

    [HideInInspector] public string attackTrigger = "Attack";
    [HideInInspector] public string stealTrigger = "Steal";

    private GameObject player;
    private Rigidbody2D rb;
    private Vector3 home;
    private bool isHome = true;
    private bool isReturningHome = false;
    private bool hasStolen = false;
    private float lastStealTime = -Mathf.Infinity;

    private float distanceToPlayer; // <--- Added for global access
    private int stolenCoins = 0;
    private GameObject targetCoin;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        rb = GetComponent<Rigidbody2D>();
        home = transform.position;

        rb.freezeRotation = true;
        rb.gravityScale = 1f;

        patrolDirection.Normalize();

        // ✅ Only fix: Ignore collision with the player
        Collider2D thiefCol = GetComponent<Collider2D>();
        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (thiefCol != null && playerCol != null)
            Physics2D.IgnoreCollision(thiefCol, playerCol);
    }

    void Update()
    {
        if (player == null)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        Vector3 toPlayer = player.transform.position - transform.position;
        distanceToPlayer = toPlayer.magnitude; // updated here

        if (hasStolen)
        {
            RunAwayFromPlayer();
            return;
        }

        if (distanceToPlayer <= stealDistance && Time.time >= lastStealTime + stealCooldown)
        {
            StealFromPlayer();
            PlayStealAnimation();
            return;
        }

        FindClosestCoin();

        if (targetCoin != null)
        {
            MoveToCoin();
            return;
        }

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

    void StealFromPlayer()
    {
        Collectables playerCollect = player.GetComponent<Collectables>();
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

        // Steal coins
        if (playerCollect != null && playerCollect.coins > 0)
        {
            int stolenAmount = Mathf.Min(coinsToSteal, playerCollect.coins);
            playerCollect.coins -= stolenAmount;
            stolenCoins += stolenAmount;
            Debug.Log($"Thief stole {stolenAmount} coins from player!");
        }

        // Deal damage
        if (playerHealth != null && !playerHealth.GetComponent<Powerups>().isInvincible)
        {
            playerHealth.health -= damageToPlayer;

            if (playerHealth.health < 0) playerHealth.health = 0;
            playerHealth.healthBar.fillAmount = playerHealth.health / playerHealth.maxHealth;

            if (playerHealth.health <= 0)
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);

            Debug.Log($"Thief damaged player for {damageToPlayer} health!");
            PlayAttackAnimation(); // separate attack animation
        }

        hasStolen = true;
        lastStealTime = Time.time;
        rb.velocity = new Vector2(0, rb.velocity.y);
    }

    void PlayStealAnimation()
    {
        if (stealAnimator != null && !string.IsNullOrEmpty(stealTrigger))
            stealAnimator.SetTrigger(stealTrigger);
    }

    void PlayAttackAnimation()
    {
        if (attackAnimator != null && !string.IsNullOrEmpty(attackTrigger))
            attackAnimator.SetTrigger(attackTrigger);
    }

    void FindClosestCoin()
    {
        GameObject[] coins = GameObject.FindGameObjectsWithTag("Coin");
        GameObject closest = null;
        float closestDist = Mathf.Infinity;

        foreach (GameObject coin in coins)
        {
            float dist = Vector3.Distance(transform.position, coin.transform.position);
            if (dist < closestDist && dist <= chaseTriggerDistance)
            {
                closestDist = dist;
                closest = coin;
            }
        }

        targetCoin = closest;
    }

    void MoveToCoin()
    {
        if (targetCoin == null)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        Vector3 toCoin = targetCoin.transform.position - transform.position;
        float distToCoin = toCoin.magnitude;

        if (distToCoin <= stealDistance)
        {
            PickUpCoin();
            rb.velocity = new Vector2(0, rb.velocity.y);
            PlayStealAnimation();
            return;
        }
        else
        {
            if (IsGroundAhead(toCoin.normalized.x))
                rb.velocity = new Vector2(toCoin.normalized.x * chaseSpeed, rb.velocity.y);
            else
                rb.velocity = new Vector2(0, rb.velocity.y);

            isHome = false;
            isReturningHome = false;
        }
    }

    void PickUpCoin()
    {
        if (targetCoin == null) return;

        Destroy(targetCoin);
        stolenCoins++;
        Debug.Log("Thief picked up a coin!");

        if (stolenCoins >= coinsToSteal)
        {
            hasStolen = true;
            lastStealTime = Time.time;
        }

        targetCoin = null;
    }

    void RunAwayFromPlayer()
    {
        Vector3 runDir = (transform.position - player.transform.position).normalized;

        if (IsGroundAhead(runDir.x))
            rb.velocity = new Vector2(runDir.x * chaseSpeed * 1.5f, rb.velocity.y);
        else
            rb.velocity = new Vector2(0, rb.velocity.y);

        if (Vector3.Distance(transform.position, player.transform.position) > chaseTriggerDistance * 2)
        {
            hasStolen = false;
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    void ChasePlayer(Vector3 toPlayer)
    {
        if (IsGroundAhead(toPlayer.normalized.x))
            rb.velocity = new Vector2(toPlayer.normalized.x * chaseSpeed, rb.velocity.y);
        else
            rb.velocity = new Vector2(0, rb.velocity.y);

        isHome = false;
        isReturningHome = false;
    }

    void ReturnHome()
    {
        Vector3 homeDir = home - transform.position;
        float distToHome = homeDir.magnitude;
        float stopThreshold = 0.1f;

        if (distToHome > stopThreshold)
        {
            float speed = chaseSpeed;
            if (distToHome < 1f)
                speed = Mathf.Lerp(0, chaseSpeed, distToHome / 1f);

            if (IsGroundAhead(homeDir.normalized.x))
                rb.velocity = new Vector2(homeDir.normalized.x * speed, rb.velocity.y);
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
            rb.velocity = new Vector2(patrolDirection.x * chaseSpeed, rb.velocity.y);
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

    public void DropStolenCoins()
    {
        if (stolenCoins <= 0) return;

        for (int i = 0; i < stolenCoins; i++)
        {
            Vector2 dropPos = (Vector2)transform.position + Random.insideUnitCircle * coinDropSpread;
            Instantiate(coinPrefab, dropPos, Quaternion.identity);
        }

        Debug.Log($"Thief dropped {stolenCoins} coins!");
        stolenCoins = 0;
    }
}
