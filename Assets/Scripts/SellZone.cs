using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static UnitAI;

public class SellZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Sell Zone Settings")]
    [SerializeField] private float sellPercentage = 0.6f;
    [SerializeField] private Color normalColor = Color.red;
    [SerializeField] private Color highlightColor = Color.yellow;

    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMPro.TMP_Text sellText;

    [Header("🔊 Sell Zone Audio")]
    [Tooltip("Audio played when successfully selling a unit")]
    [SerializeField] private AudioClip sellSound;

    [Tooltip("Audio played when sell attempt fails")]
    [SerializeField] private AudioClip sellFailSound;

    [Tooltip("Audio played when hovering over sell zone with unit")]
    [SerializeField] private AudioClip sellHoverSound;

    [Tooltip("Volume for sell audio")]
    [Range(0f, 1f)]
    public float sellAudioVolume = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        SetupAudio();
        SetupUI();
        SetupButton();
    }

    // ✅ Setup audio system
    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound for UI
        audioSource.volume = sellAudioVolume;

        Debug.Log("🔊 Sell zone audio system initialized");
    }

    // ✅ Play sell audio
    private void PlaySellAudio(AudioClip clip, string actionName = "")
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, sellAudioVolume);
            Debug.Log($"🔊 Sell zone played {actionName} audio");
        }
        else if (actionName != "")
        {
            Debug.Log($"🔇 Sell zone missing {actionName} audio clip");
        }
    }

    private void SetupUI()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (backgroundImage != null)
            backgroundImage.color = normalColor;
    }

    private void SetupButton()
    {
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
            // ✅ Play fail sound when no unit is held
            PlaySellAudio(sellFailSound, "sell fail");
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
            PlaySellAudio(sellFailSound, "sell fail");
            Debug.Log("❌ Held object is not a sellable unit");
            return;
        }

        // Only allow selling player units
        if (unit.team != Team.Player)
        {
            PlaySellAudio(sellFailSound, "sell fail");
            Debug.Log("❌ Cannot sell enemy units");
            return;
        }

        // Only allow selling during prep phase (not during combat)
        if (StageManager.Instance != null &&
            StageManager.Instance.currentPhase == StageManager.GamePhase.Combat &&
            unit.currentState != UnitState.Bench)
        {
            PlaySellAudio(sellFailSound, "sell fail");
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
            PlaySellAudio(sellFailSound, "sell fail");
            Debug.Log("❌ This unit cannot be sold");
            return;
        }

        // Perform the sale
        SellUnit(unit, sellPrice, heldUnit);
    }

    private int CalculateSellPrice(UnitAI unit)
    {
        int baseCost = GetUnitOriginalCost(unit);
        if (baseCost <= 0)
        {
            baseCost = unit.starLevel;
        }

        int effectiveCost = baseCost;
        if (unit.starLevel == 2)
            effectiveCost = baseCost * 3;
        else if (unit.starLevel == 3)
            effectiveCost = baseCost * 9;

        int sellPrice = Mathf.RoundToInt(effectiveCost * sellPercentage);

        return Mathf.Max(1, sellPrice);
    }

    private int GetUnitOriginalCost(UnitAI unit)
    {
        ShopSlotUI[] allShopSlots = FindObjectsOfType<ShopSlotUI>();

        foreach (var slot in allShopSlots)
        {
            if (slot.unitPrefab != null &&
                slot.unitPrefab.name == unit.gameObject.name.Replace("(Clone)", ""))
            {
                return slot.cost;
            }
        }

        return GetFallbackCost(unit);
    }

    private int GetFallbackCost(UnitAI unit)
    {
        string unitName = unit.unitName.ToLower();

        if (unitName.Contains("haymaker")) return 5;
        if (unitName.Contains("killswitch")) return 4;
        if (unitName.Contains("manadrive")) return 3;
        if (unitName.Contains("needlebot")) return 1;
        if (unitName.Contains("b.o.p")) return 2;

        return unit.starLevel;
    }

    private void SellUnit(UnitAI unit, int sellPrice, Draggable draggable)
    {
        Debug.Log($"💰 Selling {unit.unitName} for {sellPrice} gold");

        // ✅ Play sell success audio
        PlaySellAudio(sellSound, "sell success");

        // Stop dragging
        draggable.isDragging = false;
        Draggable.currentlyDragging = null;

        // Add gold to player
        EconomyManager.Instance.AddGold(sellPrice);

        // Clear tile assignment properly
        if (unit.currentTile != null)
        {
            unit.currentTile.Free(unit);
        }

        // Unregister from game manager
        GameManager.Instance.UnregisterUnit(unit);

        // Re-evaluate traits after unit removal
        TraitManager.Instance.EvaluateTraits(GameManager.Instance.playerUnits);
        TraitManager.Instance.ApplyTraits(GameManager.Instance.playerUnits);

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

    // ✅ Hover detection for audio feedback
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Check if holding a sellable unit
        Draggable[] allDraggables = FindObjectsOfType<Draggable>();
        foreach (var draggable in allDraggables)
        {
            if (draggable.isDragging)
            {
                UnitAI unit = draggable.GetComponent<UnitAI>();
                if (unit != null && unit.team == Team.Player)
                {
                    // ✅ Play hover sound when entering with valid unit
                    PlaySellAudio(sellHoverSound, "sell hover");

                    // Visual highlight
                    if (backgroundImage != null)
                        backgroundImage.color = highlightColor;

                    // Show sell price preview
                    int sellPrice = CalculateSellPrice(unit);
                    if (sellText != null)
                        sellText.text = $"Sell for {sellPrice}g";
                }
                break;
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Reset visual state
        if (backgroundImage != null)
            backgroundImage.color = normalColor;

        ResetSellText();
    }

    private void ResetColor()
    {
        if (backgroundImage != null)
            backgroundImage.color = normalColor;
    }

    private void ResetSellText()
    {
        if (sellText != null)
            sellText.text = "Sell";
    }
}
