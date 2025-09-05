using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;

    // 👇 single source of truth
    private readonly Dictionary<Vector2Int, HexTile> tiles = new Dictionary<Vector2Int, HexTile>();
    private readonly List<HexTile> allTiles = new List<HexTile>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- Registration & queries ---

    public void RegisterTile(Vector2Int coords, HexTile tile)
    {
        if (!tiles.ContainsKey(coords))
        {
            tiles.Add(coords, tile);
            allTiles.Add(tile);
        }
    }

    public HexTile GetTile(Vector2Int coords)
    {
        tiles.TryGetValue(coords, out var tile);
        return tile;
    }

    public List<HexTile> GetAllTiles() => allTiles;

    public List<HexTile> GetEnemyTiles() => allTiles.FindAll(t => t.owner == TileOwner.Enemy);
    public List<HexTile> GetPlayerTiles() => allTiles.FindAll(t => t.owner == TileOwner.Player);

    public HexTile GetTileAt(Vector2Int coord) => GetTile(coord);

    public HexTile GetTileFromWorld(Vector3 worldPos)
    {
        float bestDist = float.MaxValue;
        HexTile best = null;
        foreach (var kv in tiles)
        {
            float d = Vector3.Distance(worldPos, kv.Value.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = kv.Value;
            }
        }
        return best;
    }

    // --- Neighbors ---
    public List<HexTile> GetNeighbors(HexTile tile)
    {
        var result = new List<HexTile>();
        if (tile == null) return result;

        // 👇 convert IEnumerable → List
        foreach (var c in tile.GetNeighbors())
        {
            var n = GetTile(c);
            if (n != null) result.Add(n);
        }
        return result;
    }
    public bool AreNeighbors(HexTile a, HexTile b)
    {
        if (a == null || b == null) return false;
        foreach (var c in a.GetNeighbors())
        {
            if (c == b.gridPosition) return true;
        }
        return false;
    }


    // --- Pathfinding (BFS) ---

    public List<HexTile> FindPath(HexTile start, HexTile goal)
    {
        var path = new List<HexTile>();
        if (start == null || goal == null) return path;

        var frontier = new Queue<HexTile>();
        var cameFrom = new Dictionary<HexTile, HexTile>();

        frontier.Enqueue(start);
        cameFrom[start] = null;

        while (frontier.Count > 0)
        {
            HexTile current = frontier.Dequeue();
            if (current == goal) break;

            foreach (var neighbor in GetNeighbors(current))
            {
                // skip occupied tiles unless it’s the goal
                if (neighbor.occupyingUnit != null && neighbor != goal) continue;
                if (cameFrom.ContainsKey(neighbor)) continue;

                cameFrom[neighbor] = current;
                frontier.Enqueue(neighbor);
            }
        }

        // Reconstruct
        if (!cameFrom.ContainsKey(goal)) return path; // no route found

        var step = goal;
        while (step != null)
        {
            path.Insert(0, step);
            step = cameFrom[step];
        }
        return path;
    }

    public HexTile GetClosestFreeNeighbor(HexTile enemyTile, HexTile fromTile)
    {
        if (enemyTile == null) return null;

        var neighbors = GetNeighbors(enemyTile);
        HexTile best = null;
        float bestDist = Mathf.Infinity;

        foreach (var n in neighbors)
        {
            if (n.occupyingUnit != null) continue;
            float d = Vector3.Distance(fromTile.transform.position, n.transform.position);
            if (d < bestDist)
            {
                best = n;
                bestDist = d;
            }
        }
        return best;
    }
}
