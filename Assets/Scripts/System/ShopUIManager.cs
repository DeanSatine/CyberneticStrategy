using UnityEngine;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance;

    public ShopSlotUI[] shopSlots; // assign your 5 ShopSlot_X here

    private void Awake()
    {
        Instance = this;
    }

    public void RefreshShopUI()
    {
        // deactivate all slots first
        foreach (var slot in shopSlots)
            slot.gameObject.SetActive(false);

        // ask ShopManager what the shop should be
        ShopManager.Instance.FillShop(shopSlots);
    }
}
