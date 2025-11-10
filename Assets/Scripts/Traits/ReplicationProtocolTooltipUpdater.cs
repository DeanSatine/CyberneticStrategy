using TMPro;
using UnityEngine;

public class ReplicationProtocolTooltipUpdater : MonoBehaviour
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

        UnitAI haymaker = FindHaymakerUnit();

        if (haymaker != null)
        {
            var cloneComponent = FindHaymakerClone();

            if (cloneComponent != null)
            {
                string soulStatus = cloneComponent.GetDetailedSoulStatus();

                tooltipText.text = $"Summon a clone of Haymaker with 25% health and damage. The clone does not benefit from traits.\n\n" +
                                   $"When units on the board die, Haymaker absorbs their soul. The clone gains 1% health and damage for every 5 souls absorbed.\n\n" +
                                   $"Soul Status: {soulStatus}";
            }
            else
            {
                tooltipText.text = "Summon a clone of Haymaker with 25% health and damage. The clone does not benefit from traits.\n\n" +
                                   "When units on the board die, Haymaker absorbs their soul. The clone gains 1% health and damage for every 5 souls absorbed.";
            }
        }
        else
        {
            tooltipText.text = "Summon a clone of Haymaker with 25% health and damage. The clone does not benefit from traits.\n\n" +
                               "When units on the board die, Haymaker absorbs their soul. The clone gains 1% health and damage for every 5 souls absorbed.";
        }
    }

    private UnitAI FindHaymakerUnit()
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        foreach (var unit in allUnits)
        {
            if (unit.unitName == "Haymaker" && unit.team == Team.Player)
            {
                return unit;
            }
        }
        return null;
    }

    private HaymakerClone FindHaymakerClone()
    {
        HaymakerClone[] allClones = FindObjectsOfType<HaymakerClone>();
        foreach (var clone in allClones)
        {
            if (clone.GetComponent<UnitAI>() != null && clone.GetComponent<UnitAI>().team == Team.Player)
            {
                return clone;
            }
        }
        return null;
    }
}
