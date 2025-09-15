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
    public GameObject abilityTooltip;
    public TMP_Text abilityTooltipText;

    private UnitAI currentUnit;

    private void Awake()
    {
        Hide();
        if (abilityTooltip != null) abilityTooltip.SetActive(false);
    }

    public void Show(UnitAI unit)
    {
        currentUnit = unit;
        gameObject.SetActive(true);

        // ✅ Static values
        if (portraitImage) portraitImage.sprite = unit.portraitSprite;
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

    private void RefreshDynamic()
    {
        if (currentUnit == null) return;

        adText.text = $"AD: {currentUnit.attackDamage:F0}";
        asText.text = $"AS: {currentUnit.attackSpeed:F2}";
        hpText.text = $"HP: {currentUnit.currentHealth:F0}/{currentUnit.maxHealth:F0}";
        armorText.text = $"Armor: {currentUnit.armor:F0}";
        manaText.text = $"Mana: {currentUnit.currentMana:F0}/{currentUnit.maxMana:F0}";
    }

    // Hover handlers
    public void OnAbilityIconEnter()
    {
        if (abilityTooltip != null) abilityTooltip.SetActive(true);
    }

    public void OnAbilityIconExit()
    {
        if (abilityTooltip != null) abilityTooltip.SetActive(false);
    }
}
