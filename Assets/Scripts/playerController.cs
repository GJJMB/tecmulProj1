using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;

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

    [Header("Audio")]
    [Tooltip("Movement sounds to be played randomly when the player moves. Add as many as you want.")]
    public System.Collections.Generic.List<AudioClip> movementSounds = new System.Collections.Generic.List<AudioClip>();

    private AudioSource audioSource;

    [Header("Events")]
    public UnityEvent onReachedExit;

    [Header("Turn Counter")]
    public int turnCounter = 0;

    [Header("Keys")]
    [Tooltip("Keys collected by the player")]
    public System.Collections.Generic.List<int> collectedKeys = new System.Collections.Generic.List<int>();

    [Tooltip("UI text to display collected keys")]
    public TMP_Text keyCounterText;

    public GameObject enemyPrefab; // Assign an enemy prefab in the Inspector
    private int _cellX, _cellY;
    private int _spawnedEnemies = 0;
    private bool _moving;
    private WinScreen _winScreen;
    private float _elapsedTime = 0f;
    private bool _timerActive = false;

    // Direction vectors matching MazeGenerator's N/E/S/W order
    private static readonly int[] DX = { 0, 1, 0, -1 };
    private static readonly int[] DY = { 1, 0, -1, 0 };

    private InputAction moveAction;

    void Start()
    {
        // Automatically find WinScreen anywhere in the scene — no Inspector wiring needed
        _winScreen = FindFirstObjectByType<WinScreen>();

        if (_winScreen == null)
            Debug.LogError("PlayerController: Could not find a WinScreen component anywhere in the scene!");
        else
            Debug.Log($"PlayerController: WinScreen found on '{_winScreen.gameObject.name}'");

        // Get or create an AudioSource for movement sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

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

        // Start the timer
        _elapsedTime = 0f;
        _timerActive = true;

        // Initialize UI
        UpdateKeyUI();
    }

    void Update()
    {
        // Input handled via Input System callbacks
        // Update timer
        if (_timerActive)
        {
            _elapsedTime += Time.deltaTime;
        }
    }

    /// <summary>Call this from UI buttons as well (pass 0-3 for N/E/S/W).</summary>
    public void TryMove(int dir)
    {
        if (_moving) return;
        if (!grid.IsPassable(_cellX, _cellY, dir)) return;

        // Check for locked doors
        if (IsDoorBlocking(_cellX, _cellY, dir)) return;

        PlayRandomMovementSound();
        int nx = _cellX + DX[dir];
        int ny = _cellY + DY[dir];

        if (nx < 0 || nx >= grid.Width || ny < 0 || ny >= grid.Height) return;

        _cellX = nx;
        _cellY = ny;
        StartCoroutine(SlideTo(grid.CellToWorld(nx, ny) + Vector3.up * 1f));
        grid.NotifyMoved(nx, ny);

        // Increment turn counter
        turnCounter++;

        // Check if it's time to spawn an enemy
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

    /// <summary>Triggered when player reaches the exit cell.</summary>
    public void OnReachedExit()
    {
        Debug.Log($"OnReachedExit called! Position: ({_cellX}, {_cellY}), WinScreen found: {_winScreen != null}");

        // Stop the timer
        _timerActive = false;

        if (_winScreen != null)
        {
            _winScreen.ShowWinScreen(_elapsedTime);
        }
        else
        {
            Debug.LogError("PlayerController: WinScreen not found in scene!");
        }
    }

    /// <summary>Gets the elapsed time since the game started.</summary>
    public float GetElapsedTime()
    {
        return _elapsedTime;
    }

    // ── Key System ───────────────────────────────────────────────────────────

    /// <summary>Called when the player collects a key.</summary>
    public void CollectKey(int keyId)
    {
        if (!collectedKeys.Contains(keyId))
        {
            collectedKeys.Add(keyId);
            Debug.Log($"Player collected key {keyId}. Total keys: {collectedKeys.Count}");
            UpdateKeyUI();
        }
    }

    /// <summary>Checks if the player has the specified key.</summary>
    public bool HasKey(int keyId)
    {
        return collectedKeys.Contains(keyId);
    }

    /// <summary>Gets the number of keys collected.</summary>
    public int GetKeyCount()
    {
        return collectedKeys.Count;
    }

    /// <summary>Updates the key counter UI.</summary>
    private void UpdateKeyUI()
    {
        if (keyCounterText != null)
        {
            keyCounterText.text = $"Keys: {collectedKeys.Count}";
        }
    }

    /// <summary>Checks if a door is blocking movement in the specified direction.</summary>
    private bool IsDoorBlocking(int x, int y, int dir)
    {
        // Find door objects at the wall position
        Vector3 wallPosition = GetWallPosition(x, y, dir);
        Collider[] colliders = Physics.OverlapSphere(wallPosition, 0.5f);

        foreach (Collider collider in colliders)
        {
            Door door = collider.GetComponent<Door>();
            if (door != null && door.isActiveAndEnabled)
            {
                // Check if player has the key for this door
                if (!HasKey(door.doorId))
                {
                    Debug.Log($"Door {door.doorId} is locked. Player needs key {door.doorId}.");
                    return true;
                }
                else
                {
                    // Player has key, unlock the door
                    door.UnlockDoor();
                }
            }
        }

        return false;
    }

    /// <summary>Gets the world position of a wall between two cells.</summary>
    private Vector3 GetWallPosition(int x, int y, int dir)
    {
        Vector3 cellCenter = grid.CellToWorld(x, y);
        float halfCell = grid.Generator.cellSize * 0.5f;

        switch (dir)
        {
            case 0: return cellCenter + new Vector3(0, 1f, halfCell);  // North
            case 1: return cellCenter + new Vector3(halfCell, 1f, 0);  // East
            case 2: return cellCenter + new Vector3(0, 1f, -halfCell); // South
            case 3: return cellCenter + new Vector3(-halfCell, 1f, 0); // West
            default: return cellCenter;
        }
    }

    /// <summary>Plays a random movement sound from the available sounds.</summary>
    private void PlayRandomMovementSound()
    {
        // Play a random sound if any are available
        if (movementSounds.Count > 0 && audioSource != null)
        {
            AudioClip randomSound = movementSounds[Random.Range(0, movementSounds.Count)];
            audioSource.PlayOneShot(randomSound);
        }
    }

    void OnDestroy()
    {
        moveAction.Disable();
    }
}