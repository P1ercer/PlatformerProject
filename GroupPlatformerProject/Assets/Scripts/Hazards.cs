using UnityEngine;

public class Hazards : MonoBehaviour
{
    [Header("Hazard Settings")]
    [Tooltip("How much damage this hazard deals on contact.")]
    public float damage = 1f;

    [Tooltip("If true, hazard only damages the player once per contact.")]
    public bool oneTimeDamage = false;

    private bool hasDealtDamage = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryDealDamage(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDealDamage(collision.gameObject);
    }

    private void TryDealDamage(GameObject obj)
    {
        if (oneTimeDamage && hasDealtDamage) return;

        // Attempt to get PlayerHealth
        PlayerHealth player = obj.GetComponent<PlayerHealth>();
        if (player != null)
        {
            player.TakeDamage(damage);
            hasDealtDamage = true;
        }
    }
}
