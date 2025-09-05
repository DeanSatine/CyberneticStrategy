using UnityEngine;

public class BulkheadTrait : MonoBehaviour
{
    [HideInInspector] public float bonusHealthPercent;
    [HideInInspector] public float deathSharePercent;

    private UnitAI unitAI;
    private bool applied = false;
    private float baseMaxHealth;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        baseMaxHealth = unitAI.maxHealth;
    }

    private void OnEnable()
    {
        ApplyBonusHealth();
        UnitAI.OnAnyUnitDeath += OnUnitDeath;
    }

    private void OnDisable()
    {
        UnitAI.OnAnyUnitDeath -= OnUnitDeath;
    }

    private void ApplyBonusHealth()
    {
        if (applied) return;
        float bonus = baseMaxHealth * bonusHealthPercent;
        unitAI.maxHealth += bonus;
        unitAI.currentHealth += bonus;
        applied = true;
    }

    private void OnUnitDeath(UnitAI deadUnit)
    {
        if (deadUnit == unitAI) // this Bulkhead died
        {
            UnitAI nearest = FindNearestAlly();
            if (nearest != null)
            {
                float healthShare = unitAI.maxHealth * deathSharePercent;
                nearest.currentHealth += healthShare; // ✅ allow overheal
                Debug.Log($"{unitAI.unitName} shared {healthShare} HP with {nearest.unitName} (now {nearest.currentHealth}/{nearest.maxHealth}+)");
            }
        }
    }

    private UnitAI FindNearestAlly()
    {
        UnitAI[] all = FindObjectsOfType<UnitAI>();
        UnitAI nearest = null;
        float minDist = Mathf.Infinity;

        foreach (var ally in all)
        {
            if (ally == unitAI || !ally.isAlive) continue;
            if (ally.team != unitAI.team) continue;

            float dist = Vector3.Distance(unitAI.transform.position, ally.transform.position);

            // ✅ Prioritize Bulkheads, then fallback to anyone
            bool allyIsBulkhead = ally.traits.Contains(Trait.Bulkhead);
            bool nearestIsBulkhead = nearest != null && nearest.traits.Contains(Trait.Bulkhead);

            if ((allyIsBulkhead && !nearestIsBulkhead) || dist < minDist)
            {
                minDist = dist;
                nearest = ally;
            }
        }

        return nearest;
    }
}
