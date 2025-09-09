using UnityEngine;
using UnityEngine.UI;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance;

    [Header("Shop Controls")]
    public Button rerollButton;
    public TMPro.TMP_Text rerollCostText;

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

        // Update reroll cost display
        UpdateRerollCostDisplay();
    }

    private void OnRerollClicked()
    {
        ShopManager.Instance.RerollShop();
        UpdateRerollCostDisplay();
    }

    private void UpdateRerollCostDisplay()
    {
        if (rerollCostText != null)
        {
            rerollCostText.text = $"Reroll (2g)";
        }
    }

    public void RefreshShopUI()
    {
        // This will be called when the shop needs to be refreshed
        ShopManager.Instance.GenerateShop();
    }
}
