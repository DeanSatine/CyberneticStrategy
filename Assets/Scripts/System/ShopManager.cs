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
        // ✅ DON'T generate shop immediately in PvP mode
        if (PvPGameManager.Instance != null && PvPGameManager.Instance.enabled)
        {
            Debug.Log("🛒 PvP Mode: Waiting for board assignment before generating shop");

            // ✅ Clear any existing bench slots from inspector
            if (benchSlots != null && benchSlots.Length > 0)
            {
                Debug.Log($"🛒 Clearing {benchSlots.Length} inspector-assigned bench slots (will be set dynamically)");
                benchSlots = new Transform[0];
            }

            return;
        }

        // Single-player mode: generate immediately
        GenerateShop();
    }


    // ✅ Add this method to be called after board is assigned
    public void InitializeForPvP()
    {
        Debug.Log("🛒 Initializing shop for PvP mode");
        GenerateShop();
    }


    public void GenerateShop()
    {
        Debug.Log("🛒 Generating new shop...");

        // ✅ CRITICAL: Check if shop slots are set up
        if (shopSlotParents == null || shopSlotParents.Length == 0)
        {
            Debug.LogError("❌ shopSlotParents is null or empty! Shop cannot generate.");
            return;
        }

        // ✅ Check if we have available cards
        if (availableCards == null || availableCards.Length == 0)
        {
            Debug.LogError("❌ availableCards is null or empty! Shop cannot generate.");
            return;
        }

        // ✅ CLEAR existing shop first
        ClearShop();

        // Get cards available for current stage
        List<ShopCard> availableForStage = GetCardsForStage();

        if (availableForStage == null || availableForStage.Count == 0)
        {
            Debug.LogError("❌ No cards available for current stage!");
            return;
        }

        // Randomly select 5 cards
        List<ShopCard> selectedCards = SelectRandomCards(availableForStage, 5);

        // Instantiate selected cards in slots
        for (int i = 0; i < selectedCards.Count && i < shopSlotParents.Length; i++)
        {
            if (shopSlotParents[i] == null)
            {
                Debug.LogError($"❌ shopSlotParents[{i}] is null!");
                continue;
            }

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
    public void SetBenchSlots(List<HexTile> newBenchTiles)
    {
        benchSlots = new Transform[newBenchTiles.Count];
        for (int i = 0; i < newBenchTiles.Count; i++)
        {
            benchSlots[i] = newBenchTiles[i].transform;
        }
        Debug.Log($"✅ ShopManager bench updated to {benchSlots.Length} slots");
    }

    private List<ShopCard> GetCardsForStage()
    {
        int stage = 1;

        if (PvPGameManager.Instance != null && PvPGameManager.Instance.enabled)
        {
            int round = PvPGameManager.Instance.currentRound;
            stage = round <= 3 ? 1 : round <= 6 ? 2 : 3;
            Debug.Log($"🛒 PvP Mode: Round {round} → Stage {stage} shop pool");
        }
        else if (StageManager.Instance != null)
        {
            stage = StageManager.Instance.currentStage;
            Debug.Log($"🛒 Single-Player Mode: Stage {stage} shop pool");
        }

        List<ShopCard> availableForStage = new List<ShopCard>();

        foreach (var card in availableCards)
        {
            if (IsCardAvailableForStage(card.tier, stage))
            {
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
        Debug.Log($"🛒 TryBuyUnit called for {slot.unitPrefab.name}, cost: {slot.cost}");

        if (!EconomyManager.Instance.SpendGold(slot.cost))
        {
            Debug.Log("💸 Not enough gold!");
            return;
        }

        // ✅ CRITICAL: Make sure benchSlots is populated
        if (benchSlots == null || benchSlots.Length == 0)
        {
            Debug.LogError("❌ benchSlots is null or empty! Shop not properly initialized.");
            EconomyManager.Instance.AddGold(slot.cost);
            return;
        }

        // ✅ Find first empty bench slot using HexTile system
        Transform freeSlot = null;
        HexTile freeTile = null;

        foreach (Transform benchSlot in benchSlots)
        {
            if (benchSlot == null)
            {
                Debug.LogWarning("⚠️ benchSlot is null, skipping");
                continue;
            }

            HexTile hexTile = benchSlot.GetComponent<HexTile>();
            if (hexTile == null)
            {
                Debug.LogWarning($"⚠️ {benchSlot.name} has no HexTile component");
                continue;
            }

            if (hexTile.tileType == TileType.Bench && hexTile.occupyingUnit == null)
            {
                freeSlot = benchSlot;
                freeTile = hexTile;
                Debug.Log($"✅ Found free bench slot: {benchSlot.name}");
                break;
            }
        }

        if (freeSlot == null)
        {
            Debug.Log("🪑 Bench is full!");
            EconomyManager.Instance.AddGold(slot.cost);
            return;
        }

        // ✅ Calculate spawn position BEFORE instantiating
        Vector3 spawnPosition = freeTile.transform.position;
        spawnPosition.y += 0.0f;  

        Debug.Log($"📍 Spawning unit at tile position: {spawnPosition}");

        // Spawn unit on free bench slot
        GameObject unit = Instantiate(slot.unitPrefab, spawnPosition, Quaternion.identity);
        unit.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        Debug.Log($"📍 Unit spawned at final position: {unit.transform.position}");

        // ✅ CRITICAL: Properly claim the tile
        UnitAI newUnit = unit.GetComponent<UnitAI>();
        if (newUnit == null)
        {
            Debug.LogError($"❌ {slot.unitPrefab.name} has no UnitAI component!");
            Destroy(unit);
            EconomyManager.Instance.AddGold(slot.cost);
            return;
        }

        if (!freeTile.TryClaim(newUnit))
        {
            Debug.LogError("❌ Failed to claim tile!");
            Destroy(unit);
            EconomyManager.Instance.AddGold(slot.cost);
            return;
        }

        // Set proper state
        newUnit.currentState = UnitAI.UnitState.Bench;
        newUnit.team = Team.Player;
        newUnit.teamID = 1;

        // ✅ Register new unit with GameManager
        TestGameManager.Instance.RegisterUnit(newUnit, true);

        Debug.Log($"✅ Bought {slot.unitPrefab.name} for {slot.cost} gold, placed on bench at {freeSlot.name}");

        // Try merging
        TestGameManager.Instance.TryMergeUnits(newUnit);

        // Remove the bought card from shop
        if (currentShopInstances.Contains(slot.gameObject))
        {
            currentShopInstances.Remove(slot.gameObject);
        }
        Destroy(slot.gameObject);
    }


}
