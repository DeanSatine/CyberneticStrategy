using UnityEngine;

public enum TileType
{
    Board,
    Bench
}

public class HexTile : MonoBehaviour
{
    public TileType tileType;
    public UnitAI occupyingUnit; // ✅ Track which unit is on this tile
}
