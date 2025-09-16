using UnityEngine;

/// <summary>
/// Marker component to identify Haymaker clones and provide clone-specific functionality
/// </summary>
public class HaymakerClone : MonoBehaviour
{
    [Header("Clone Info")]
    public string originalUnitName;
    public bool canBeSold = false;

    [Header("Clone Stats Display")]
    public bool showInUI = true;

    private void Awake()
    {
        var unitAI = GetComponent<UnitAI>();
        if (unitAI != null)
        {
            originalUnitName = unitAI.unitName.Replace(" Clone", "");

            // ✅ Ensure clone has proper unit name for ability descriptions
            if (!unitAI.unitName.Contains("Clone"))
            {
                unitAI.unitName = $"{unitAI.unitName} Clone";
            }

            Debug.Log($"[HaymakerClone] Clone initialized with name: '{unitAI.unitName}'");
        }
    }

    // ✅ Get slam damage for display purposes
    public float GetSlamDamage()
    {
        var unitAI = GetComponent<UnitAI>();
        if (unitAI == null) return 0f;

        var masterAbility = FindObjectOfType<HaymakerAbility>();
        if (masterAbility != null)
        {
            int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, masterAbility.slamDamage.Length - 1);
            return masterAbility.slamDamage[starIndex];
        }

        return 0f;
    }

    // ✅ Enhanced empowerment status with detailed soul counter
    public string GetDetailedSoulStatus()
    {
        var masterAbility = FindObjectOfType<HaymakerAbility>();
        if (masterAbility != null)
        {
            int totalSouls = masterAbility.SoulCount;
            int empowerments = totalSouls / 5;
            int soulsToNext = 5 - (totalSouls % 5);

            if (soulsToNext == 5) soulsToNext = 0; // If exactly divisible by 5

            return $"Souls Absorbed: {totalSouls} | Empowerments: {empowerments} | Next in: {soulsToNext}";
        }
        return "Master Haymaker not found";
    }

    // ✅ Get current stat bonus percentage
    public float GetCurrentStatBonusPercent()
    {
        var masterAbility = FindObjectOfType<HaymakerAbility>();
        if (masterAbility != null)
        {
            int empowerments = masterAbility.SoulCount / 5;
            return empowerments * 1f; // 1% per empowerment, return as percentage
        }
        return 0f;
    }

    // ✅ Calculate actual HP and damage values including bonuses
    public (float currentHP, float currentDamage, float baseHP, float baseDamage) GetStatsWithBonuses()
    {
        var unitAI = GetComponent<UnitAI>();
        if (unitAI == null) return (0, 0, 0, 0);

        var masterAbility = FindObjectOfType<HaymakerAbility>();
        if (masterAbility != null)
        {
            // Find base stats (25% of original Haymaker)
            var masterUnitAI = masterAbility.GetComponent<UnitAI>();
            if (masterUnitAI != null)
            {
                float baseHP = masterUnitAI.maxHealth * 0.25f;
                float baseDamage = masterUnitAI.attackDamage * 0.25f;

                // Calculate bonus from souls
                float bonusPercent = GetCurrentStatBonusPercent() / 100f; // Convert to decimal
                float currentHP = baseHP * (1f + bonusPercent);
                float currentDamage = baseDamage * (1f + bonusPercent);

                return (currentHP, currentDamage, baseHP, baseDamage);
            }
        }

        // Fallback to current values
        return (unitAI.maxHealth, unitAI.attackDamage, unitAI.maxHealth, unitAI.attackDamage);
    }

    // ✅ Get formatted stats comparison
    public string GetStatsComparison()
    {
        var (currentHP, currentDamage, baseHP, baseDamage) = GetStatsWithBonuses();
        float bonusPercent = GetCurrentStatBonusPercent();

        if (bonusPercent > 0)
        {
            return $"Stats: {currentHP:F0} HP ({baseHP:F0} +{bonusPercent:F0}%), {currentDamage:F0} AD ({baseDamage:F0} +{bonusPercent:F0}%)";
        }
        else
        {
            return $"Stats: {baseHP:F0} HP, {baseDamage:F0} AD (25% of original)";
        }
    }
}
