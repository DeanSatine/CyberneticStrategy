using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public List<UnitAI> playerUnits = new List<UnitAI>();
    private List<UnitAI> enemyUnits = new List<UnitAI>();

    [Header("Merge VFX")]
    public GameObject starUpVFXPrefab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- Register / Unregister ---
    public void RegisterUnit(UnitAI unit, bool isPlayer)
    {
        if (isPlayer)
        {
            if (!playerUnits.Contains(unit))
                playerUnits.Add(unit);
        }
        else
        {
            if (!enemyUnits.Contains(unit))
                enemyUnits.Add(unit);
        }

        // ✅ Update fight button visibility when units change
        if (isPlayer && UIManager.Instance != null)
        {
            UIManager.Instance.UpdateFightButtonVisibility();
        }
    }

    public void UnregisterUnit(UnitAI unit)
    {
        bool wasPlayerUnit = playerUnits.Contains(unit);

        playerUnits.Remove(unit);
        enemyUnits.Remove(unit);

        // ✅ Update fight button visibility when units change
        if (wasPlayerUnit && UIManager.Instance != null)
        {
            UIManager.Instance.UpdateFightButtonVisibility();
        }
    }

    // --- Try merging when a new unit is bought ---
    public void TryMergeUnits(UnitAI triggerUnit = null)
    {
        Debug.Log($"🔎 Checking for merges{(triggerUnit ? $" triggered by {triggerUnit.unitName}" : "")}");

        // Group units by name and star level
        var unitGroups = playerUnits
            .Where(u => u != null && u.isAlive)
            .GroupBy(u => new { u.unitName, u.starLevel })
            .Where(g => g.Count() >= 3)
            .ToList();

        foreach (var group in unitGroups)
        {
            var unitsToMerge = group.Take(3).ToList();
            Debug.Log($"🌟 Merging 3x {group.Key.unitName} (star {group.Key.starLevel})");

            // Keep the first unit and upgrade it
            UnitAI upgradedUnit = unitsToMerge[0];
            upgradedUnit.UpgradeStarLevel();

            // Spawn merge VFX if available
            if (starUpVFXPrefab != null)
            {
                Instantiate(starUpVFXPrefab, upgradedUnit.transform.position, Quaternion.identity);
            }

            // Remove and destroy the other two units
            for (int i = 1; i < unitsToMerge.Count; i++)
            {
                UnitAI unitToRemove = unitsToMerge[i];

                // Free the tile properly
                if (unitToRemove.currentTile != null)
                {
                    unitToRemove.currentTile.Free(unitToRemove);
                }

                // Unregister and destroy
                UnregisterUnit(unitToRemove);
                Destroy(unitToRemove.gameObject);
            }

            Debug.Log($"✅ {upgradedUnit.unitName} upgraded to {upgradedUnit.starLevel} stars!");
        }

        // Re-evaluate traits after merging
        if (unitGroups.Any())
        {
            TraitManager.Instance.EvaluateTraits(playerUnits);
            TraitManager.Instance.ApplyTraits(playerUnits);
        }
    }

    // --- Access ---
    public List<UnitAI> GetPlayerUnits() => playerUnits;
    public List<UnitAI> GetEnemyUnits() => enemyUnits;

    // --- Reset Player Units after combat ---
    public void ResetPlayerUnits()
    {
        foreach (var unit in playerUnits)
        {
            if (unit != null && unit.isAlive)
            {
                unit.ResetAfterCombat();        // stop attacking
                if (unit.startingTile != null)  // snap back to saved tile
                    unit.AssignToTile(unit.startingTile);
            }
        }

        // ✅ Reapply traits after reset
        TraitManager.Instance.EvaluateTraits(playerUnits);
        TraitManager.Instance.ApplyTraits(playerUnits);
    }
}
