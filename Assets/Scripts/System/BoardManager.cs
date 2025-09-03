using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;

    // Fast lookup: grid coordinates → tile
    private Dictionary<Vector2Int, HexTile> tiles = new Dictionary<Vector2Int, HexTile>();
    [Header("Hex Neighbor Settings")]
    [Tooltip("Expected center-to-center distance between adjacent hex tiles.")]
    public float neighborDistance = 0f; // 0 = auto-detect
    [Range(0.05f, 0.3f)]
    [Tooltip("How much tolerance we allow when testing if two tiles are neighbors.")]
    public float neighborTolerance = 0.15f;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public List<HexTile> FindPath(HexTile start, HexTile goal)
    {
        Queue<HexTile> frontier = new Queue<HexTile>();
        Dictionary<HexTile, HexTile> cameFrom = new Dictionary<HexTile, HexTile>();

        frontier.Enqueue(start);
        cameFrom[start] = null;

        while (frontier.Count > 0)
        {
            HexTile current = frontier.Dequeue();

            if (current == goal)
                break;

            foreach (var neighborPos in current.GetNeighbors())
            {
                HexTile neighbor = GetTileAt(neighborPos);
                if (neighbor == null) continue;

                // Skip occupied tiles unless it's the goal
                if (neighbor.occupyingUnit != null && neighbor != goal)
                    continue;

                if (!cameFrom.ContainsKey(neighbor))
                {
                    frontier.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                }
            }
        }

        // ✅ Reconstruct path
        List<HexTile> path = new List<HexTile>();
        HexTile step = goal;

        while (step != null && cameFrom.ContainsKey(step))
        {
            path.Insert(0, step);
            step = cameFrom[step];
        }

        return path;
    }
    public bool AreNeighbors(HexTile a, HexTile b, float hexSize = 1f)
    {
        if (a == null || b == null) return false;
        return Vector3.Distance(a.transform.position, b.transform.position) <= hexSize * 1.1f;
    }
    public List<HexTile> GetNeighbors(HexTile tile)
    {
        var result = new List<HexTile>();
        if (tile == null) return result;

        foreach (var kv in tiles)
        {
            HexTile candidate = kv.Value;
            if (candidate == tile) continue;

            float dist = Vector3.Distance(tile.transform.position, candidate.transform.position);

            // treat as adjacent if ~1 hex away
            if (Mathf.Abs(dist - neighborDistance) <= neighborDistance * neighborTolerance)
                result.Add(candidate);
        }

        return result;
    }

    public HexTile GetClosestFreeNeighbor(HexTile enemyTile, HexTile fromTile)
    {
        if (enemyTile == null) return null;

        List<HexTile> neighbors = GetNeighbors(enemyTile); // <-- you must have this already
        HexTile best = null;
        float bestDist = Mathf.Infinity;

        foreach (var n in neighbors)
        {
            if (n.occupyingUnit != null) continue; // skip occupied tiles

            float dist = Vector3.Distance(fromTile.transform.position, n.transform.position);
            if (dist < bestDist)
            {
                best = n;
                bestDist = dist;
            }
        }

        return best;
    }


    /// Register a tile in the board dictionary
    public void RegisterTile(Vector2Int coord, HexTile tile)
    {
        if (!tiles.ContainsKey(coord))
            tiles.Add(coord, tile);
    }

    /// Lookup tile at grid coordinates
    public HexTile GetTileAt(Vector2Int coord)
    {
        tiles.TryGetValue(coord, out var tile);
        return tile;
    }

    /// Approximate: find the closest tile in world space (useful for MoveTowards)
    public HexTile GetTileFromWorld(Vector3 worldPos)
    {
        float bestDist = float.MaxValue;
        HexTile bestTile = null;

        foreach (var kv in tiles)
        {
            float dist = Vector3.Distance(worldPos, kv.Value.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestTile = kv.Value;
            }
        }

        return bestTile;
    }
}
