using UnityEngine;

/// <summary>
/// Simple camera follow script for a top-down maze game.
/// Attach this to your main camera and assign the player transform in the inspector.
/// </summary>

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The transform to follow (usually the player)")]
    public Transform target;

    [Header("Follow Settings")]
    [Tooltip("How smoothly the camera follows the target")]
    [Range(0.1f, 10f)]
    public float smoothSpeed = 5f;

    [Tooltip("Offset from the target position")]
    public Vector3 offset = new Vector3(0f, 10f, -5f);

    [Tooltip("Should the camera look at the target?")]
    public bool lookAtTarget = true;

    private void LateUpdate()
    {
        if (target == null) return;

        // Calculate desired position
        Vector3 desiredPosition = target.position + offset;

        // Smoothly move to desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // Look at target if enabled
        if (lookAtTarget)
        {
            transform.LookAt(target);
        }
    }
}