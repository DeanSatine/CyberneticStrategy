using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance;

    [Header("Shop Slots")]
    public Button[] shopButtons;   // assign 5 buttons in inspector
    public TextMeshProUGUI[] shopTexts; // assign TMP labels for the 5 slots

    [Header("Extra UI")]
    public Button rerollButton;
    public TextMeshProUGUI goldText;  // assign in inspector

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        RefreshShopUI();

        // Hook reroll
        rerollButton.onClick.AddListener(() =>
        {
            ShopManager.Instance.RerollShop();
            RefreshShopUI();
        });
    }

    public void RefreshShopUI()
    {
        for (int i = 0; i < shopButtons.Length; i++)
        {
            if (i < ShopManager.Instance.currentShop.Count)
            {
                ShopUnit unit = ShopManager.Instance.currentShop[i];
                shopTexts[i].text = $"{unit.unitName}\n{unit.cost}g";

                int index = i; // local copy for lambda
                shopButtons[i].onClick.RemoveAllListeners();
                shopButtons[i].onClick.AddListener(() => TryBuyUnit(index));

                shopButtons[i].interactable = true;
            }
            else
            {
                shopTexts[i].text = "Empty";
                shopButtons[i].interactable = false;
            }
        }

        // update gold display
        goldText.text = $"Gold: {EconomyManager.Instance.currentGold}";
    }

    private void TryBuyUnit(int index)
    {
        ShopManager.Instance.BuyUnit(index);
        RefreshShopUI();
    }
}
