using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

[System.Serializable]
public class ShopCard
{
    public GameObject cardPrefab;  // Drag your card prefabs here
    public int cost;              // Set the cost for each card
    public int tier;              // Determines when it appears (1-5)
}

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("Card Setup - Drag your card prefabs here")]
    public ShopCard[] availableCards;

    [Header("Shop Slots")]
    public Transform[] shopSlotParents;  // The 5 slot parent transforms

    [Header("Bench Settings")]
    public Transform[] benchSlots;

    [Header("Shop Settings")]
    [SerializeField] private int rerollCost = 2;

    // Track current shop instances
    private List<GameObject> currentShopInstances = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GenerateShop();
    }

    public void GenerateShop()
    {
        Debug.Log("🛒 Generating new shop...");

        // ✅ CLEAR existing shop first
        ClearShop();

        // Get cards available for current stage
        List<ShopCard> availableForStage = GetCardsForStage();

        // Randomly select 5 cards
        List<ShopCard> selectedCards = SelectRandomCards(availableForStage, 5);

        // Instantiate selected cards in slots
        for (int i = 0; i < selectedCards.Count && i < shopSlotParents.Length; i++)
        {
            InstantiateCardInSlot(selectedCards[i], i);
        }

        Debug.Log($"✅ Shop generated with {selectedCards.Count} units!");
    }

    // ✅ CRITICAL: This method clears the shop properly
    private void ClearShop()
    {
        // Destroy all current shop card instances
        foreach (GameObject cardInstance in currentShopInstances)
        {
            if (cardInstance != null)
                Destroy(cardInstance);
        }
        currentShopInstances.Clear();

        Debug.Log("🧹 Cleared existing shop cards");
    }

    private List<ShopCard> GetCardsForStage()
    {
        int stage = StageManager.Instance.currentStage;
        List<ShopCard> availableForStage = new List<ShopCard>();

        foreach (var card in availableCards)
        {
            if (IsCardAvailableForStage(card.tier, stage))
            {
                // Add multiple entries for weighted selection
                int weight = GetCardWeight(card.tier, stage);
                for (int w = 0; w < weight; w++)
                {
                    availableForStage.Add(card);
                }
            }
        }

        return availableForStage;
    }

    private bool IsCardAvailableForStage(int tier, int stage)
    {
        switch (stage)
        {
            case 1:
                return tier <= 2; // Only tier 1-2 units
            case 2:
                return tier <= 4; // Tier 1-4 units
            case 3:
            default:
                return true; // All tiers available
        }
    }

    private int GetCardWeight(int tier, int stage)
    {
        // Higher weight = more likely to appear
        switch (stage)
        {
            case 1:
                switch (tier)
                {
                    case 1: return 7; // 70% weight
                    case 2: return 3; // 30% weight
                    default: return 0;
                }
            case 2:
                switch (tier)
                {
                    case 1: return 3; // 30% weight
                    case 2: return 4; // 40% weight  
                    case 3: return 2; // 20% weight
                    case 4: return 1; // 10% weight
                    default: return 0;
                }
            case 3:
            default:
                switch (tier)
                {
                    case 1: return 1; // 10% weight
                    case 2: return 2; // 20% weight
                    case 3: return 3; // 30% weight
                    case 4: return 2; // 20% weight
                    case 5: return 2; // 20% weight
                    default: return 1;
                }
        }
    }

    private List<ShopCard> SelectRandomCards(List<ShopCard> available, int count)
    {
        List<ShopCard> selected = new List<ShopCard>();
        List<ShopCard> pool = new List<ShopCard>(available);

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            ShopCard selectedCard = pool[randomIndex];
            selected.Add(selectedCard);

            // Remove all instances of this card type from pool to avoid duplicates
            pool.RemoveAll(card => card.cardPrefab == selectedCard.cardPrefab);
        }

        return selected;
    }

    private void InstantiateCardInSlot(ShopCard shopCard, int slotIndex)
    {
        if (shopCard.cardPrefab == null || slotIndex >= shopSlotParents.Length)
            return;

        GameObject cardInstance = Instantiate(shopCard.cardPrefab, shopSlotParents[slotIndex]);

        RectTransform cardRect = cardInstance.GetComponent<RectTransform>();
        if (cardRect != null)
        {
            // ✅ FIX: Standardize anchors to center
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);

            // Now reset position
            cardRect.localPosition = Vector3.zero;
            cardRect.localRotation = Quaternion.identity;
            cardRect.localScale = new Vector3(8.9f, 8.9f, 8.9f);
            cardRect.anchoredPosition = Vector2.zero;
        }
        else
        {
            cardInstance.transform.localPosition = Vector3.zero;
            cardInstance.transform.localRotation = Quaternion.identity;
            cardRect.localScale = new Vector3(8.9f, 8.9f, 8.9f);
        }

        ShopSlotUI shopSlotUI = cardInstance.GetComponent<ShopSlotUI>();
        if (shopSlotUI != null)
        {
            shopSlotUI.cost = shopCard.cost;

            if (shopSlotUI.buyButton == null)
            {
                Button cardButton = cardInstance.GetComponent<Button>();
                if (cardButton == null)
                    cardButton = cardInstance.GetComponentInChildren<Button>();

                if (cardButton != null)
                {
                    shopSlotUI.buyButton = cardButton;
                    cardButton.onClick.RemoveAllListeners();
                    cardButton.onClick.AddListener(() => shopSlotUI.OnBuyClicked());
                }
                else
                {
                    cardButton = cardInstance.AddComponent<Button>();
                    shopSlotUI.buyButton = cardButton;
                    cardButton.onClick.AddListener(() => shopSlotUI.OnBuyClicked());
                }
            }
        }

        cardInstance.SetActive(true);
        currentShopInstances.Add(cardInstance);

        Debug.Log($"🃏 Instantiated {shopCard.cardPrefab.name} in slot {slotIndex} (Cost: {shopCard.cost})");
    }


    public void RerollShop()
    {
        Debug.Log("🎲 RerollShop method called!");

        if (!EconomyManager.Instance.SpendGold(rerollCost))
        {
            Debug.Log("💸 Not enough gold to reroll shop!");
            return;
        }

        Debug.Log($"🎲 Rerolling shop for {rerollCost} gold...");
        GenerateShop(); // This now clears first, then generates new
    }

    public void TryBuyUnit(ShopSlotUI slot)
    {
        if (!EconomyManager.Instance.SpendGold(slot.cost))
        {
            Debug.Log("💸 Not enough gold!");
            return;
        }

        // ✅ FIXED: Find first empty bench slot using HexTile system
        Transform freeSlot = null;
        HexTile freeTile = null;

        foreach (Transform benchSlot in benchSlots)
        {
            HexTile hexTile = benchSlot.GetComponent<HexTile>();
            if (hexTile != null && hexTile.tileType == TileType.Bench && hexTile.occupyingUnit == null)
            {
                freeSlot = benchSlot;
                freeTile = hexTile;
                break;
            }
        }

        if (freeSlot == null)
        {
            Debug.Log("🪑 Bench is full!");
            EconomyManager.Instance.AddGold(slot.cost);
            return;
        }

        // Spawn unit on free bench slot
        GameObject unit = Instantiate(slot.unitPrefab, freeSlot.position, Quaternion.identity);
        unit.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        // ✅ CRITICAL: Properly claim the tile instead of just setting parent
        UnitAI newUnit = unit.GetComponent<UnitAI>();
        if (!freeTile.TryClaim(newUnit))
        {
            Debug.LogError("Failed to claim free tile - this shouldn't happen!");
            Destroy(unit);
            EconomyManager.Instance.AddGold(slot.cost);
            return;
        }

        // Set proper state
        newUnit.currentState = UnitAI.UnitState.Bench;

        // ✅ Register new unit with GameManager so it's tracked
        GameManager.Instance.RegisterUnit(newUnit, true);

        Debug.Log($"✅ Bought {slot.unitPrefab.name} for {slot.cost} gold, placed on bench!");

        // ✅ Now merging will work
        GameManager.Instance.TryMergeUnits(newUnit);

        // Remove the bought card from shop by destroying it
        if (currentShopInstances.Contains(slot.gameObject))
        {
            currentShopInstances.Remove(slot.gameObject);
        }
        Destroy(slot.gameObject);
    }

}
