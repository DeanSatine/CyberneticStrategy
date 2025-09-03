using UnityEngine;

public static class HexMetrics
{
    // Cube-like neighbor offsets for axial hex coords (flat-topped assumed)
    public static readonly Vector2Int[] neighborOffsets = new Vector2Int[]
    {
        new Vector2Int(+1, 0),
        new Vector2Int(0, +1),
        new Vector2Int(-1, +1),
        new Vector2Int(-1, 0),
        new Vector2Int(0, -1),
        new Vector2Int(+1, -1),
    };
}
