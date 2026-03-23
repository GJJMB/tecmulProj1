using UnityEngine;

public static class TreeMazeBuilder
{
    public static int[,] Build<T>(Tree<T> root, out int rows, out int cols)
    {
        var coords = new Dictionary<Tree<T>, (int r, int c)>();
        AssignCoords(root, coords);

        int maxR = 0, maxC = 0;
        foreach (var (r, c) in coords.Values)
        {
            if (r > maxR) { maxR = r; }
            if (c > maxC) { maxC = c; }
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
    private static int SubtreeWidth<T>(Tree<T> node)
    {
        if (node.Children.Count == 0) return 1;
        int w = 0;
        foreach (var c in node.Children) w += SubtreeWidth(c);
        return w;
    }

    private static void AssignCoords<T>(
        Tree<T> root,
        Dictionary<Tree<T>, (int r, int c)> coords)
    {
        void Recurse(Tree<T> node, int depth, int colOffset)
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
        Tree<T> node,
        Dictionary<Tree<T>, (int r, int c)> coords,
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

    /// <summary></summary>
    public static void PrintMatrix(int[,] m)
    {
        int mRows = m.GetLength(0);
        int mCols = m.GetLength(1);
        Console.Write("   ");
        for (int c = 0; c < mCols; c++) Console.Write($"{c,2}");
        Console.WriteLine();
        for (int r = 0; r < mRows; r++)
        {
            Console.Write($"{r,2} ");
            for (int c = 0; c < mCols; c++)
                Console.Write($"{m[r, c],2}");
            Console.WriteLine();
        }
    }
}



public class Mazegen
{
    public static void Main()
    {
        TreeMazeBuilder tree = new();
        TreeMazeBuilder t = new();
        SpawnMaze(t.Build(tree, 35, 35));
    }
    private void SpawnMaze(int[,] m)
    {
        int mRows = m.GetLength(0);
        int mCols = m.GetLength(1);
        float cartesianZ = 1.0f;
        for (int r = 0; r < mRows; r++)
        {
            for (int c = 0; c < mCols; c++)
            {
                if (m[r, c] == 0)
                {
                    Vector3 position = new Vector3(r * spacing, cartesianZ, c * spacing);
                    Instantiate(wallPrefab, position, Quaternion.identity, mazeContainer.transform);
                }
            }
        }
    }
}
