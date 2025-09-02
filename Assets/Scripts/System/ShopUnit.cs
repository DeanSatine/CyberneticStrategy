using UnityEngine;

[System.Serializable]
public class ShopUnit
{
    public string unitName;
    public GameObject prefab;
    [Range(1, 5)] public int cost;   // 1 = Needlebot, 2 = B.O.P., etc.
    public Trait[] traits;

    [Header("Shop Card Art")]
    public Sprite shopSprite; // card image (with cost/name baked in)
}
