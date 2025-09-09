using UnityEngine;
using UnityEngine.UI;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance;

    [Header("Shop Controls")]
    public Button rerollButton;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Setup reroll button
        if (rerollButton != null)
        {
            rerollButton.onClick.AddListener(OnRerollClicked);
        }

    }

    private void OnRerollClicked()
    {
        ShopManager.Instance.RerollShop();
    }


    public void RefreshShopUI()
    {
        // This will be called when the shop needs to be refreshed
        ShopManager.Instance.GenerateShop();
    }
}
