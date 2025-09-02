using UnityEngine;
using UnityEngine.UI;

public class ShopCardUI : MonoBehaviour
{
    private ShopUnit unit;
    private Image cardImage;

    private void Awake()
    {
        cardImage = GetComponent<Image>();
    }

    public void Init(ShopUnit newUnit)
    {
        unit = newUnit;
        if (cardImage != null && unit.shopSprite != null)
            cardImage.sprite = unit.shopSprite;

        // reset button event
        GetComponent<Button>().onClick.RemoveAllListeners();
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
