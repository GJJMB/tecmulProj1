using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class EnemyController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player GameObject to follow.")]
    public Transform player;

    [Header("Movement")]
    [Tooltip("Speed at which the enemy moves towards the player.")]
    public float moveSpeed = 3f;

    [Tooltip("Minimum distance to maintain from the player.")]
    public float minDistance = 1f;

    [Header("Game Over")]
    [Tooltip("GameObject containing the GameOverScreen component.")]
    public GameObject gameOverController;
    private GameOverScreen gameOverScreen;

    [Header("Maze Grid Reference")]
    [Tooltip("The MazeGridController that provides maze data.")]
    public MazeGridController grid;

    private Queue<Vector3> path = new Queue<Vector3>();

    // Direction vectors matching MazeGenerator's N/E/S/W order
    private static readonly int[] DX = { 0, 1, 0, -1 };
    private static readonly int[] DY = { 1, 0, -1, 0 };

    void Start()
    {
        // Load enemy move time from GameSetup (settings)
        moveTime = GameSetup.EnemyMoveTime;
        Debug.Log($"EnemyController: Enemy moveTime set to {moveTime}");

        // agent = GetComponent<NavMeshAgent>();
        // if (agent == null)
        // {
        //     Debug.LogError("EnemyController: NavMeshAgent component is missing!");
        //     return;
        // }

        // agent.speed = moveSpeed;
        // agent.stoppingDistance = minDistance;

        // Get GameOverScreen component
        if (gameOverController != null)
        {
            gameOverScreen = gameOverController.GetComponent<GameOverScreen>();
            if (gameOverScreen == null)
            {
                Debug.LogError("EnemyController: gameOverController does not have a GameOverScreen component!");
            }
        }
        else
        {
            Debug.LogWarning("EnemyController: gameOverController is not assigned!");
        }

        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("EnemyController: Player found automatically by tag.");
            }
            else
            {
                Debug.LogError("EnemyController: Player not assigned and not found by tag!");
            }
        }

        // Assign the grid reference dynamically
        grid = FindObjectOfType<MazeGridController>();
        if (grid == null)
        {
            Debug.LogError("EnemyController: MazeGridController not found!");
            return;
        }

        // Start pathfinding
        StartCoroutine(UpdatePath());
    }

    void Update()
    {
        // No NavMeshAgent movement; handled by MoveTo coroutine
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if collided with player
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Enemy touched player! Game Over!");
            TriggerGameOver();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Alternative collision detection using triggers
        if (other.CompareTag("Player"))
        {
        Debug.Log("the player has died");
            Debug.Log("Enemy touched player! Game Over!");
            TriggerGameOver();
        }
        else
        {
        Debug.Log("thats interesting, the enemy collided with " + other.name + " but not the player");
        Debug.Log("its tag is " + other.tag);
        }
    }

    private void TriggerGameOver()
    {
        if (gameOverScreen != null)
        {
            gameOverScreen.ShowGameOverScreen();
        }
        else
        {
            Debug.LogError("EnemyController: Cannot show game over screen - reference not set!");
        }
    }

    IEnumerator UpdatePath()
    {
        while (true)
        {
            if (grid != null && player != null)
            {
                Vector3 playerPos = player.position;
                Vector3 enemyPos = transform.position;

                var (enemyX, enemyY) = WorldToCell(enemyPos);
                var (playerX, playerY) = WorldToCell(playerPos);

                path = PathfindingAlgorithm(enemyX, enemyY, playerX, playerY);

                //Debug.Log($"Enemy path length: {path.Count}, from ({enemyX},{enemyY}) to ({playerX},{playerY})");

                if (path.Count > 0)
                {
                    Vector3 nextPos = path.Dequeue();
                    if (moveCoroutine != null)
                        StopCoroutine(moveCoroutine);
                    moveCoroutine = StartCoroutine(MoveTo(nextPos + Vector3.up * 1f));
                }
            }
            yield return new WaitForSeconds(0.5f); // Update path every 0.5 seconds
        }
    }
    
    private float moveTime = 0.2f; // match your player's moveTime
    
    IEnumerator MoveTo(Vector3 target)
    {
        Vector3 start   = transform.position;
        float   elapsed = 0f;
    
        while (elapsed < moveTime)
        {
            elapsed           += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, elapsed / moveTime);
            yield return null;
        }
    
        transform.position = target; // snap to exact target at the end
    }

        private Queue<Vector3> PathfindingAlgorithm(int startX, int startY, int targetX, int targetY)
    {
        // A* Pathfinding Algorithm
        PriorityQueue<Node> openSet = new PriorityQueue<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        Node startNode = new Node(startX, startY, 0, Heuristic(startX, startY, targetX, targetY));
        openSet.Enqueue(startNode);

        Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();

        while (openSet.Count > 0)
        {
            Node current = openSet.Dequeue();

            if (current.X == targetX && current.Y == targetY)
            {
                return ReconstructPath(cameFrom, current);
            }

            closedSet.Add(current);

            foreach (Node neighbor in GetNeighbors(current))
            {
                if (closedSet.Contains(neighbor))
                    continue;

                float tentativeGScore = current.G + 1; // Assuming uniform cost

                if (!openSet.Contains(neighbor))
                {
                    cameFrom[neighbor] = current;
                    neighbor.G = tentativeGScore;
                    neighbor.F = neighbor.G + Heuristic(neighbor.X, neighbor.Y, targetX, targetY);
                    openSet.Enqueue(neighbor);
                }
                else if (tentativeGScore < neighbor.G)
                {
                    cameFrom[neighbor] = current;
                    neighbor.G = tentativeGScore;
                    neighbor.F = neighbor.G + Heuristic(neighbor.X, neighbor.Y, targetX, targetY);
                }
            }
        }

        return new Queue<Vector3>(); // Return empty path if no solution
    }

    private float Heuristic(int x, int y, int targetX, int targetY)
    {
        return Mathf.Abs(x - targetX) + Mathf.Abs(y - targetY); // Manhattan distance
    }

    private IEnumerable<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        for (int dir = 0; dir < 4; dir++)
        {
            int nx = node.X + DX[dir];
            int ny = node.Y + DY[dir];

            // Check bounds before accessing IsPassable
            if (nx < 0 || ny < 0 || nx >= grid.Width || ny >= grid.Height)
                continue;

            if (grid.IsPassable(node.X, node.Y, dir))
            {
                neighbors.Add(new Node(nx, ny));
            }
        }

        return neighbors;
    }

    private Queue<Vector3> ReconstructPath(Dictionary<Node, Node> cameFrom, Node current)
    {
        Stack<Vector3> path = new Stack<Vector3>();
        while (cameFrom.ContainsKey(current))
        {
            path.Push(grid.CellToWorld(current.X, current.Y));
            current = cameFrom[current];
        }

        return new Queue<Vector3>(path);
    }

    private class Node : IComparable<Node>
    {
        public int X, Y;
        public float G, F;

        public Node(int x, int y, float g = 0, float f = 0)
        {
            X = x;
            Y = y;
            G = g;
            F = f;
        }

        public int CompareTo(Node other)
        {
            return F.CompareTo(other.F);
        }

        public override bool Equals(object obj)
        {
            if (obj is Node other)
            {
                return X == other.X && Y == other.Y;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }
    }

    // Simple PriorityQueue implementation for A*
    private class PriorityQueue<T> where T : IComparable<T>
    {
        private List<T> data = new List<T>();

        public int Count => data.Count;

        public void Enqueue(T item)
        {
            data.Add(item);
            int ci = data.Count - 1;
            while (ci > 0)
            {
                int pi = (ci - 1) / 2;
                if (data[ci].CompareTo(data[pi]) >= 0) break;
                (data[ci], data[pi]) = (data[pi], data[ci]);
                ci = pi;
            }
        }

        public T Dequeue()
        {
            int li = data.Count - 1;
            T frontItem = data[0];
            data[0] = data[li];
            data.RemoveAt(li);
            --li;
            int pi = 0;
            while (true)
            {
                int ci = pi * 2 + 1;
                if (ci > li) break;
                int rc = ci + 1;
                if (rc <= li && data[rc].CompareTo(data[ci]) < 0)
                    ci = rc;
                if (data[pi].CompareTo(data[ci]) <= 0) break;
                (data[pi], data[ci]) = (data[ci], data[pi]);
                pi = ci;
            }
            return frontItem;
        }

        public bool Contains(T item) => data.Contains(item);
    }

    // Converts a world position to grid cell coordinates (x, y)
    private (int x, int y) WorldToCell(Vector3 worldPos)
    {
        if (grid == null || grid.Generator == null) return (0, 0);
        Vector3 localPos = worldPos - grid.transform.position;
        int x = Mathf.FloorToInt(localPos.x / grid.Generator.cellSize + 0.5f);
        int y = Mathf.FloorToInt(localPos.z / grid.Generator.cellSize + 0.5f);
        return (x, y);
    }

    private Coroutine moveCoroutine;
}