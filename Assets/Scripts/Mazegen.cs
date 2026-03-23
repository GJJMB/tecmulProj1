using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class OurTree<T>
{
    public T Value;
    public List<OurTree<T>> Children = new List<OurTree<T>>();

    public OurTree(T value = default)
    {
        Value = value;
    }

    public OurTree<T> AddChild(T value)
    {
        var child = new OurTree<T>(value);
        Children.Add(child);
        return child;
    }
}
public static class TreeMazeBuilder
{
    public static int[,] Build<T>(OurTree<T> root, out int rows, out int cols)
    {
        var coords = new Dictionary<OurTree<T>, (int r, int c)>();
        AssignCoords(root, coords);

        int maxR = 0, maxC = 0;
        foreach (var (r, c) in coords.Values)
        {
            if (r > maxR) maxR = r;
            if (c > maxC) maxC = c;
        }

        rows = maxR + 1;
        cols = maxC + 1;

        int mRows = 2 * rows - 1;
        int mCols = 2 * cols - 1;
        var m = new int[mRows, mCols];
        foreach (var (r, c) in coords.Values)
            m[r * 2, c * 2] = 1;

        OpenEdges(root, coords, m);

        return m;
    }

    private static int SubtreeWidth<T>(OurTree<T> node)
    {
        if (node.Children.Count == 0) return 1;
        int w = 0;
        foreach (var child in node.Children) w += SubtreeWidth(child);
        return w;
    }
    private static void AssignCoords<T>(
        OurTree<T> root,
        Dictionary<OurTree<T>, (int r, int c)> coords)
    {
        void Recurse(OurTree<T> node, int depth, int colOffset)
        {
            int width = SubtreeWidth(node);
            int col = colOffset + width / 2;
            coords[node] = (depth, col);

            int childOffset = colOffset;
            foreach (var child in node.Children)
            {
                Recurse(child, depth + 1, childOffset);
                childOffset += SubtreeWidth(child);
            }
        }

        Recurse(root, 0, 0);
    }

    private static void OpenEdges<T>(
        OurTree<T> node,
        Dictionary<OurTree<T>, (int r, int c)> coords,
        int[,] m)
    {
        var (pr, pc) = coords[node];
        foreach (var child in node.Children)
        {
            var (cr, cc) = coords[child];
            CarveHorizontal(m, pr, pc, cc);
            CarveVertical(m, pr, cr, cc);
            OpenEdges(child, coords, m);
        }
    }

    private static void CarveHorizontal(int[,] m, int r, int c1, int c2)
    {
        int step = c1 <= c2 ? 1 : -1;
        for (int c = c1; c != c2 + step; c += step)
            m[r * 2, c * 2] = 1;
        for (int c = c1; c != c2; c += step)
            m[r * 2, c * 2 + step] = 1;
    }

    private static void CarveVertical(int[,] m, int r1, int r2, int c)
    {
        for (int r = r1; r <= r2; r++)
            m[r * 2, c * 2] = 1;
        for (int r = r1; r < r2; r++)
            m[r * 2 + 1, c * 2] = 1;
    }

    public static string Render(int[,] m)
    {
        int mRows = m.GetLength(0);
        int mCols = m.GetLength(1);
        var sb = new StringBuilder();
        for (int r = 0; r < mRows; r++)
        {
            for (int c = 0; c < mCols; c++)
                sb.Append(m[r, c] == 1 ? "  " : "██");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public static void PrintMatrix(int[,] m)
    {
        int mRows = m.GetLength(0);
        int mCols = m.GetLength(1);
        Debug.Log("Matrix:");
        for (int r = 0; r < mRows; r++)
        {
            var row = new StringBuilder($"{r,2} ");
            for (int c = 0; c < mCols; c++)
                row.Append($"{m[r, c],2}");
            Debug.Log(row.ToString());
        }
    }
}
public class Mazegen : MonoBehaviour
{
    [Header("Maze Settings")]
    [SerializeField] private int treeDepth = 4;
    [SerializeField] private int treeBranches = 3;

    [Header("Spawning")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private float spacing = 1.0f;
    [SerializeField] private Transform mazeContainer;

    [Header("Camera Framing")]
    [SerializeField] private bool autoFrameCamera = true;
    [SerializeField] private float cameraHeightMultiplier = 1.2f;
    private int[,] _maze;
    private int _mRows, _mCols;

    void Start()
    {
        OurTree<int> tree = BuildSampleTree(treeDepth, treeBranches);
        _maze = TreeMazeBuilder.Build(tree, out int rows, out int cols);
        _mRows = _maze.GetLength(0);
        _mCols = _maze.GetLength(1);

        Debug.Log($"[Mazegen] Grid: {rows} logical rows × {cols} logical cols " +
                  $"→ {_mRows} × {_mCols} cells");

        SpawnMaze(_maze);

        if (autoFrameCamera)
            FrameCamera();
    }
    private OurTree<int> BuildSampleTree(int depth, int branches, int label = 0)
    {
        var node = new OurTree<int>(label);
        if (depth > 1)
            for (int i = 0; i < branches; i++)
                node.Children.Add(BuildSampleTree(depth - 1, branches, label * branches + i + 1));
        return node;
    }
    private void SpawnMaze(int[,] m)
    {
        int mRows = m.GetLength(0);
        int mCols = m.GetLength(1);
        const float wallY = 1.0f;

        for (int r = 0; r < mRows; r++)
            for (int c = 0; c < mCols; c++)
                if (m[r, c] == 0)
                {
                    Vector3 pos = new Vector3(c * spacing, wallY, r * spacing);
                    Instantiate(wallPrefab, pos, Quaternion.identity, mazeContainer);
                }
    }
    private void FrameCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[Mazegen] No MainCamera found – skipping auto-frame.");
            return;
        }
        float worldWidth = (_mCols - 1) * spacing;
        float worldDepth = (_mRows - 1) * spacing;
        Vector3 centre = new Vector3(worldWidth * 0.5f, 0f, worldDepth * 0.5f);
        float halfFov = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float halfExtent = Mathf.Max(worldWidth, worldDepth) * 0.5f;
        float camHeight = (halfExtent / Mathf.Tan(halfFov)) * cameraHeightMultiplier;

        cam.transform.position = centre + Vector3.up * camHeight;
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Debug.Log($"[Mazegen] Camera framed at {cam.transform.position}, height={camHeight:F1}");
    }
    private void OnDrawGizmos()
    {
        OurTree<int> previewTree = BuildSampleTree(treeDepth, treeBranches);
        int[,] m = TreeMazeBuilder.Build(previewTree, out _, out _);
        int mRows = m.GetLength(0);
        int mCols = m.GetLength(1);

        Vector3 origin = transform.position;

        for (int r = 0; r < mRows; r++)
        {
            for (int c = 0; c < mCols; c++)
            {
                Vector3 pos = origin + new Vector3(c * spacing, 1f, r * spacing);

                if (m[r, c] == 0)
                {
                    Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.6f);
                    Gizmos.DrawCube(pos, Vector3.one * spacing * 0.9f);
                }
                else
                {
                    Gizmos.color = new Color(1f, 1f, 0.2f, 0.15f);
                    Gizmos.DrawWireCube(pos, Vector3.one * spacing * 0.9f);
                }
            }
        }
        float w = (mCols - 1) * spacing;
        float d = (mRows - 1) * spacing;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(
            origin + new Vector3(w * 0.5f, 1f, d * 0.5f),
            new Vector3(w + spacing, spacing, d + spacing));
    }
}
