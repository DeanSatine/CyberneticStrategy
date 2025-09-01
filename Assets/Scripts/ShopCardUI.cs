using UnityEngine;
using UnityEngine.UI;

public class ShopCardUI : MonoBehaviour
{
    private ShopUnit unit;

    public void Init(ShopUnit newUnit)
    {
        unit = newUnit;
        GetComponent<Button>().onClick.AddListener(OnClickBuy);
    }

    private void OnClickBuy()
    {
        int index = ShopManager.Instance.currentShop.IndexOf(unit);
        if (index >= 0)
        {
            ShopManager.Instance.BuyUnit(index);
            ShopUIManager.Instance.RefreshShopUI();
        }
    }
}
