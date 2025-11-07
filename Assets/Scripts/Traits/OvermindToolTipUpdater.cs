using TMPro;
using UnityEngine;

public class OvermindTooltipUpdater : MonoBehaviour
{
    private TextMeshProUGUI tooltipText;

    private void Awake()
    {
        tooltipText = GetComponentInChildren<TextMeshProUGUI>();
        UpdateTooltipText();
    }

    private void OnEnable()
    {
        UpdateTooltipText();
    }

    private void UpdateTooltipText()
    {
        if (tooltipText == null)
            return;

        UnitAI coreweaver = FindCoreweaverUnit();
        if (coreweaver != null)
        {
            var ability = coreweaver.GetComponent<CoreweaverAbility>();
            if (ability != null)
            {
                int starIndex = Mathf.Clamp(coreweaver.starLevel - 1, 0, 2);
                float baseMana = ability.baseManaPerSecond.Length > starIndex ? ability.baseManaPerSecond[starIndex] : 5f;
                float manaPerSec = baseMana + (coreweaver.attackSpeed * ability.attackSpeedToManaConversion);

                tooltipText.text = $"Coreweaver cannot move or auto attack. Generates <color=#00BFFF>{manaPerSec:F1}</color> mana per second.";
            }
            else
            {
                tooltipText.text = "Coreweaver cannot move or auto attack. Generates mana per second.";
            }
        }
        else
        {
            tooltipText.text = "Coreweaver cannot move or auto attack. Generates mana per second.";
        }
    }

    private UnitAI FindCoreweaverUnit()
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        foreach (var unit in allUnits)
        {
            if (unit.unitName == "Coreweaver" && unit.team == Team.Player)
            {
                return unit;
            }
        }
        return null;
    }
}
