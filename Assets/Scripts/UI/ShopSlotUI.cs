using UnityEngine;
using UnityEngine.UI;

public class ShopSlotUI : MonoBehaviour
{
    [Header("Unit Info")]
    public GameObject unitPrefab;   // prefab to spawn when bought
    public int cost;

    [Header("UI")]
    public Button buyButton;

    private void Awake()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);
    }

    public void OnBuyClicked()
    {
        ShopManager.Instance.TryBuyUnit(this);
    }
    public GameObject GetUnitPrefab()
    {
        // Spawn with default forward rotation
        GameObject obj = Instantiate(unitPrefab);
        obj.transform.rotation = Quaternion.Euler(0,90,0); // force forward
        return obj;
    }

}
