using UnityEngine;
using UnityEngine.UI;
using static UnitAI;

public class SellZone : MonoBehaviour
{
    [Header("Sell Zone Settings")]
    [SerializeField] private float sellPercentage = 0.6f; // Sell for 60% of original cost
    [SerializeField] private Color normalColor = Color.red;
    [SerializeField] private Color highlightColor = Color.yellow;

    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMPro.TMP_Text sellText;

    [Header("Audio")]
    [SerializeField] private AudioClip sellSound;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (backgroundImage != null)
            backgroundImage.color = normalColor;

        // Add button component for clicking
        Button button = GetComponent<Button>();
        if (button == null)
            button = gameObject.AddComponent<Button>();

        button.onClick.AddListener(OnSellZoneClicked);
    }

    private void OnSellZoneClicked()
    {
        // Find if any unit is currently being dragged
        Draggable[] allDraggables = FindObjectsOfType<Draggable>();
        Draggable heldUnit = null;

        foreach (var draggable in allDraggables)
        {
            if (draggable.isDragging)
            {
                heldUnit = draggable;
                break;
            }
        }

        if (heldUnit == null)
        {
            Debug.Log("💡 Click while holding a unit to sell it");
            if (sellText != null)
            {
                sellText.text = "Hold a unit first";
                Invoke(nameof(ResetSellText), 1.5f);
            }
            return;
        }

        UnitAI unit = heldUnit.GetComponent<UnitAI>();
        if (unit == null)
        {
            Debug.Log("❌ Held object is not a sellable unit");
            return;
        }

        // Only allow selling player units
        if (unit.team != Team.Player)
        {
            Debug.Log("❌ Cannot sell enemy units");
            return;
        }

        // Only allow selling during prep phase (not during combat)
        if (StageManager.Instance != null &&
            StageManager.Instance.currentPhase == StageManager.GamePhase.Combat &&
            unit.currentState != UnitState.Bench)
        {
            Debug.Log("❌ Cannot sell units during combat (except benched units)");
            if (sellText != null)
            {
                sellText.text = "Can't sell in combat";
                Invoke(nameof(ResetSellText), 1.5f);
            }
            return;
        }

        // Calculate sell price
        int sellPrice = CalculateSellPrice(unit);

        if (sellPrice <= 0)
        {
            Debug.Log("❌ This unit cannot be sold");
            return;
        }

        // Perform the sale
        SellUnit(unit, sellPrice, heldUnit);
    }

    private int CalculateSellPrice(UnitAI unit)
    {
        // Find the base cost
        int baseCost = GetUnitOriginalCost(unit);
        if (baseCost <= 0)
        {
            baseCost = unit.starLevel; // fallback
        }

        // Factor in star level (2★ = 3x cost, 3★ = 9x cost)
        int effectiveCost = baseCost;
        if (unit.starLevel == 2)
            effectiveCost = baseCost * 3;
        else if (unit.starLevel == 3)
            effectiveCost = baseCost * 9;

        // Apply sell percentage
        int sellPrice = Mathf.RoundToInt(effectiveCost * sellPercentage);

        return Mathf.Max(1, sellPrice);
    }


    private int GetUnitOriginalCost(UnitAI unit)
    {
        // Search all shop slots to find this unit's cost
        ShopSlotUI[] allShopSlots = FindObjectsOfType<ShopSlotUI>();

        foreach (var slot in allShopSlots)
        {
            if (slot.unitPrefab != null &&
                slot.unitPrefab.name == unit.gameObject.name.Replace("(Clone)", ""))
            {
                return slot.cost;
            }
        }

        // If not found in shop, try to determine cost by unit name or type
        return GetFallbackCost(unit);
    }

    private int GetFallbackCost(UnitAI unit)
    {
        // Fallback cost determination based on unit name or properties
        string unitName = unit.unitName.ToLower();

        if (unitName.Contains("haymaker")) return 5;
        if (unitName.Contains("killswitch")) return 4;
        if (unitName.Contains("manadrive")) return 3;
        if (unitName.Contains("needlebot")) return 1;
        if (unitName.Contains("b.o.p")) return 2;

        // Default based on star level
        return unit.starLevel;
    }

    private void SellUnit(UnitAI unit, int sellPrice, Draggable draggable)
    {
        Debug.Log($"💰 Selling {unit.unitName} for {sellPrice} gold");

        // Stop dragging
        draggable.isDragging = false;
        Draggable.currentlyDragging = null; // ✅ Reset global reference

        // Add gold to player
        EconomyManager.Instance.AddGold(sellPrice);

        // ✅ CRITICAL: Clear tile assignment PROPERLY
        if (unit.currentTile != null)
        {
            unit.currentTile.Free(unit);
        }

        // Unregister from game manager
        GameManager.Instance.UnregisterUnit(unit);

        // Re-evaluate traits after unit removal
        TraitManager.Instance.EvaluateTraits(GameManager.Instance.playerUnits);
        TraitManager.Instance.ApplyTraits(GameManager.Instance.playerUnits);

        // Play sell sound
        if (sellSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(sellSound);
        }

        // Visual feedback
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.green;
            Invoke(nameof(ResetColor), 0.5f);
        }

        // Update sell text temporarily
        if (sellText != null)
        {
            sellText.text = $"+{sellPrice} Gold";
            Invoke(nameof(ResetSellText), 2f);
        }

        // Destroy the unit
        Destroy(unit.gameObject);
    }

    private void Update()
    {
        Draggable[] allDraggables = FindObjectsOfType<Draggable>();
        Draggable heldUnit = null;

        foreach (var draggable in allDraggables)
        {
            if (draggable.isDragging)
            {
                heldUnit = draggable;
                break;
            }
        }

        if (heldUnit != null)
        {
            UnitAI unit = heldUnit.GetComponent<UnitAI>();
            if (unit != null && unit.team == Team.Player)
            {
                int previewSellPrice = CalculateSellPrice(unit);

                // Highlight color + show preview
                if (backgroundImage != null)
                    backgroundImage.color = Color.Lerp(backgroundImage.color, highlightColor, Time.deltaTime * 5f);

                if (sellText != null)
                    sellText.text = $"Sell for {previewSellPrice}";
            }
        }
        else
        {
            // Reset to normal appearance
            if (backgroundImage != null)
                backgroundImage.color = Color.Lerp(backgroundImage.color, normalColor, Time.deltaTime * 5f);

            if (sellText != null)
                sellText.text = "Sell Zone";
        }
    }


    private void ResetSellText()
    {
        if (sellText != null)
            sellText.text = "Sell Zone";
    }

    private void ResetColor()
    {
        if (backgroundImage != null)
            backgroundImage.color = normalColor;
    }
}
