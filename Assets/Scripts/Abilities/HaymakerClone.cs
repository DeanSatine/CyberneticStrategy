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

    // ✅ FIX: Direct reference to master instead of FindObjectOfType
    [HideInInspector] public HaymakerAbility masterHaymaker;

    private void Awake()
    {
        var unitAI = GetComponent<UnitAI>();
        if (unitAI != null)
        {
            originalUnitName = unitAI.unitName.Replace(" Clone", "");

            // ✅ Ensure clone has proper unit name for ability descriptions
            if (!unitAI.unitName.Contains("Clone"))
            {
                unitAI.unitName = "Haymaker Clone";
            }

            // ✅ FIX: Find the correct master Haymaker by proximity and team
            if (masterHaymaker == null)
            {
                HaymakerAbility[] allHaymakers = FindObjectsOfType<HaymakerAbility>();
                float closestDistance = float.MaxValue;

                foreach (var haymaker in allHaymakers)
                {
                    var haymakerUnitAI = haymaker.GetComponent<UnitAI>();
                    if (haymakerUnitAI != null && haymakerUnitAI.team == unitAI.team)
                    {
                        float distance = Vector3.Distance(transform.position, haymaker.transform.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            masterHaymaker = haymaker;
                        }
                    }
                }
            }

            Debug.Log($"[HaymakerClone] Clone initialized with master: {masterHaymaker?.unitAI.unitName}");
        }
    }

    // ✅ FIXED: Use masterHaymaker reference instead of FindObjectOfType
    public float GetSlamDamage()
    {
        var unitAI = GetComponent<UnitAI>();
        if (unitAI == null) return 0f;

        if (masterHaymaker != null)
        {
            int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, masterHaymaker.slamDamage.Length - 1);
            return masterHaymaker.slamDamage[starIndex];
        }

        return 0f;
    }

    // ✅ FIXED: Use masterHaymaker reference
    public string GetDetailedSoulStatus()
    {
        if (masterHaymaker != null)
        {
            int totalSouls = masterHaymaker.SoulCount;
            int empowerments = totalSouls / 5;
            int soulsToNext = 5 - (totalSouls % 5);

            if (soulsToNext == 5) soulsToNext = 0; // If exactly divisible by 5

            string progressBar = GetProgressBar(totalSouls % 5, 5);

            if (totalSouls == 0)
            {
                return $"No souls absorbed yet. Next empowerment in {soulsToNext} souls.";
            }
            else if (soulsToNext == 0)
            {
                return $"💀 {totalSouls} souls absorbed | {empowerments} empowerments | Ready for next! {progressBar}";
            }
            else
            {
                return $"💀 {totalSouls} souls absorbed | {empowerments} empowerments | Next in {soulsToNext} {progressBar}";
            }
        }
        return "❌ Master Haymaker not found";
    }

    // ✅ NEW: Visual progress bar for souls
    private string GetProgressBar(int current, int max)
    {
        string bar = "[";
        for (int i = 0; i < max; i++)
        {
            if (i < current)
                bar += "●"; // Filled
            else
                bar += "○"; // Empty
        }
        bar += "]";
        return bar;
    }

    // ✅ FIXED: Use masterHaymaker reference
    public float GetCurrentStatBonusPercent()
    {
        if (masterHaymaker != null)
        {
            int empowerments = masterHaymaker.SoulCount / 5;
            return empowerments * 1f; // 1% per empowerment, return as percentage
        }
        return 0f;
    }

    // ✅ FIXED: Use masterHaymaker reference
    public (float currentHP, float currentDamage, float baseHP, float baseDamage) GetStatsWithBonuses()
    {
        var unitAI = GetComponent<UnitAI>();
        if (unitAI == null) return (0, 0, 0, 0);

        if (masterHaymaker != null)
        {
            // Find base stats (25% of original Haymaker)
            var masterUnitAI = masterHaymaker.GetComponent<UnitAI>();
            if (masterUnitAI != null)
            {
                float baseHP = masterUnitAI.maxHealth * 0.25f;
                float baseDamage = masterUnitAI.attackDamage * 0.25f;

                // Calculate bonus from souls
                int empowerments = masterHaymaker.SoulCount / 5;
                float bonusPercent = empowerments * 1f / 100f; // Convert to decimal
                float currentHP = baseHP * (1f + bonusPercent);
                float currentDamage = baseDamage * (1f + bonusPercent);

                return (currentHP, currentDamage, baseHP, baseDamage);
            }
        }

        // Fallback to current values
        return (unitAI.maxHealth, unitAI.attackDamage, unitAI.maxHealth, unitAI.attackDamage);
    }

    // ✅ ENHANCED: Get formatted stats comparison with empowerment details
    public string GetStatsComparison()
    {
        var (currentHP, currentDamage, baseHP, baseDamage) = GetStatsWithBonuses();
        float bonusPercent = GetCurrentStatBonusPercent();

        if (bonusPercent > 0)
        {
            return $"⚡ Empowered: {currentHP:F0} HP, {currentDamage:F0} AD (+{bonusPercent:F0}% from souls)";
        }
        else
        {
            return $"📊 Base Stats: {baseHP:F0} HP, {baseDamage:F0} AD (25% of original)";
        }
    }

    // ✅ FIXED: Use masterHaymaker reference
    public string GetEmpowermentTier()
    {
        if (masterHaymaker != null)
        {
            int empowerments = masterHaymaker.SoulCount / 5;

            if (empowerments == 0) return "🔹 Tier: Base Clone";
            else if (empowerments <= 5) return $"🔸 Tier: Awakened Clone (Level {empowerments})";
            else if (empowerments <= 10) return $"🔶 Tier: Enhanced Clone (Level {empowerments})";
            else if (empowerments <= 20) return $"🔥 Tier: Empowered Clone (Level {empowerments})";
            else return $"💀 Tier: Soul Reaper (Level {empowerments})";
        }
        return "❓ Tier: Unknown";
    }

    // ✅ NEW: Method to reassign master (useful when master gets upgraded)
    public void ReassignMaster(HaymakerAbility newMaster)
    {
        if (newMaster != null)
        {
            masterHaymaker = newMaster;
            Debug.Log($"[HaymakerClone] Master reassigned to: {newMaster.unitAI.unitName}");
        }
    }

    // ✅ NEW: Validate master is still valid
    public bool IsMasterValid()
    {
        return masterHaymaker != null && masterHaymaker.gameObject != null;
    }
}
