using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class ShopCardReference
{
    public string unitName;
    public GameObject cardGameObject;  // Reference to existing card in hierarchy
    public int tier;                   // 1-5, affects spawn rates
}

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("Existing Unit Cards")]
    public ShopCardReference[] availableCards;

    [Header("Shop Slots")]
    public Transform[] shopSlotParents;  // The 5 slot parent transforms

    [Header("Bench Settings")]
    public Transform[] benchSlots; // assign your 9 bench slot transforms in inspector

    [Header("Shop Settings")]
    [SerializeField] private int rerollCost = 2;
    [SerializeField] private int unitsPerShop = 5;

    private void Awake()
    {
        Instance = this;

        // Auto-populate cards if not set up
        if (availableCards == null || availableCards.Length == 0)
        {
            SetupCardsFromHierarchy();
        }
    }

    private void Start()
    {
        GenerateShop();
    }

    private void SetupCardsFromHierarchy()
    {
        // Find all ShopSlotUI components in the scene
        ShopSlotUI[] allShopSlots = FindObjectsOfType<ShopSlotUI>();
        List<ShopCardReference> foundCards = new List<ShopCardReference>();

        foreach (var slot in allShopSlots)
        {
            string cardName = slot.gameObject.name;
            int tier = DetermineTierFromName(cardName);

            foundCards.Add(new ShopCardReference
            {
                unitName = cardName,
                cardGameObject = slot.gameObject,
                tier = tier
            });
        }

        availableCards = foundCards.ToArray();
        Debug.Log($"🔍 Auto-discovered {availableCards.Length} shop cards");
    }

    private int DetermineTierFromName(string cardName)
    {
        // Determine tier based on card name
        string name = cardName.ToLower();
        if (name.Contains("bop")) return 1;
        if (name.Contains("needle")) return 2;
        if (name.Contains("mana")) return 3;
        if (name.Contains("kill")) return 4;
        if (name.Contains("hay")) return 5;

        return 1; // Default tier
    }

    public void GenerateShop()
    {
        Debug.Log("🛒 Generating new shop...");

        // First, deactivate all cards
        foreach (var card in availableCards)
        {
            if (card.cardGameObject != null)
                card.cardGameObject.SetActive(false);
        }

        // Get cards available for current stage
        List<ShopCardReference> availableForStage = GetCardsForStage();

        // Randomly select cards to show
        List<ShopCardReference> selectedCards = SelectRandomCards(availableForStage, unitsPerShop);

        // Activate selected cards and position them in slots
        for (int i = 0; i < selectedCards.Count && i < shopSlotParents.Length; i++)
        {
            ActivateCardInSlot(selectedCards[i], i);
        }

        Debug.Log($"✅ Shop generated with {selectedCards.Count} units!");
    }

    private List<ShopCardReference> GetCardsForStage()
    {
        int stage = StageManager.Instance.currentStage;
        List<ShopCardReference> availableForStage = new List<ShopCardReference>();

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

    private List<ShopCardReference> SelectRandomCards(List<ShopCardReference> available, int count)
    {
        List<ShopCardReference> selected = new List<ShopCardReference>();
        List<ShopCardReference> pool = new List<ShopCardReference>(available);

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            ShopCardReference selectedCard = pool[randomIndex];
            selected.Add(selectedCard);

            // Remove all instances of this card from pool to avoid duplicates
            pool.RemoveAll(card => card.cardGameObject == selectedCard.cardGameObject);
        }

        return selected;
    }

    private void ActivateCardInSlot(ShopCardReference card, int slotIndex)
    {
        if (card.cardGameObject == null || slotIndex >= shopSlotParents.Length)
            return;

        // Remember original world scale before moving
        Vector3 worldScale = card.cardGameObject.transform.lossyScale;

        // Parent the card to the slot, but don't keep world position
        card.cardGameObject.transform.SetParent(shopSlotParents[slotIndex], false);

        // Snap to slot center
        card.cardGameObject.transform.localPosition = Vector3.zero;

        // Restore world scale so the card looks the same size as before
        Vector3 parentScale = shopSlotParents[slotIndex].lossyScale;
        card.cardGameObject.transform.localScale = new Vector3(
            worldScale.x / parentScale.x,
            worldScale.y / parentScale.y,
            worldScale.z / parentScale.z
        );

        // Show the card
        card.cardGameObject.SetActive(true);

        Debug.Log($"🃏 Activated {card.unitName} in slot {slotIndex}, preserved world scale {worldScale}");
    }


    public void RerollShop()
    {
        if (!EconomyManager.Instance.SpendGold(rerollCost))
        {
            Debug.Log("💸 Not enough gold to reroll shop!");
            return;
        }

        Debug.Log($"🎲 Rerolling shop for {rerollCost} gold...");
        GenerateShop();
    }

    public void TryBuyUnit(ShopSlotUI slot)
    {
        if (!EconomyManager.Instance.SpendGold(slot.cost))
        {
            Debug.Log("💸 Not enough gold!");
            return;
        }

        // Find first empty bench slot
        Transform freeSlot = null;
        foreach (Transform benchSlot in benchSlots)
        {
            if (benchSlot.childCount == 0)
            {
                freeSlot = benchSlot;
                break;
            }
        }

        if (freeSlot == null)
        {
            Debug.Log("🪑 Bench is full!");
            // Refund the gold
            EconomyManager.Instance.AddGold(slot.cost);
            return;
        }

        // Spawn unit on free bench slot
        GameObject unit = Instantiate(slot.unitPrefab, freeSlot.position, Quaternion.identity);
        unit.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        unit.transform.SetParent(freeSlot);

        Debug.Log($"✅ Bought {slot.unitPrefab.name} for {slot.cost} gold, placed on bench!");

        // Hide the bought card from shop
        slot.gameObject.SetActive(false);
    }

    // Public methods for UI
    public void OnRerollButtonClicked()
    {
        RerollShop();
    }

    public void RefreshShop()
    {
        GenerateShop();
    }

    // Legacy compatibility
    public void FillShop(ShopSlotUI[] slots)
    {
        GenerateShop();
    }
}
