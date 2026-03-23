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

    void Start()
    {
        
        OurTree<int> tree = BuildSampleTree(treeDepth, treeBranches);
        int[,] maze = TreeMazeBuilder.Build(tree, out int rows, out int cols);
        Debug.Log($"Maze grid: {rows} logical rows, {cols} logical cols " +
                  $"→ {maze.GetLength(0)} × {maze.GetLength(1)} cells");
        SpawnMaze(maze);
    }
    private OurTree<int> BuildSampleTree(int depth, int branches, int label = 0)
    {
        var node = new OurTree<int>(label);
        if (depth > 1)
        {
            for (int i = 0; i < branches; i++)
                node.Children.Add(BuildSampleTree(depth - 1, branches, label * branches + i + 1));
        }
        return node;
    }
    private void SpawnMaze(int[,] m)
    {
        int mRows = m.GetLength(0);
        int mCols = m.GetLength(1);
        const float cartesianY = 1.0f;

        for (int r = 0; r < mRows; r++)
        {
            for (int c = 0; c < mCols; c++)
            {
                if (m[r, c] == 0)
                {
                    Vector3 position = new Vector3(c * spacing, cartesianY, r * spacing);
                    Instantiate(wallPrefab, position, Quaternion.identity, mazeContainer);
                }
            }
        }
    }
}
