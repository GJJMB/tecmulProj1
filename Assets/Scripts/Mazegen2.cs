using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural Maze Generator for Unity
/// Uses Recursive Backtracking (Depth-First Search) algorithm.
///
/// HOW TO USE:
/// 1. Create an empty GameObject in your scene and attach this script.
/// 2. Create a Cube prefab for walls, assign it to "wallPrefab".
/// 3. Create a Cube prefab for the floor, assign it to "floorPrefab" (optional).
/// 4. Press Play — the maze generates automatically.
/// 5. Adjust Width, Height, and Cell Size in the Inspector.
/// </summary>
public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Settings")]
    [Tooltip("Number of cells horizontally")]
    public int width = 10;

    [Tooltip("Number of cells vertically")]
    public int height = 10;

    [Tooltip("Size of each maze cell in Unity units")]
    public float cellSize = 2f;

    [Tooltip("Height of the walls")]
    public float wallHeight = 2f;

    [Header("Prefabs")]
    [Tooltip("Prefab used for walls (should be a 1x1x1 cube)")]
    public GameObject wallPrefab;

    [Tooltip("Optional floor tile prefab")]
    public GameObject floorPrefab;

    [Header("Generation")]
    [Tooltip("Seed for reproducible mazes (0 = random)")]
    public int seed = 0;

    [Tooltip("Visualize generation step-by-step")]
    public bool animateGeneration = false;

    [Tooltip("Delay between steps when animating (seconds)")]
    public float animationDelay = 0.05f;

    // --- Internal State ---
    private bool[,] visited;
    private bool[,,] walls; // walls[x, y, dir]: 0=North, 1=East, 2=South, 3=West
    private GameObject mazeParent;

    // Directions: North, East, South, West
    private static readonly int[] dx = { 0, 1, 0, -1 };
    private static readonly int[] dy = { 1, 0, -1, 0 };
    private static readonly int[] opposite = { 2, 3, 0, 1 }; // Opposite direction index

    void Start()
    {
        GenerateMaze();
    }

    /// <summary>
    /// Public entry point — call this to (re)generate the maze.
    /// </summary>
    public void GenerateMaze()
    {
        // Clean up previous maze
        if (mazeParent != null)
            Destroy(mazeParent);

        mazeParent = new GameObject("Maze");
        mazeParent.transform.SetParent(transform);

        // Seed the RNG
        if (seed == 0)
            Random.InitState(System.Environment.TickCount);
        else
            Random.InitState(seed);

        // Initialize grid — all walls up
        visited = new bool[width, height];
        walls = new bool[width, height, 4]; // true = wall exists

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                for (int d = 0; d < 4; d++)
                    walls[x, y, d] = true;

        if (animateGeneration)
            StartCoroutine(CarvePassagesAnimated(0, 0));
        else
        {
            CarvePassages(0, 0);
            BuildMesh();
        }
    }

    // ──────────────────────────────────────────
    //  ALGORITHM: Recursive Backtracking (DFS)
    // ──────────────────────────────────────────

    void CarvePassages(int cx, int cy)
    {
        visited[cx, cy] = true;

        // Shuffle directions
        int[] dirs = ShuffledDirections();

        foreach (int dir in dirs)
        {
            int nx = cx + dx[dir];
            int ny = cy + dy[dir];

            if (InBounds(nx, ny) && !visited[nx, ny])
            {
                // Remove wall between current cell and neighbour
                walls[cx, cy, dir] = false;
                walls[nx, ny, opposite[dir]] = false;

                CarvePassages(nx, ny);
            }
        }
    }

    IEnumerator CarvePassagesAnimated(int cx, int cy)
    {
        visited[cx, cy] = true;
        int[] dirs = ShuffledDirections();

        foreach (int dir in dirs)
        {
            int nx = cx + dx[dir];
            int ny = cy + dy[dir];

            if (InBounds(nx, ny) && !visited[nx, ny])
            {
                walls[cx, cy, dir] = false;
                walls[nx, ny, opposite[dir]] = false;

                yield return new WaitForSeconds(animationDelay);

                yield return StartCoroutine(CarvePassagesAnimated(nx, ny));
            }
        }

        // Rebuild mesh each step when animating
        BuildMesh();
    }

    // ──────────────────────────────────────────
    //  MESH BUILDING
    // ──────────────────────────────────────────

    void BuildMesh()
    {
        // Clear previous geometry
        foreach (Transform child in mazeParent.transform)
            Destroy(child.gameObject);

        float wallThickness = cellSize * 0.1f;
        float halfCell = cellSize * 0.5f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 cellCenter = new Vector3(x * cellSize, 0, y * cellSize);

                // Floor tile
                if (floorPrefab != null)
                {
                    GameObject floor = Instantiate(floorPrefab, mazeParent.transform);
                    floor.name = $"Floor_{x}_{y}";
                    floor.transform.position = cellCenter;
                    floor.transform.localScale = new Vector3(cellSize, 0.1f, cellSize);
                }

                // North wall (along +Z edge)
                if (walls[x, y, 0])
                    SpawnWall($"Wall_N_{x}_{y}",
                        cellCenter + new Vector3(0, wallHeight * 0.5f, halfCell),
                        new Vector3(cellSize + wallThickness, wallHeight, wallThickness));

                // East wall (along +X edge)
                if (walls[x, y, 1])
                    SpawnWall($"Wall_E_{x}_{y}",
                        cellCenter + new Vector3(halfCell, wallHeight * 0.5f, 0),
                        new Vector3(wallThickness, wallHeight, cellSize + wallThickness));

                // South wall — only spawn on the bottom border to avoid duplicates
                if (y == 0 && walls[x, y, 2])
                    SpawnWall($"Wall_S_{x}_{y}",
                        cellCenter + new Vector3(0, wallHeight * 0.5f, -halfCell),
                        new Vector3(cellSize + wallThickness, wallHeight, wallThickness));

                // West wall — only spawn on the left border to avoid duplicates
                if (x == 0 && walls[x, y, 3])
                    SpawnWall($"Wall_W_{x}_{y}",
                        cellCenter + new Vector3(-halfCell, wallHeight * 0.5f, 0),
                        new Vector3(wallThickness, wallHeight, cellSize + wallThickness));
            }
        }

        // Mark entrance and exit
        MarkEntrance();
        MarkExit();
    }

    void SpawnWall(string wallName, Vector3 position, Vector3 scale)
    {
        if (wallPrefab == null) return;

        GameObject wall = Instantiate(wallPrefab, mazeParent.transform);
        wall.name = wallName;
        wall.transform.position = position;
        wall.transform.localScale = scale;
    }

    void MarkEntrance()
    {
        // Remove south wall of cell (0,0) as entrance
        walls[0, 0, 2] = false;
    }

    void MarkExit()
    {
        // Remove north wall of top-right cell as exit
        walls[width - 1, height - 1, 0] = false;
    }

    // ──────────────────────────────────────────
    //  HELPERS
    // ──────────────────────────────────────────

    bool InBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;

    int[] ShuffledDirections()
    {
        int[] dirs = { 0, 1, 2, 3 };
        for (int i = dirs.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
        }
        return dirs;
    }

#if UNITY_EDITOR
    // Draw cell grid in Scene view for debugging
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.3f);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                Gizmos.DrawWireCube(
                    transform.position + new Vector3(x * cellSize, 0, y * cellSize),
                    new Vector3(cellSize, 0.1f, cellSize));
    }
#endif
}
