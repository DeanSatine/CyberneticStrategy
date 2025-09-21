using UnityEngine;

public class BulkheadTrait : MonoBehaviour
{
    [HideInInspector] public float bonusHealthPercent;
    [HideInInspector] public float deathSharePercent;

    private UnitAI unitAI;
    private bool applied = false;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
    }

    private void OnEnable()
    {
        // ❌ REMOVED: Don't apply immediately, wait for TraitManager to set values
        // ApplyBonusHealth();
        UnitAI.OnAnyUnitDeath += OnUnitDeath;
    }

    // ✅ Public method for TraitManager to call after setting values
    public void ApplyBonusHealthPublic()
    {
        ApplyBonusHealth();
    }


    private void OnDisable()
    {
        UnitAI.OnAnyUnitDeath -= OnUnitDeath;
    }

    private void ApplyBonusHealth()
    {
        if (applied) return;

        // 🔍 DEBUG: Add logging to see what values we have
        Debug.Log($"🔍 [BULKHEAD DEBUG] {unitAI.unitName} - bonusHealthPercent: {bonusHealthPercent}, baseMaxHealth: {unitAI.baseMaxHealth}");

        // ✅ FIXED: Use the UnitAI's baseMaxHealth and bonusMaxHealth system
        float bonus = unitAI.baseMaxHealth * bonusHealthPercent;

        Debug.Log($"🔍 [BULKHEAD DEBUG] Calculated bonus: {bonus}");

        unitAI.bonusMaxHealth += bonus;

        // ✅ Recalculate max health using the proper system
        unitAI.RecalculateMaxHealth();

        // ✅ Heal by the same amount (allows overheal)
        unitAI.currentHealth += bonus;

        // ✅ Update UI to show the new values
        if (unitAI.ui != null)
            unitAI.ui.UpdateHealth(unitAI.currentHealth);

        applied = true;

        Debug.Log($"💪 {unitAI.unitName} Bulkhead activated! +{bonus:F1} max HP (now {unitAI.currentHealth:F1}/{unitAI.maxHealth:F1})");
    }
    public void RemoveBonusHealthPublic()
    {
        RemoveBonusHealth();
    }

    private void RemoveBonusHealth()
    {
        if (!applied) return;

        // ✅ Calculate the bonus that was previously applied
        float bonus = unitAI.baseMaxHealth * bonusHealthPercent;

        // ✅ Remove the bonus from max health
        unitAI.bonusMaxHealth -= bonus;

        // ✅ Recalculate max health
        unitAI.RecalculateMaxHealth();

        // ✅ Adjust current health if it exceeds new max health
        if (unitAI.currentHealth > unitAI.maxHealth)
        {
            unitAI.currentHealth = unitAI.maxHealth;
        }

        // ✅ Update UI
        if (unitAI.ui != null)
            unitAI.ui.UpdateHealth(unitAI.currentHealth);

        applied = false;

        Debug.Log($"💔 {unitAI.unitName} Bulkhead deactivated! -{bonus:F1} max HP (now {unitAI.currentHealth:F1}/{unitAI.maxHealth:F1})");
    }

    // ✅ Static method to reset all Bulkhead traits
    public static void ResetAllBulkheads()
    {
        BulkheadTrait[] allBulkheads = FindObjectsOfType<BulkheadTrait>();

        foreach (var bulkhead in allBulkheads)
        {
            if (bulkhead != null)
            {
                bulkhead.RemoveBonusHealth();
                Destroy(bulkhead); // Remove the component entirely
            }
        }

        Debug.Log("🔄 All Bulkhead traits reset");
    }


    private void OnUnitDeath(UnitAI deadUnit)
    {
        if (deadUnit == unitAI) // this Bulkhead died
        {
            UnitAI nearest = FindNearestAlly();
            if (nearest != null)
            {
                float healthShare = unitAI.maxHealth * deathSharePercent;
                nearest.currentHealth += healthShare; // ✅ allows overheal!

                // ✅ Update UI to show the health change
                if (nearest.ui != null)
                    nearest.ui.UpdateHealth(nearest.currentHealth);

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
