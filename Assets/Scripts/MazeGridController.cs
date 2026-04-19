using UnityEngine;
using System;

/// <summary>
/// Sits alongside MazeGenerator and exposes a step-based movement grid.
/// Call IsPassable(x, y, dir) to check if a move is legal before committing.
/// </summary>
public class MazeGridController : MonoBehaviour
{
    // Direction constants matching MazeGenerator's internal convention
    public const int North = 0, East = 1, South = 2, West = 3;

    private MazeGenerator _gen;

    // Cached ref so other scripts don't need to find MazeGenerator themselves
    public MazeGenerator Generator => _gen;

    // Fired whenever a move is validated and committed: (newX, newY)
    public event Action<int, int> OnPlayerMoved;

    void Awake()
    {
        _gen = GetComponent<MazeGenerator>();
        if (_gen == null)
            Debug.LogError("MazeGridController requires a MazeGenerator on the same GameObject.");
    }

    /// <summary>Returns true if there is NO wall between cell (x,y) and its neighbour in 'dir'.</summary>
    public bool IsPassable(int x, int y, int dir)
    {
        // walls array is private in MazeGenerator — expose it via a helper below,
        // OR make walls internal/public in MazeGenerator (see note).
        return !_gen.HasWall(x, y, dir);
    }

    /// <summary>Converts a grid cell to its world-space centre position (at floor level).</summary>
    public Vector3 CellToWorld(int x, int y)
    {
        return transform.position + new Vector3(x * _gen.cellSize, 0f, y * _gen.cellSize);
    }

    public int Width  => _gen.width;
    public int Height => _gen.height;

    /// <summary>Call this from PlayerController after validating a move.</summary>
    public void NotifyMoved(int newX, int newY) => OnPlayerMoved?.Invoke(newX, newY);
}
