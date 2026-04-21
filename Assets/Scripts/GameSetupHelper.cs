using UnityEngine;

public class GameSetupHelper : MonoBehaviour
{
    [Header("Auto-Setup")]
    [Tooltip("Run this in Awake to ensure proper setup.")]
    public bool autoSetup = true;

    void Awake()
    {
        if (!autoSetup) return;

        // Ensure Player has correct tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Try to find by name
            player = GameObject.Find("Player");
            if (player != null)
            {
                player.tag = "Player";
                Debug.Log("GameSetupHelper: Set Player tag on Player GameObject.");
            }
        }

        // Ensure Player has collider for enemy collision
        if (player != null && player.GetComponent<Collider>() == null)
        {
            CapsuleCollider collider = player.AddComponent<CapsuleCollider>();
            collider.isTrigger = false;
            collider.height = 1f;
            collider.radius = 0.5f;
            Debug.Log("GameSetupHelper: Added CapsuleCollider to Player.");
        }

        Debug.Log("GameSetupHelper: Setup complete.");
    }
}