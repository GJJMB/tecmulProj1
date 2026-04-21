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

    [Tooltip("The GameObject that has MazeGrid")]
    public MazeGenerator gridvars;
    [Header("Movement")]
    [Tooltip("Time in seconds to slide one cell.")]
    public float moveTime = 0.15f;

    [Header("Events")]
    public UnityEvent onReachedExit;

    [Header("Turn Counter")]
    public int turnCounter = 0;

    public GameObject enemyPrefab; // Assign an enemy prefab in the Inspector
    private int _cellX, _cellY;
    private int _spawnedEnemies = 0;
    private bool _moving;
    private WinScreen _winScreen;

    // Direction vectors matching MazeGenerator's N/E/S/W order
    private static readonly int[] DX = { 0, 1, 0, -1 };
    private static readonly int[] DY = { 1, 0, -1, 0 };

    private InputAction moveAction;

    void Start()
    {
        // Automatically find WinScreen anywhere in the scene — no Inspector wiring needed
        _winScreen = FindObjectOfType<WinScreen>(includeInactive: true);

        if (_winScreen == null)
            Debug.LogError("PlayerController: Could not find a WinScreen component anywhere in the scene!");
        else
            Debug.Log($"PlayerController: WinScreen found on '{_winScreen.gameObject.name}'");

        // Snap to entry cell (0, 0) at generation time
        _cellX = 0;
        _cellY = 0;
        transform.position = grid.CellToWorld(_cellX, _cellY) + Vector3.up * 0.5f;

        // Initialize input
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("Dpad")
            .With("Up",    "<Keyboard>/w")
            .With("Up",    "<Keyboard>/upArrow")
            .With("Down",  "<Keyboard>/s")
            .With("Down",  "<Keyboard>/downArrow")
            .With("Left",  "<Keyboard>/a")
            .With("Left",  "<Keyboard>/leftArrow")
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
        if (!grid.IsPassable(_cellX, _cellY, dir)) return;

        int nx = _cellX + DX[dir];
        int ny = _cellY + DY[dir];

        if (nx < 0 || nx >= grid.Width || ny < 0 || ny >= grid.Height) return;

        _cellX = nx;
        _cellY = ny;
        StartCoroutine(SlideTo(grid.CellToWorld(nx, ny) + Vector3.up * 0.5f));
        grid.NotifyMoved(nx, ny);

        // Increment turn counter
        turnCounter++;

        // Check if it's time to spawn an enemy
        Debug.Log($"Turn {turnCounter} taken. Checking for enemy spawn... Spawned enemies: {_spawnedEnemies}");
        Debug.Log($"gridvars.turnsUntilEnemySpawn: {gridvars.turnsUntilEnemySpawn}, gridvars.maxEnemies: {gridvars.maxEnemies}");
        Debug.Log($"gridvars.maxEnemies: {gridvars.maxEnemies}, _spawnedEnemies: {_spawnedEnemies}");
        if (turnCounter % gridvars.turnsUntilEnemySpawn == 0 && turnCounter > 0 && _spawnedEnemies < gridvars.maxEnemies)
        {
            if (enemyPrefab != null)
            {
                _spawnedEnemies++;
                Vector3 spawnPos = grid.CellToWorld(0, 0) + Vector3.up * 0.5f; // Spawn at entry cell
                Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            }
        }

        // Win check
        if (nx == grid.Generator.width - 1 && ny == grid.Generator.height - 1)
        {
            onReachedExit.Invoke();
            OnReachedExit();
        }
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        if (_moving) return;
        if (PauseMenu.IsPaused) return;

        Vector2 move = context.ReadValue<Vector2>();
        if (move == Vector2.zero) return;

        if      (move.y > 0) TryMove(MazeGridController.North);
        else if (move.x > 0) TryMove(MazeGridController.East);
        else if (move.y < 0) TryMove(MazeGridController.South);
        else if (move.x < 0) TryMove(MazeGridController.West);
    }

    private IEnumerator SlideTo(Vector3 target)
    {
        _moving = true;
        Vector3 start   = transform.position;
        float   elapsed = 0f;

        while (elapsed < moveTime)
        {
            elapsed           += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, elapsed / moveTime);
            yield return null;
        }

        transform.position = target;
        _moving = false;
    }

    // ── Current cell accessors (useful for HUD / minimap) ────────────────────
    public int CellX => _cellX;
    public int CellY => _cellY;

    /// <summary>Triggered when player reaches the exit cell.</summary>
    public void OnReachedExit()
    {
        Debug.Log($"OnReachedExit called! Position: ({_cellX}, {_cellY}), WinScreen found: {_winScreen != null}");

        if (_winScreen != null)
        {
            _winScreen.ShowWinScreen();
        }
        else
        {
            Debug.LogError("PlayerController: WinScreen not found in scene!");
        }
    }

    void OnDestroy()
    {
        moveAction.Disable();
    }
}