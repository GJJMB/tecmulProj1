using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Step-based player controller for a procedural maze.
/// Requires a MazeGridController on the maze GameObject.
/// Attach this to the player GameObject.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The GameObject that has MazeGridController + MazeGenerator.")]
    public MazeGridController grid;

    [Header("Movement")]
    [Tooltip("Time in seconds to slide one cell.")]
    public float moveTime = 0.15f;

    [Header("Events")]
    public UnityEvent onReachedExit;

    // ── State ────────────────────────────────────────────────────────────────
    private int _cellX, _cellY;
    private bool _moving;

    // Direction vectors matching MazeGenerator's N/E/S/W order
    private static readonly int[] DX = { 0, 1, 0, -1 };
    private static readonly int[] DY = { 1, 0, -1, 0 };

    private InputAction moveAction;

    void Start()
    {
        // Snap to entry cell (0, 0) at generation time
        _cellX = 0;
        _cellY = 0;
        transform.position = grid.CellToWorld(_cellX, _cellY) + Vector3.up * 0.5f;

        // Initialize input
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.performed += OnMovePerformed;
        moveAction.Enable();
    }

    void Update()
    {
        // Input handled via Input System callbacks
    }

    /// <summary>Call this from UI buttons as well (pass 0-3 for N/E/S/W).</summary>
    public void TryMove(int dir)
    {
        if (_moving) return;
        if (!grid.IsPassable(_cellX, _cellY, dir)) return;   // wall blocks

        int nx = _cellX + DX[dir];
        int ny = _cellY + DY[dir];

        // Bounds guard (should be redundant if IsPassable is correct, but safety first)
        if (nx < 0 || nx >= grid.Width || ny < 0 || ny >= grid.Height) return;

        _cellX = nx;
        _cellY = ny;

        StartCoroutine(SlideTo(grid.CellToWorld(nx, ny) + Vector3.up * 0.5f));
        grid.NotifyMoved(nx, ny);

        // Win check
        if (nx == grid.Generator.width - 1 && ny == grid.Generator.height - 1)
            onReachedExit.Invoke();
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        if (_moving) return;

        Vector2 move = context.ReadValue<Vector2>();
        if (move == Vector2.zero) return;

        // Determine direction based on the vector
        if (move.y > 0) TryMove(MazeGridController.North);
        else if (move.x > 0) TryMove(MazeGridController.East);
        else if (move.y < 0) TryMove(MazeGridController.South);
        else if (move.x < 0) TryMove(MazeGridController.West);
    }

    private IEnumerator SlideTo(Vector3 target)
    {
        _moving = true;
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, elapsed / moveTime);
            yield return null;
        }

        transform.position = target;
        _moving = false;
    }

    // ── Current cell accessors (useful for HUD / minimap) ────────────────────
    public int CellX => _cellX;
    public int CellY => _cellY;

    void OnDestroy()
    {
        moveAction.Disable();
    }
}
