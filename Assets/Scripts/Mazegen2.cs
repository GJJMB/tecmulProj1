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

    [Tooltip("Optional custom Entry floor prefab. If null, the entry floor uses the normal floor prefab with a colored marker.")]
    public GameObject entryFloorPrefab;

    [Tooltip("Optional custom Exit floor prefab. If null, the exit floor uses the normal floor prefab with a colored marker.")]
    public GameObject exitFloorPrefab;

    [Tooltip("Optional custom Entry marker prefab. If null, a green floor marker is created automatically.")]
    public GameObject entryMarkerPrefab;

    [Tooltip("Optional custom Exit marker prefab. If null, a red pillar is created automatically.")]
    public GameObject exitMarkerPrefab;

    [Tooltip("Player prefab to spawn at the entry. Should have PlayerController script.")]
    public GameObject playerPrefab;

    [Header("Entry / Exit Colors (used when no prefab is assigned)")]
    public Color entryColor = new Color(0.1f, 0.9f, 0.2f);   // bright green
    public Color exitColor = new Color(0.9f, 0.15f, 0.1f);  // bright red

    [Header("Generation")]
    [Tooltip("Seed for reproducible mazes (0 = random each run)")]
    public int seed = 0;

    [Tooltip("Visualize generation step-by-step")]
    public bool animateGeneration = false;

    [Tooltip("Delay between steps when animating (seconds)")]
    public float animationDelay = 0.05f;

    [Header("Enemy Settings")]
    [Tooltip("Prefab for the enemy to spawn.")]
    public GameObject enemyPrefab;

    [Tooltip("Number of turns before spawning an enemy.")]
    public int turnsUntilEnemySpawn = 5;

    [Tooltip("Number maximum enemies.")]
    public int maxEnemies = 1;

    [Header("Key & Door Settings")]
    [Tooltip("Prefab for keys that the player must collect.")]
    public GameObject keyPrefab;

    [Tooltip("Prefab for locked doors that require keys.")]
    public GameObject doorPrefab;

    [Tooltip("Number of keys/doors to place in the maze.")]
    [Range(1, 5)]
    public int numKeysAndDoors = 2;

    [Tooltip("Color for locked doors.")]
    public Color doorColor = new Color(0.8f, 0.4f, 0.1f); // orange

    [Tooltip("Color for keys.")]
    public Color keyColor = new Color(1f, 0.8f, 0f); // gold



    // ── Public read-only access to portal world positions ────────────────────
    public Vector3 EntryWorldPosition { get; private set; }
    public Vector3 ExitWorldPosition { get; private set; }

    // ── Internal state ───────────────────────────────────────────────────────
    private bool[,] visited;
    private bool[,,] walls;   // walls[x, y, dir] — 0=N  1=E  2=S  3=W

    private GameObject mazeParent;

    private static readonly int[] dx = { 0, 1, 0, -1 };
    private static readonly int[] dy = { 1, 0, -1, 0 };
    private static readonly int[] opposite = { 2, 3, 0, 1 };

    // Entry: bottom-left cell (0,0), opening faces South
    // Exit:  top-right cell (w-1, h-1), opening faces North
    private int EntryCellX => 0;
    private int EntryCellY => 0;
    private int ExitCellX => width - 1;
    private int ExitCellY => height - 1;

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>Exposes wall state for external grid queries.</summary>
    public bool HasWall(int x, int y, int dir) => walls[x, y, dir];

    void Start()
    {
        // If the helper has a seed (anything other than 0), use it!
        if (GameSetup.SelectedSeed != 0)
        {
            this.seed = GameSetup.SelectedSeed;
        }
        if (GameSetup.MapWidth > 0)
        {
            this.width = GameSetup.MapWidth;
        }
        if (GameSetup.MapHeight > 0)
        {
            this.height = GameSetup.MapHeight;
        }
        if(GameSetup.NumDoorsKeys > 0)
        {
            this.numKeysAndDoors = GameSetup.NumDoorsKeys;
        }

        Debug.Log("MazeGenerator: Starting maze generation with seed " + seed);
        Debug.Log("MazeGenerator: Starting maze generation with width " + width);
        Debug.Log("MazeGenerator: Starting maze generation with height " + height);
        //log all of the gamesetup vars
        Debug.Log("GameSetup width" + GameSetup.MapWidth);
        Debug.Log("GameSetup height" + GameSetup.MapHeight);
        Debug.Log("GameSetup DoorsAndKeys" + GameSetup.NumDoorsKeys);
        Debug.Log("GameSetup SelectedSeed" + GameSetup.SelectedSeed);

        GenerateMaze();
    }    /// <summary>Call this at any time to (re)generate the maze.</summary>


    /// <summary>Call this at any time to (re)generate the maze.</summary>
    public void GenerateMaze()
    {
        if (mazeParent != null) Destroy(mazeParent);

        mazeParent = new GameObject("Maze");
        mazeParent.transform.SetParent(transform);

        Random.InitState(seed == 0 ? System.Environment.TickCount : seed);

        visited = new bool[width, height];
        walls = new bool[width, height, 4];

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

    // ── Algorithm: Recursive Backtracking (DFS) ───────────────────────────────

    void CarvePassages(int cx, int cy)
    {
        visited[cx, cy] = true;
        foreach (int dir in ShuffledDirections())
        {
            int nx = cx + dx[dir], ny = cy + dy[dir];
            if (!InBounds(nx, ny) || visited[nx, ny]) continue;
            walls[cx, cy, dir] = false;
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
            walls[cx, cy, dir] = false;
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

        float wt = cellSize * 0.1f;  // wall thickness
        float halfCell = cellSize * 0.5f;

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Vector3 cc = CellCenter(x, y);

                // Floor tile
                GameObject floorSource = floorPrefab;
                Color? floorColor = null;
                bool isEntry = x == EntryCellX && y == EntryCellY;
                bool isExit = x == ExitCellX && y == ExitCellY;

                if (isEntry)
                {
                    floorSource = entryFloorPrefab != null ? entryFloorPrefab : floorPrefab;
                    floorColor = entryColor;
                }
                else if (isExit)
                {
                    floorSource = exitFloorPrefab != null ? exitFloorPrefab : floorPrefab;
                    floorColor = exitColor;
                }

                SpawnFloor(floorSource, $"Floor_{x}_{y}", cc, new Vector3(cellSize, 0.1f, cellSize), floorColor);

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

        PlaceKeysAndDoors();
        SpawnMarkers();
        SpawnPlayer();
    }

    // ── Key & Door Placement ─────────────────────────────────────────────────

    /// <summary>Places keys and doors in the maze ensuring umKeysAndDoors <= 0 || (keyPrefab == null && doorPrefab == null)) return;keys are accessible before doors.</summary>
    /// 
    void PlaceKeysAndDoors()
    {
        if (numKeysAndDoors <= 0 || keyPrefab == null || doorPrefab == null) return;

        // Find the main path from entry to exit
        List<Vector2Int> mainPath = FindMainPath();

        if (mainPath.Count < numKeysAndDoors * 3) return; // Need enough path cells

        // Place doors and keys together, only if a valid key position is found
        int pathStep = mainPath.Count / (numKeysAndDoors + 1);
        int doorCount = 0;
        for (int i = 1; i <= numKeysAndDoors; i++)
        {
            int pathIndex = Mathf.Min(i * pathStep, mainPath.Count - 2);
            Vector2Int doorCell = mainPath[pathIndex];
            int doorDir = FindSuitableDoorWall(doorCell);
            if (doorDir != -1)
            {
                Vector2Int keyPos = FindKeyPosition(doorCell, mainPath);
                if (keyPos != Vector2Int.zero)
                {
                    // Only spawn door if a key can be placed before it
                    int nx = doorCell.x + dx[doorDir];
                    int ny = doorCell.y + dy[doorDir];

                    // Make sure the corridor is open on BOTH sides
                    walls[doorCell.x, doorCell.y, doorDir] = false;
                    walls[nx, ny, opposite[doorDir]] = false;
                    SpawnDoor(doorCell, doorDir, doorCount + 1);
                    SpawnKey(keyPos, doorCount + 1);
                    doorCount++;
                }
            }
        }
    }

    /// <summary>Finds the main path from entry to exit using BFS.</summary>
    List<Vector2Int> FindMainPath()
    {
        List<Vector2Int> path = new List<Vector2Int>();
        bool[,] visited = new bool[width, height];
        Vector2Int?[,] parent = new Vector2Int?[width, height];

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(EntryCellX, EntryCellY));
        visited[EntryCellX, EntryCellY] = true;

        bool found = false;
        while (queue.Count > 0 && !found)
        {
            Vector2Int current = queue.Dequeue();

            for (int dir = 0; dir < 4; dir++)
            {
                int nx = current.x + dx[dir];
                int ny = current.y + dy[dir];

                if (InBounds(nx, ny) && !visited[nx, ny] && !walls[current.x, current.y, dir])
                {
                    visited[nx, ny] = true;
                    parent[nx, ny] = current;
                    queue.Enqueue(new Vector2Int(nx, ny));

                    if (nx == ExitCellX && ny == ExitCellY)
                    {
                        found = true;
                        break;
                    }
                }
            }
        }

        // Reconstruct path
        if (found)
        {
            Vector2Int current = new Vector2Int(ExitCellX, ExitCellY);
            while (current != new Vector2Int(EntryCellX, EntryCellY))
            {
                path.Insert(0, current);
                if (parent[current.x, current.y].HasValue)
                    current = parent[current.x, current.y].Value;
                else
                    break;
            }
            path.Insert(0, new Vector2Int(EntryCellX, EntryCellY));
        }

        return path;
    }

    /// <summary>Finds a suitable wall direction for a door at the given cell.</summary>
    int FindSuitableDoorWall(Vector2Int cell)
    {
        // Prefer walls that lead to side passages, not dead ends
        List<int> candidates = new List<int>();

        for (int dir = 0; dir < 4; dir++)
        {
            if (walls[cell.x, cell.y, dir]) continue; // Must be an open passage

            int nx = cell.x + dx[dir];
            int ny = cell.y + dy[dir];

            if (!InBounds(nx, ny)) continue;

            // Check if the adjacent cell has at least one other connection
            int connections = 0;
            for (int d = 0; d < 4; d++)
            {
                if (!walls[nx, ny, d]) connections++;
            }

            if (connections >= 2) // Has multiple connections, good for door
            {
                candidates.Add(dir);
            }
        }

        if (candidates.Count > 0)
        {
            return candidates[Random.Range(0, candidates.Count)];
        }

        // Fallback: any wall
        for (int dir = 0; dir < 4; dir++)
        {
            if (walls[cell.x, cell.y, dir]) return dir;
        }

        return -1;
    }

    /// <summary>Finds a suitable position for a key that's accessible before reaching the door.</summary>
    Vector2Int FindKeyPosition(Vector2Int doorPos, List<Vector2Int> mainPath)
    {
        // Look for dead ends or side areas before the door on the main path
        int doorIndex = mainPath.IndexOf(doorPos);
        if (doorIndex <= 0) return Vector2Int.zero;

        // Search backwards from the door position
        for (int i = doorIndex - 1; i >= 0; i--)
        {
            Vector2Int cell = mainPath[i];

            // Check adjacent cells for dead ends
            for (int dir = 0; dir < 4; dir++)
            {
                int nx = cell.x + dx[dir];
                int ny = cell.y + dy[dir];

                if (!InBounds(nx, ny) || walls[cell.x, cell.y, dir]) continue;

                // Check if this adjacent cell is a dead end (only one connection)
                int connections = 0;
                for (int d = 0; d < 4; d++)
                {
                    if (!walls[nx, ny, d]) connections++;
                }

                if (connections == 1) // Dead end, perfect for key
                {
                    return new Vector2Int(nx, ny);
                }
            }
        }

        // Fallback: place key in a random accessible cell before the door
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int i = 0; i < doorIndex; i++)
        {
            candidates.Add(mainPath[i]);
        }

        if (candidates.Count > 0)
        {
            return candidates[Random.Range(0, candidates.Count)];
        }

        return Vector2Int.zero;
    }

    /// <summary>Spawns a door at the specified cell and direction.</summary>
    void SpawnDoor(Vector2Int cell, int dir, int doorId)
    {
        Vector3 cellCenter = CellCenter(cell.x, cell.y);

        float doorHeight = wallHeight * 0.8f;
        float doorThickness = cellSize * 0.3f;

        // 🔥 NEW: place door exactly between this cell and the neighbor
        Vector3 neighborCenter = CellCenter(cell.x + dx[dir], cell.y + dy[dir]);
        Vector3 position = (cellCenter + neighborCenter) / 2f;
        position.y = doorHeight * 0.5f;

        // Scale still depends on direction
        Vector3 scale = Vector3.one;

        switch (dir)
        {
            case 0: // North / South → wide on X
            case 2:
                scale = new Vector3(cellSize * 0.7f, doorHeight, doorThickness);
                break;

            case 1: // East / West → wide on Z
            case 3:
                scale = new Vector3(doorThickness, doorHeight, cellSize * 0.7f);
                break;

            default:
                return;
        }

        GameObject door;

        if (doorPrefab != null)
        {
            door = Instantiate(doorPrefab, mazeParent.transform);
        }
        else
        {
            // Fallback: simple cube door
            door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.transform.SetParent(mazeParent.transform);

            ApplyEmissiveMaterial(door, doorColor);
        }

        door.transform.position = position;
        door.transform.localScale = scale;
        door.name = $"Door_{doorId}";

        // Add door component
        Door doorComponent = door.AddComponent<Door>();
        doorComponent.doorId = doorId;
    }

    /// <summary>Spawns a key at the specified cell.</summary>
    void SpawnKey(Vector2Int cell, int keyId)
    {
        Vector3 position = CellCenter(cell.x, cell.y) + Vector3.up * 1f;
        float keySize = cellSize * 0.3f;

        GameObject key;

        if (keyPrefab != null)
        {
            key = Instantiate(keyPrefab, mazeParent.transform);
        }
        else
        {
            key = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            key.transform.SetParent(mazeParent.transform);

            ApplyEmissiveMaterial(key, keyColor);
        }

        key.transform.position = position;
        key.transform.localScale = new Vector3(keySize, 0.1f, keySize);
        key.name = $"Key_{keyId}";

        // ✅ Collider must be trigger
        Collider col = key.GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        // ✅ Rigidbody required for trigger detection
        Rigidbody rb = key.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // ✅ Key logic
        Key keyComponent = key.AddComponent<Key>();
        keyComponent.keyId = keyId;
    }

    /// <summary>
    /// Places a visible marker at each opening.
    /// Uses the assigned prefab if set, otherwise auto-creates a colored cylinder pillar.
    /// </summary>
    void SpawnMarkers()
    {
        float markerSize = cellSize * 0.75f;
        float markerHeight = 0.05f;

        Vector3 entryPos = CellCenter(EntryCellX, EntryCellY) + Vector3.up * (markerHeight * 0.5f + 0.01f);
        Vector3 exitPos = CellCenter(ExitCellX, ExitCellY) + Vector3.up * (markerHeight * 0.5f + 0.01f);

        EntryWorldPosition = CellCenter(EntryCellX, EntryCellY);
        ExitWorldPosition = CellCenter(ExitCellX, ExitCellY);

        SpawnPortalMarker("Entry", entryPos, entryColor, entryMarkerPrefab, markerSize, markerHeight);
        SpawnPortalMarker("Exit", exitPos, exitColor, exitMarkerPrefab, markerSize, markerHeight);
    }

    void SpawnPortalMarker(string label, Vector3 pos, Color col, GameObject prefab, float size, float height)
    {
        GameObject marker;

        if (prefab != null)
        {
            marker = Instantiate(prefab, mazeParent.transform);
            marker.transform.position = pos;
            marker.transform.localScale = new Vector3(size, height, size);
        }
        else
        {
            marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.transform.SetParent(mazeParent.transform);
            marker.transform.position = pos;
            marker.transform.localScale = new Vector3(size, height, size);
            ApplyEmissiveMaterial(marker, col);
        }

        marker.name = $"Marker_{label}";
        Destroy(marker.GetComponent<Collider>());
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

    void SpawnEnemy()
    {
        if (enemyPrefab != null)
        {
            Vector3 spawnPos = CellCenter(EntryCellX, EntryCellY) + Vector3.up * 0.5f; // Spawn at entry cell
            GameObject enemy = Instantiate(enemyPrefab, mazeParent.transform);
            enemy.transform.position = spawnPos;
            enemy.name = "Enemy";
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
        go.transform.position = position;
        go.transform.localScale = scale;
    }

    void SpawnFloor(GameObject prefab, string objName, Vector3 position, Vector3 scale, Color? tint = null)
    {
        GameObject go;
        if (prefab != null)
        {
            go = Instantiate(prefab, mazeParent.transform);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(mazeParent.transform);
            Destroy(go.GetComponent<Collider>());
        }

        go.name = objName;
        go.transform.position = position;
        go.transform.localScale = scale;

        if (tint.HasValue)
        {
            Renderer rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = tint.Value;
                rend.material = mat;
            }
        }
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
