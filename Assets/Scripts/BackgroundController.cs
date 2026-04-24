using UnityEngine;

/// <summary>
/// Creates and manages a background image plane for the maze scene.
/// Attach this to any GameObject in your scene to set up the background.
/// </summary>
public class BackgroundController : MonoBehaviour
{
    [Header("Background Settings")]
    [Tooltip("The image/texture to use as background.")]
    public Texture2D backgroundTexture;

    [Tooltip("Scale of the background plane.")]
    public Vector3 backgroundScale = new Vector3(100f, 100f, 1f);

    [Tooltip("Position offset for the background (Z should be behind the maze).")]
    public Vector3 backgroundPosition = new Vector3(0f, 0f, -50f);

    [Tooltip("Material to use for the background. If null, creates a standard material.")]
    public Material backgroundMaterial;

    private GameObject _backgroundPlane;

    void Start()
    {
        CreateBackground();
    }

    /// <summary>Creates the background plane with the specified texture.</summary>
    public void CreateBackground()
    {
        // Check if background already exists
        if (_backgroundPlane != null)
        {
            Debug.LogWarning("BackgroundController: Background already exists!");
            return;
        }

        // Create a new plane GameObject
        _backgroundPlane = new GameObject("BackgroundPlane");
        _backgroundPlane.transform.SetParent(transform);
        _backgroundPlane.transform.localPosition = backgroundPosition;
        _backgroundPlane.transform.localScale = backgroundScale;

        // Add a mesh filter and renderer
        MeshFilter meshFilter = _backgroundPlane.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = _backgroundPlane.AddComponent<MeshRenderer>();

        // Use a plane mesh
        Mesh planeMesh = Resources.GetBuiltinResource<Mesh>("Plane.fbx");
        meshFilter.mesh = planeMesh;

        // Create or assign material
        Material material;
        if (backgroundMaterial != null)
        {
            material = new Material(backgroundMaterial);
        }
        else
        {
            material = new Material(Shader.Find("Standard"));
        }

        // Assign texture if provided
        if (backgroundTexture != null)
        {
            material.mainTexture = backgroundTexture;
        }

        meshRenderer.material = material;

        // Add a collider (optional, set as trigger so it doesn't block movement)
        BoxCollider collider = _backgroundPlane.AddComponent<BoxCollider>();
        collider.isTrigger = true;

        Debug.Log("BackgroundController: Background plane created successfully!");
    }

    /// <summary>Removes the background plane.</summary>
    public void RemoveBackground()
    {
        if (_backgroundPlane != null)
        {
            Destroy(_backgroundPlane);
            Debug.Log("BackgroundController: Background plane removed.");
        }
    }

    /// <summary>Updates the background texture.</summary>
    public void SetBackgroundTexture(Texture2D newTexture)
    {
        if (_backgroundPlane != null)
        {
            backgroundTexture = newTexture;
            MeshRenderer meshRenderer = _backgroundPlane.GetComponent<MeshRenderer>();
            if (meshRenderer != null && meshRenderer.material != null)
            {
                meshRenderer.material.mainTexture = newTexture;
                Debug.Log("BackgroundController: Background texture updated.");
            }
        }
    }
}
