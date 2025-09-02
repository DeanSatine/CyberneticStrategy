using UnityEngine;

[System.Serializable]
public class ShopUnit
{
    public string unitName;     // "Needlebot", "B.O.P.", etc.
    public GameObject prefab;   // Prefab that spawns on bench
    [Range(1, 5)]
    public int cost;            // Gold cost (1–5)
    public Sprite icon;         // Optional: used in UI
}
