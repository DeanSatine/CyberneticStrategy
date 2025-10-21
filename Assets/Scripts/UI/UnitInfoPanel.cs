using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitInfoPanelUI : MonoBehaviour
{
    [Header("References")]
    public Image portraitImage;
    public TMP_Text adText;
    public TMP_Text asText;
    public TMP_Text hpText;
    public TMP_Text armorText;
    public TMP_Text manaText;

    [Header("Ability")]
    public Image abilityIcon;
    public GameObject abilityTooltip;       // tooltip panel
    public TMP_Text abilityTooltipText;     // description text

    private UnitAI currentUnit;
    public bool IsShowingUnit(UnitAI unit) => currentUnit == unit;

    private void Awake()
    {
        Hide();
        if (abilityTooltip != null)
            abilityTooltip.SetActive(false); // start hidden
    }

    public void Show(UnitAI unit)
    {
        currentUnit = unit;
        gameObject.SetActive(true);

        if (abilityTooltipText != null) abilityTooltipText.text = unit.GetAbilityDescription();

        RefreshDynamic();
    }

    public void Hide()
    {
        currentUnit = null;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (currentUnit != null)
            RefreshDynamic();
    }
    public void ForceRefresh()
    {
        if (currentUnit != null)
            RefreshDynamic(); // (RefreshDynamic() in your current script is private; make it internal/private/protected as needed)
    }

    private void RefreshDynamic()
    {
        if (currentUnit == null) return;

        adText.text = $"AD: {currentUnit.attackDamage:F0}";
        asText.text = $"AS: {currentUnit.attackSpeed:F2}";
        hpText.text = $"HP: {currentUnit.currentHealth:F0}/{currentUnit.maxHealth:F0}";
        armorText.text = $"Armor: {currentUnit.armor:F0}";
        manaText.text = $"Mana: {currentUnit.currentMana:F0}/{currentUnit.maxMana:F0}";
    }

    // === Tooltip Hover ===
    public void OnAbilityIconEnter()
    {
        if (abilityTooltip != null)
            abilityTooltip.SetActive(true);
    }

    public void OnAbilityIconExit()
    {
        if (abilityTooltip != null)
            abilityTooltip.SetActive(false);
    }
}
