using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UnitDetailsUI : MonoBehaviour
{
    public static UnitDetailsUI Instance;

    [Header("UI Elements")]
    public GameObject panelRoot;
    public Image portraitImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI adText;
    public TextMeshProUGUI asText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI armorText;
    public TextMeshProUGUI rangeText;
    public TextMeshProUGUI manaText;
    public Image abilityIcon;

    [Header("Ability Tooltip")]
    public GameObject abilityTooltipPrefab;
    private GameObject abilityTooltipInstance;

    private UnitAI currentUnit;

    private void Awake()
    {
        Instance = this;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (currentUnit != null)
        {
            // Live stat updates
            adText.text = currentUnit.attackDamage.ToString("F0");
            asText.text = currentUnit.attackSpeed.ToString("F2");
            hpText.text = $"{currentUnit.currentHealth}/{currentUnit.maxHealth}";
            armorText.text = currentUnit.armor.ToString("F0");
            rangeText.text = currentUnit.attackRange.ToString("F1");
            manaText.text = $"{currentUnit.currentMana}/{currentUnit.maxMana}";
        }
    }

    public void Show(UnitAI unit)
    {
        currentUnit = unit;
        if (unit == null) return;

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        // Example portrait/icon if you have one set per unit
        if (portraitImage != null && unit.portraitSprite != null)
            portraitImage.sprite = unit.portraitSprite;

        if (nameText != null)
            nameText.text = unit.unitName;

        // Init stats immediately
        adText.text = unit.attackDamage.ToString("F0");
        asText.text = unit.attackSpeed.ToString("F2");
        hpText.text = $"{unit.currentHealth}/{unit.maxHealth}";
        armorText.text = unit.armor.ToString("F0");
        rangeText.text = unit.attackRange.ToString("F1");
        manaText.text = $"{unit.currentMana}/{unit.maxMana}";

        // Hook ability tooltip events
        if (abilityIcon != null)
        {
            var trigger = abilityIcon.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = abilityIcon.gameObject.AddComponent<EventTrigger>();

            trigger.triggers.Clear();

            EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener((data) => ShowAbilityTooltip(unit));
            trigger.triggers.Add(enter);

            EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener((data) => HideAbilityTooltip());
            trigger.triggers.Add(exit);
        }
    }

    public void Hide()
    {
        currentUnit = null;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void ShowAbilityTooltip(UnitAI unit)
    {
        if (abilityTooltipPrefab != null && abilityTooltipInstance == null)
        {
            abilityTooltipInstance = Instantiate(abilityTooltipPrefab, panelRoot.transform);
            var tooltipText = abilityTooltipInstance.GetComponentInChildren<TextMeshProUGUI>();

            if (tooltipText != null)
                tooltipText.text = unit.GetAbilityDescription(); // Implement this in UnitAI or ability script
        }
    }

    private void HideAbilityTooltip()
    {
        if (abilityTooltipInstance != null)
        {
            Destroy(abilityTooltipInstance);
            abilityTooltipInstance = null;
        }
    }
}
