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
    }

    public void UnregisterUnit(UnitAI unit)
    {
        playerUnits.Remove(unit);
        enemyUnits.Remove(unit);
    }

    // --- Try merging when a new unit is bought ---
    public void TryMergeUnits(UnitAI newUnit)
    {
        Debug.Log($"🔎 Checking for merge on {newUnit.unitName} (star {newUnit.starLevel})");

        foreach (var u in playerUnits)
        {
            Debug.Log($"   -> {u.unitName}, star {u.starLevel}");
        }

        var sameUnits = playerUnits
            .Where(u => u.unitName == newUnit.unitName && u.starLevel == newUnit.starLevel)
            .ToList();

        if (sameUnits.Count >= 3)
        {
            UnitAI upgradedUnit = sameUnits[0];
            upgradedUnit.UpgradeStarLevel();

            // Remove the other two
            for (int i = 1; i < 3; i++)
            {
                UnitAI unitToRemove = sameUnits[i];
                playerUnits.Remove(unitToRemove);
                Destroy(unitToRemove.gameObject);
            }
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
