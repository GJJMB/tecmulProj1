using UnityEngine;

/// <summary>
/// Key collectible that the player can pick up.
/// When collected, it unlocks doors with matching IDs.
/// </summary>
public class Key : MonoBehaviour
{
    [Tooltip("ID of this key (matches door ID)")]
    public int keyId = 1;

    [Tooltip("Sound to play when collected")]
    public AudioClip collectSound;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player collided with key {keyId}.");
            // Find player controller and add key
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.CollectKey(keyId);

                // Play sound if available
                if (collectSound != null)
                {
                    AudioSource.PlayClipAtPoint(collectSound, transform.position);
                }

                // Destroy the key
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.Log($"thats interesting, {other.name} collided with key {keyId} but is not the player.");
            Debug.Log($"its tag is {other.tag}");
        }

    }
}