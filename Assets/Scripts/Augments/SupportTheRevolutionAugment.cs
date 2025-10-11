// Updated /Assets/Scripts/Augments/SupportTheRevolutionAugment.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class SupportTheRevolutionAugment : BaseAugment
{
    [Header("Gear Settings")]
    public int gearsPerUnit = 3;
    public float baseHealAmount = 50f;
    public float healPerStage = 50f;
    public float gearRefreshTime = 10f;

    [Header("Visual Settings")]
    public GameObject gearPrefab;
    public GameObject healVFXPrefab;
    public float gearOrbitRadius = 1.5f;
    public float gearOrbitSpeed = 90f; // degrees per second

    private Dictionary<UnitAI, GearSystem> unitGearSystems = new Dictionary<UnitAI, GearSystem>();

    public SupportTheRevolutionAugment()
    {
        augmentName = "Support the Revolution";
        description = "Your units start combat with 3 gears which fly to and heal the lowest health ally on attack. Gears heal 50-300 health (based on stage) and refresh every 10 seconds.";
        type = AugmentType.Generic;
        augmentColor = Color.green;
    }

    public override void ApplyAugment()
    {
        Debug.Log($"⚙️ Applying {augmentName}");

        // Get configured gear prefab from AugmentManager
        LoadConfiguredSettings();

        // Apply to all existing player units
        ApplyToAllPlayerUnits();
    }

    private void LoadConfiguredSettings()
    {
        // Find the configuration for this augment
        AugmentConfiguration[] configs = Object.FindObjectsOfType<AugmentConfiguration>();
        AugmentConfiguration supportConfig = System.Array.Find(configs,
            c => c != null && c.AugmentId == "SupportRevolution");

        if (supportConfig != null)
        {
            // Load configured values
            if (supportConfig.GetGearPrefab() != null)
            {
                gearPrefab = supportConfig.GetGearPrefab();
                Debug.Log($"⚙️ Using configured gear prefab: {gearPrefab.name}");
            }

            gearsPerUnit = supportConfig.GetGearsPerUnit();
            gearOrbitRadius = supportConfig.GetGearOrbitRadius();
            gearOrbitSpeed = supportConfig.GetGearOrbitSpeed();
            baseHealAmount = supportConfig.GetBaseHealAmount();

            Debug.Log($"⚙️ Loaded configuration: {gearsPerUnit} gears, {gearOrbitRadius} radius, {baseHealAmount} heal");
        }
        else
        {
            Debug.LogWarning("⚠️ No Support Revolution configuration found, using defaults");
        }
    }

    private void ApplyToAllPlayerUnits()
    {
        UnitAI[] playerUnits = Object.FindObjectsOfType<UnitAI>().Where(u => u.team == Team.Player).ToArray();

        foreach (var unit in playerUnits)
        {
            AddGearSystemToUnit(unit);
        }
    }

    private void AddGearSystemToUnit(UnitAI unit)
    {
        if (unitGearSystems.ContainsKey(unit)) return;

        GearSystem gearSystem = unit.gameObject.AddComponent<GearSystem>();

        // Pass the configured gear prefab to the GearSystem
        gearSystem.SetGearPrefab(gearPrefab);
        gearSystem.Initialize(this, gearsPerUnit, gearOrbitRadius, gearOrbitSpeed);

        unitGearSystems[unit] = gearSystem;

        Debug.Log($"⚙️ Added gear system to {unit.unitName} with prefab: {(gearPrefab != null ? gearPrefab.name : "default")}");
    }

    public float GetCurrentHealAmount()
    {
        int currentStage = StageManager.Instance != null ? StageManager.Instance.currentStage : 1;
        return baseHealAmount + (healPerStage * (currentStage - 1));
    }

    public UnitAI FindLowestHealthAlly(UnitAI attacker)
    {
        UnitAI[] playerUnits = Object.FindObjectsOfType<UnitAI>().Where(u =>
            u.team == Team.Player && u.isAlive && u != attacker).ToArray();

        if (playerUnits.Length == 0) return null;

        UnitAI lowestHealthUnit = playerUnits[0];
        float lowestHealthPercent = (float)lowestHealthUnit.currentHealth / lowestHealthUnit.maxHealth;

        foreach (var unit in playerUnits)
        {
            float healthPercent = (float)unit.currentHealth / unit.maxHealth;
            if (healthPercent < lowestHealthPercent)
            {
                lowestHealthPercent = healthPercent;
                lowestHealthUnit = unit;
            }
        }

        return lowestHealthUnit;
    }

    public void OnGearHeal(UnitAI target, float healAmount)
    {
        if (target == null || !target.isAlive) return;

        target.currentHealth = Mathf.Min(target.maxHealth, target.currentHealth + (int)healAmount);

        // Update UI
        if (target.ui != null)
        {
            target.ui.UpdateHealth(target.currentHealth);
        }

        // Play heal VFX
        if (healVFXPrefab != null)
        {
            GameObject vfx = Object.Instantiate(healVFXPrefab, target.transform.position, Quaternion.identity);
            Object.Destroy(vfx, 2f);
        }

        Debug.Log($"⚙️ Gear healed {target.unitName} for {healAmount} health!");
    }

    public override void OnCombatStart()
    {
        // Refresh all gears for combat
        foreach (var gearSystem in unitGearSystems.Values)
        {
            if (gearSystem != null)
            {
                gearSystem.RefreshGears();
            }
        }
    }

    public override void OnCombatEnd()
    {
        // Reset gear systems for next round
        foreach (var gearSystem in unitGearSystems.Values)
        {
            if (gearSystem != null)
            {
                gearSystem.ResetForNextRound();
            }
        }
    }

    public override void OnUnitSpawned(UnitAI unit)
    {
        if (unit.team == Team.Player)
        {
            AddGearSystemToUnit(unit);
        }
    }

    public override void RemoveAugment()
    {
        // Clean up all gear systems
        foreach (var gearSystem in unitGearSystems.Values)
        {
            if (gearSystem != null)
            {
                Object.Destroy(gearSystem);
            }
        }
        unitGearSystems.Clear();
    }
}
