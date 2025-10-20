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
    public TileOwner owner = TileOwner.Neutral;
    public UnitAI occupyingUnit;
    public Vector2Int gridPosition;
    
    [Header("Highlight Settings")]
    public GameObject highlightPrefab;
    private GameObject activeHighlight;

    public bool TryClaim(UnitAI unit)
    {
        // ✅ If tile already has a unit, block placement (both board + bench)
        if (occupyingUnit != null && occupyingUnit != unit)
            return false;

        // ✅ Special case: bench should strictly allow only 1 unit
        if (tileType == TileType.Bench && occupyingUnit != null && occupyingUnit != unit)
            return false;

        occupyingUnit = unit;
        unit.currentTile = this;
        return true;
    }
    public void HighlightAsValid()
    {
        if (highlightPrefab == null) return;
        
        if (activeHighlight == null)
        {
            activeHighlight = Instantiate(highlightPrefab, transform.position + Vector3.up * 0.72f, Quaternion.Euler(0, 30.541f, 0));
            activeHighlight.transform.SetParent(transform);
        }
        
        activeHighlight.SetActive(true);
    }

    public void ClearHighlight()
    {
        if (activeHighlight != null)
        {
            activeHighlight.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (activeHighlight != null)
        {
            Destroy(activeHighlight);
        }
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
