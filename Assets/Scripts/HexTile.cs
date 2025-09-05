using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
    Board,
    Bench
}
public enum TileOwner
{
    Neutral,
    Player,
    Enemy
}
public class HexTile : MonoBehaviour
{
    public TileType tileType;
    public TileOwner owner = TileOwner.Neutral; // ✅ new field
    public UnitAI occupyingUnit; // ✅ Track which unit is on this tile
    public Vector2Int gridPosition;
  
    public bool TryClaim(UnitAI unit)
    {
        if (occupyingUnit != null && occupyingUnit != unit)
            return false;

        occupyingUnit = unit;
        unit.currentTile = this;
        return true;
    }

    public void Free(UnitAI unit)
    {
        if (occupyingUnit == unit)
        {
            occupyingUnit = null;
            unit.currentTile = null;
        }
    }
    public IEnumerable<Vector2Int> GetNeighbors()
    {
        foreach (var offset in HexMetrics.neighborOffsets)
        {
            yield return gridPosition + offset;
        }
    }
    public static HexTile FindNearestFreeTile(HexTile origin)
    {
        Queue<HexTile> queue = new Queue<HexTile>();
        HashSet<HexTile> visited = new HashSet<HexTile>();

        queue.Enqueue(origin);
        visited.Add(origin);

        while (queue.Count > 0)
        {
            HexTile current = queue.Dequeue();
            if (current.occupyingUnit == null)
                return current;

            foreach (var neighborPos in current.GetNeighbors())
            {
                HexTile neighbor = BoardManager.Instance.GetTileAt(neighborPos);
                if (neighbor != null && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return null;
    }

}
