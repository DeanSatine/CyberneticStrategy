using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitInfoPanelUI : MonoBehaviour
{
    [Header("References")]
    public Image portraitImage;

    [Header("Offensive Stats")]
    public TMP_Text adText;
    public TMP_Text apText;
    public TMP_Text asText;

    [Header("Defensive Stats")]
    public TMP_Text hpText;
    public TMP_Text armorText;
    public TMP_Text magicResistText;
    public TMP_Text damageReductionText;

    [Header("Resource")]
    public TMP_Text manaText;

    [Header("Ability")]
    public Image abilityIcon;
    public GameObject abilityTooltip;
    public TMP_Text abilityTooltipText;

    private UnitAI currentUnit;
    public bool IsShowingUnit(UnitAI unit) => currentUnit == unit;

    private void Awake()
    {
        Hide();
        if (abilityTooltip != null)
            abilityTooltip.SetActive(false);
    }

    public void Show(UnitAI unit)
    {
        currentUnit = unit;
        gameObject.SetActive(true);

        if (abilityTooltipText != null)
            abilityTooltipText.text = unit.GetAbilityDescription();

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
            RefreshDynamic();
    }

    private void RefreshDynamic()
    {
        if (currentUnit == null) return;

        if (adText != null)
            adText.text = $"AD: {currentUnit.attackDamage:F0}";

        if (apText != null)
            apText.text = $"AP: {currentUnit.abilityPower:F0}";

        if (asText != null)
            asText.text = $"AS: {currentUnit.attackSpeed:F2}";

        if (hpText != null)
        {
            ShieldComponent shield = currentUnit.GetComponent<ShieldComponent>();
            float shieldAmount = (shield != null) ? shield.CurrentShield : 0f;

            if (shieldAmount > 0)
            {
                float effectiveHP = currentUnit.currentHealth + shieldAmount;
                hpText.text = $"HP: <color=#00BFFF>{effectiveHP:F0}</color>/{currentUnit.maxHealth:F0}";
            }
            else
            {
                hpText.text = $"HP: {currentUnit.currentHealth:F0}/{currentUnit.maxHealth:F0}";
            }
        }

        if (armorText != null)
            armorText.text = $"Armor: {currentUnit.armor:F0}";

        if (magicResistText != null)
            magicResistText.text = $"MR: {currentUnit.magicResist:F0}";

        if (damageReductionText != null)
            damageReductionText.text = $"Dmg Reduction: {currentUnit.damageReduction:F0}%";

        if (manaText != null)
            manaText.text = $"Mana: {currentUnit.currentMana:F0}/{currentUnit.maxMana:F0}";
    }


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
