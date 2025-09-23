using UnityEngine;

public class BulkheadTrait : MonoBehaviour
{
    [HideInInspector] public float bonusHealthPercent;
    [HideInInspector] public float deathSharePercent;

    private UnitAI unitAI;
    private bool applied = false;
    private float appliedBonus = 0f; // Track the actual bonus applied

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
    }
    private void Update()
    {
        // ✅ CRITICAL: Deactivate trait if unit is benched
        if (unitAI != null && unitAI.currentState == UnitAI.UnitState.Bench)
        {
            if (applied)
            {
                Debug.Log($"🏟️ {unitAI.unitName} moved to bench - removing Bulkhead bonus");
                RemoveBonusHealth();
            }

            // ✅ Destroy the component when benched
            Destroy(this);
            return;
        }

        // ✅ NEW: Auto-apply if unit is on board but trait isn't applied yet
        if (unitAI != null && unitAI.currentState == UnitAI.UnitState.BoardIdle && !applied)
        {
            ApplyBonusHealth();
        }
    }
    private void OnEnable()
    {
        UnitAI.OnAnyUnitDeath += OnUnitDeath;
    }

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

        Debug.Log($"🔍 [BULKHEAD DEBUG] {unitAI.unitName} - bonusHealthPercent: {bonusHealthPercent}, baseMaxHealth: {unitAI.baseMaxHealth}");

        // ✅ FIX: Store the applied bonus amount
        appliedBonus = unitAI.baseMaxHealth * bonusHealthPercent;

        Debug.Log($"🔍 [BULKHEAD DEBUG] Calculated bonus: {appliedBonus}");

        // ✅ FIX: Add to both bonusMaxHealth and traitBonusMaxHealth
        unitAI.bonusMaxHealth += appliedBonus;
        unitAI.traitBonusMaxHealth += appliedBonus;

        unitAI.RecalculateMaxHealth();
        unitAI.currentHealth += appliedBonus;

        if (unitAI.ui != null)
            unitAI.ui.UpdateHealth(unitAI.currentHealth);

        applied = true;

        Debug.Log($"💪 {unitAI.unitName} Bulkhead activated! +{appliedBonus:F1} max HP (now {unitAI.currentHealth:F1}/{unitAI.maxHealth:F1})");
    }

    public void RemoveBonusHealthPublic()
    {
        RemoveBonusHealth();
    }

    private void RemoveBonusHealth()
    {
        if (!applied) return;

        Debug.Log($"🔍 [BULKHEAD REMOVE] Removing {appliedBonus:F1} bonus from {unitAI.unitName}");

        // ✅ FIX: Remove the exact bonus that was applied from both fields
        unitAI.bonusMaxHealth -= appliedBonus;
        unitAI.traitBonusMaxHealth -= appliedBonus;

        unitAI.RecalculateMaxHealth();

        // ✅ FIX: Adjust current health if it exceeds new max health
        if (unitAI.currentHealth > unitAI.maxHealth)
        {
            unitAI.currentHealth = unitAI.maxHealth;
        }

        if (unitAI.ui != null)
            unitAI.ui.UpdateHealth(unitAI.currentHealth);

        applied = false;
        appliedBonus = 0f;

        Debug.Log($"💔 {unitAI.unitName} Bulkhead deactivated! -{appliedBonus:F1} max HP (now {unitAI.currentHealth:F1}/{unitAI.maxHealth:F1})");
    }

    // ✅ ENHANCED: Static method to reset all Bulkhead traits with better logging
    public static void ResetAllBulkheads()
    {
        BulkheadTrait[] allBulkheads = FindObjectsOfType<BulkheadTrait>();

        Debug.Log($"🔍 [BULKHEAD RESET] Found {allBulkheads.Length} Bulkhead components to reset");

        foreach (var bulkhead in allBulkheads)
        {
            if (bulkhead != null && bulkhead.unitAI != null)
            {
                Debug.Log($"🔍 [BULKHEAD RESET] Resetting Bulkhead for {bulkhead.unitAI.unitName}");
                bulkhead.RemoveBonusHealth();
                Destroy(bulkhead); // Remove the component entirely
            }
        }

        Debug.Log("🔄 All Bulkhead traits reset");
    }

    private void OnUnitDeath(UnitAI deadUnit)
    {
        // ✅ ENHANCED: Only share health if this unit is on the board
        if (deadUnit == unitAI && unitAI.currentState == UnitAI.UnitState.BoardIdle)
        {
            UnitAI nearest = FindNearestAlly();
            if (nearest != null)
            {
                float healthShare = unitAI.maxHealth * deathSharePercent;
                nearest.currentHealth += healthShare;

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

            // ✅ CRITICAL: Only consider allies that are on the board
            if (ally.currentState != UnitAI.UnitState.BoardIdle) continue;

            float dist = Vector3.Distance(unitAI.transform.position, ally.transform.position);

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
