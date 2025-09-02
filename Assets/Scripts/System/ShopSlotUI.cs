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

    private void OnBuyClicked()
    {
        ShopManager.Instance.TryBuyUnit(this);
    }
}
