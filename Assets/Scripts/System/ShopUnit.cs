using UnityEngine;

public class ShopUnit
{
    public string unitName;
    public GameObject prefab;
    [Range(1, 5)] public int cost;   // 1 = Needlebot, 2 = B.O.P., etc.
    public Trait[] traits;
}