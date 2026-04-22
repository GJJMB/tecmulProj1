using UnityEngine;

/// <summary>
/// Door that requires a key to unlock.
/// Blocks player movement until the matching key is collected.
/// </summary>
public class Door : MonoBehaviour
{
    [Tooltip("ID of the key required to unlock this door")]
    public int doorId = 1;

    [Tooltip("Sound to play when unlocked")]
    public AudioClip unlockSound;

    [HideInInspector]
    public bool isLocked = true;
    private Collider doorCollider;
    private Renderer doorRenderer;

    private void Start()
    {
        doorCollider = GetComponent<Collider>();
        doorRenderer = GetComponent<Renderer>();

        // Start locked
        UpdateDoorState();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && isLocked)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && player.HasKey(doorId))
            {
                UnlockDoor();
            }
        }
    }

    /// <summary>Called when the player tries to unlock this door.</summary>
    public void UnlockDoor()
    {
        if (!isLocked) return;

        isLocked = false;
        UpdateDoorState();

        // Play sound if available
        if (unlockSound != null)
        {
            AudioSource.PlayClipAtPoint(unlockSound, transform.position);
        }

        // Optional: Animate door opening
        StartCoroutine(OpenDoorAnimation());
    }

    private void UpdateDoorState()
    {
        if (doorCollider != null)
        {
            doorCollider.isTrigger = !isLocked; // Locked = solid, Unlocked = trigger
        }

        if (doorRenderer != null)
        {
            // Dim the color when unlocked
            Color color = doorRenderer.material.color;
            color.a = isLocked ? 1.0f : 0.3f;
            doorRenderer.material.color = color;
        }
    }

    private System.Collections.IEnumerator OpenDoorAnimation()
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale;
        endScale.y *= 0.1f; // Shrink door vertically

        float duration = 1.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        // Optionally destroy the door after animation
        // Destroy(gameObject);
    }
}