using UnityEngine;

public class BossUIOpener : MonoBehaviour
{
    [Header("Assign the Boss UI GameObject here")]
    public GameObject bossUI;

    private void Start()
    {
        // Make sure it's off at the start, unless you want it visible immediately.
        if (bossUI != null)
            bossUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // If you're using 2D, change this to OnTriggerEnter2D + Collider2D.
        if (other.CompareTag("Player"))
        {
            if (bossUI != null)
                bossUI.SetActive(true);
            else
                Debug.LogWarning("BossUIOpener: No BossUI assigned in the inspector.");
        }
    }
}
