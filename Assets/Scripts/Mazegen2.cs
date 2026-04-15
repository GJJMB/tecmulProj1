using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural Maze Generator for Unity
/// Uses Recursive Backtracking (Depth-First Search) algorithm.
///
/// HOW TO USE:
/// 1. Create an empty GameObject and attach this script.
/// 2. Assign a Cube prefab to "wallPrefab" (and optionally "floorPrefab").
/// 3. Optionally assign custom prefabs to "entryMarkerPrefab" / "exitMarkerPrefab".
///    If left empty, colored primitive pillars are created automatically.
/// 4. Press Play — the maze generates with a green ENTRY and red EXIT marker.
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

    [Tooltip("Optional custom Entry marker prefab. If null, a green pillar is created automatically.")]
    public GameObject entryMarkerPrefab;

    [Tooltip("Optional custom Exit marker prefab. If null, a red pillar is created automatically.")]
    public GameObject exitMarkerPrefab;

    [Tooltip("Player prefab to spawn at the entry. Should have PlayerController script.")]
    public GameObject playerPrefab;

    [Header("Entry / Exit Colors (used when no prefab is assigned)")]
    public Color entryColor = new Color(0.1f, 0.9f, 0.2f);   // bright green
    public Color exitColor  = new Color(0.9f, 0.15f, 0.1f);  // bright red

    [Header("Generation")]
    [Tooltip("Seed for reproducible mazes (0 = random each run)")]
    public int seed = 0;

    [Tooltip("Visualize generation step-by-step")]
    public bool animateGeneration = false;

    [Tooltip("Delay between steps when animating (seconds)")]
    public float animationDelay = 0.05f;


    // ── Public read-only access to portal world positions ────────────────────
    public Vector3 EntryWorldPosition { get; private set; }
    public Vector3 ExitWorldPosition  { get; private set; }

    // ── Internal state ───────────────────────────────────────────────────────
    private bool[,]  visited;
    private bool[,,] walls;   // walls[x, y, dir] — 0=N  1=E  2=S  3=W

    private GameObject mazeParent;

    private static readonly int[] dx       = {  0,  1,  0, -1 };
    private static readonly int[] dy       = {  1,  0, -1,  0 };
    private static readonly int[] opposite = {  2,  3,  0,  1 };

    // Entry: bottom-left cell (0,0), opening faces South
    // Exit:  top-right cell (w-1, h-1), opening faces North
    private int EntryCellX => 0;
    private int EntryCellY => 0;
    private int ExitCellX  => width  - 1;
    private int ExitCellY  => height - 1;

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>Exposes wall state for external grid queries.</summary>
    public bool HasWall(int x, int y, int dir) => walls[x, y, dir];

    void Start() => GenerateMaze();

    /// <summary>Call this at any time to (re)generate the maze.</summary>
    public void GenerateMaze()
    {
        if (mazeParent != null) Destroy(mazeParent);

        mazeParent = new GameObject("Maze");
        mazeParent.transform.SetParent(transform);

        Random.InitState(seed == 0 ? System.Environment.TickCount : seed);

        visited = new bool[width, height];
        walls   = new bool[width, height, 4];

        for (int x = 0; x < width;  x++)
        for (int y = 0; y < height; y++)
        for (int d = 0; d < 4;      d++)
            walls[x, y, d] = true;

        if (animateGeneration)
            StartCoroutine(CarvePassagesAnimated(0, 0));
        else
        {
            CarvePassages(0, 0);
            BuildMesh();
        }
    }

    // ── Algorithm: Recursive Backtracking (DFS) ───────────────────────────────

    void CarvePassages(int cx, int cy)
    {
        visited[cx, cy] = true;
        foreach (int dir in ShuffledDirections())
        {
            int nx = cx + dx[dir], ny = cy + dy[dir];
            if (!InBounds(nx, ny) || visited[nx, ny]) continue;
            walls[cx, cy, dir]           = false;
            walls[nx, ny, opposite[dir]] = false;
            CarvePassages(nx, ny);
        }
    }

    IEnumerator CarvePassagesAnimated(int cx, int cy)
    {
        visited[cx, cy] = true;
        foreach (int dir in ShuffledDirections())
        {
            int nx = cx + dx[dir], ny = cy + dy[dir];
            if (!InBounds(nx, ny) || visited[nx, ny]) continue;
            walls[cx, cy, dir]           = false;
            walls[nx, ny, opposite[dir]] = false;
            yield return new WaitForSeconds(animationDelay);
            yield return StartCoroutine(CarvePassagesAnimated(nx, ny));
        }
        BuildMesh();
    }

    // ── Mesh Building ─────────────────────────────────────────────────────────

    void BuildMesh()
    {
        foreach (Transform child in mazeParent.transform)
            Destroy(child.gameObject);

        float wt       = cellSize * 0.1f;  // wall thickness
        float halfCell = cellSize * 0.5f;

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            Vector3 cc = CellCenter(x, y);

            // Floor tile
            if (floorPrefab != null)
                Spawn(floorPrefab, $"Floor_{x}_{y}", cc, new Vector3(cellSize, 0.1f, cellSize));

            // North wall (+Z edge)
            if (walls[x, y, 0])
                SpawnWall($"Wall_N_{x}_{y}",
                    cc + new Vector3(0, wallHeight * 0.5f, halfCell),
                    new Vector3(cellSize + wt, wallHeight, wt));

            // East wall (+X edge)
            if (walls[x, y, 1])
                SpawnWall($"Wall_E_{x}_{y}",
                    cc + new Vector3(halfCell, wallHeight * 0.5f, 0),
                    new Vector3(wt, wallHeight, cellSize + wt));

            // South wall — only on bottom border row to avoid duplicates
            if (y == 0 && walls[x, y, 2])
                SpawnWall($"Wall_S_{x}_{y}",
                    cc + new Vector3(0, wallHeight * 0.5f, -halfCell),
                    new Vector3(cellSize + wt, wallHeight, wt));

            // West wall — only on left border column to avoid duplicates
            if (x == 0 && walls[x, y, 3])
                SpawnWall($"Wall_W_{x}_{y}",
                    cc + new Vector3(-halfCell, wallHeight * 0.5f, 0),
                    new Vector3(wt, wallHeight, cellSize + wt));
        }

        OpenEntryExit();
        SpawnMarkers();
        SpawnPlayer();
    }

    // ── Entry / Exit ──────────────────────────────────────────────────────────

    /// <summary>Opens the wall gaps that form the physical entry and exit.</summary>
    void OpenEntryExit()
    {
        walls[EntryCellX, EntryCellY, 2] = false;  // Entry: south wall of (0,0)
        walls[ExitCellX,  ExitCellY,  0] = false;  // Exit:  north wall of top-right cell
    }

    /// <summary>
    /// Places a visible marker at each opening.
    /// Uses the assigned prefab if set, otherwise auto-creates a colored cylinder pillar.
    /// </summary>
    void SpawnMarkers()
    {
        float halfCell = cellSize * 0.5f;
        float pillarH  = wallHeight * 1.4f;   // taller than walls so it stands out

        // Entry sits just outside the south opening of cell (0,0)
        Vector3 entryPos = CellCenter(EntryCellX, EntryCellY)
                         + new Vector3(0, pillarH * 0.5f, -halfCell - cellSize * 0.35f);

        // Exit sits just outside the north opening of the top-right cell
        Vector3 exitPos  = CellCenter(ExitCellX, ExitCellY)
                         + new Vector3(0, pillarH * 0.5f,  halfCell + cellSize * 0.35f);

        EntryWorldPosition = entryPos;
        ExitWorldPosition  = exitPos;

        SpawnPortalMarker("Entry", entryPos, entryColor, entryMarkerPrefab, pillarH);
        SpawnPortalMarker("Exit",  exitPos,  exitColor,  exitMarkerPrefab,  pillarH);
    }

    void SpawnPortalMarker(string label, Vector3 pos, Color col, GameObject prefab, float pillarH)
    {
        GameObject marker;

        if (prefab != null)
        {
            marker = Instantiate(prefab, mazeParent.transform);
            marker.transform.position = pos;
        }
        else
        {
            // Auto-create: a glowing cylinder pillar
            marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.transform.SetParent(mazeParent.transform);
            marker.transform.position   = pos;
            marker.transform.localScale = new Vector3(cellSize * 0.22f, pillarH * 0.5f, cellSize * 0.22f);

            ApplyEmissiveMaterial(marker, col);

            // Floating disc above the pillar as a cap / beacon
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.transform.SetParent(mazeParent.transform);
            cap.transform.position   = pos + Vector3.up * (pillarH * 0.5f + 0.05f);
            cap.transform.localScale = new Vector3(cellSize * 0.45f, 0.06f, cellSize * 0.45f);
            ApplyEmissiveMaterial(cap, col);
            Destroy(cap.GetComponent<Collider>());
        }

        marker.name = $"Marker_{label}";
        Destroy(marker.GetComponent<Collider>()); // markers are visual only
    }

    void SpawnPlayer()
    {
        if (playerPrefab != null)
        {
            GameObject player = Instantiate(playerPrefab, mazeParent.transform);
            Vector3 spawnPos = CellCenter(EntryCellX, EntryCellY) + Vector3.up * 0.5f;
            player.transform.position = spawnPos;
            player.name = "Player";

            // Assign the grid reference if PlayerController is present
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.grid = GetComponent<MazeGridController>();
            }
        }
    }

    /// <summary>Creates and assigns a Standard material with emission to a GameObject.</summary>
    void ApplyEmissiveMaterial(GameObject go, Color col)
    {
        Renderer rend = go.GetComponent<Renderer>();
        if (rend == null) return;

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = col;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", col * 0.7f);
        rend.material = mat;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    Vector3 CellCenter(int x, int y) =>
        transform.position + new Vector3(x * cellSize, 0, y * cellSize);

    void SpawnWall(string wallName, Vector3 position, Vector3 scale)
    {
        if (wallPrefab == null) return;
        Spawn(wallPrefab, wallName, position, scale);
    }

    void Spawn(GameObject prefab, string objName, Vector3 position, Vector3 scale)
    {
        GameObject go = Instantiate(prefab, mazeParent.transform);
        go.name = objName;
        go.transform.position   = position;
        go.transform.localScale = scale;
    }

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
    void OnDrawGizmos()
    {
        // Cell grid
        Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.25f);
        for (int x = 0; x < width;  x++)
        for (int y = 0; y < height; y++)
            Gizmos.DrawWireCube(CellCenter(x, y), new Vector3(cellSize, 0.1f, cellSize));

        float hs = cellSize * 0.5f;

        // Entry gizmo
        Gizmos.color = entryColor;
        Gizmos.DrawSphere(
            transform.position + new Vector3(EntryCellX * cellSize, wallHeight, -hs), 0.35f);

        // Exit gizmo
        Gizmos.color = exitColor;
        Gizmos.DrawSphere(
            transform.position + new Vector3(ExitCellX * cellSize, wallHeight,
                                             ExitCellY * cellSize + hs), 0.35f);
    }
#endif
}
