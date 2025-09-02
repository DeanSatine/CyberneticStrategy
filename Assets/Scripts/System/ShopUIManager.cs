using UnityEngine;
using TMPro;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance;

    [Header("Shop UI Setup")]
    public Transform[] shopSlots;      // assign Slot0–4 here in inspector
    public GameObject shopCardPrefab;  // prefab with image+button

    [Header("Extra UI")]
    public GameObject rerollButton;
    public TextMeshProUGUI goldText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RefreshShopUI()
    {
        for (int i = 0; i < shopSlots.Length; i++)
        {
            // clear previous card from slot
            foreach (Transform child in shopSlots[i])
                Destroy(child.gameObject);

            if (i < ShopManager.Instance.currentShop.Count)
            {
                ShopUnit unit = ShopManager.Instance.currentShop[i];

                // create card inside slot
                GameObject card = Instantiate(shopCardPrefab, shopSlots[i]);
                card.transform.localPosition = Vector3.zero;
                card.transform.localScale = Vector3.one;

                // init card with data
                card.GetComponent<ShopCardUI>().Init(unit);
            }
        }

        goldText.text = $"Gold: {EconomyManager.Instance.currentGold}";
    }
}
