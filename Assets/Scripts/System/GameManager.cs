using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnitAI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public List<UnitAI> playerUnits = new List<UnitAI>();
    private List<UnitAI> enemyUnits = new List<UnitAI>();

    [Header("Merge VFX")]
    public GameObject starUpVFXPrefab;
    [Header("🔊 Star Up Audio")]
    public AudioClip starUpSound;
    [Header("🔊 Purchase Audio")]
    public AudioClip purchaseSound;
    // Update GameManager.cs Awake method
    private void Awake()
    {
        // ✅ FIXED: Proper singleton management for scene transitions
        if (Instance == null)
        {
            Instance = this;

            // ✅ NEW: Reset static events when GameManager is created
            UnitAI.ResetStaticEvents();

            Debug.Log("✅ GameManager initialized with clean state");
        }
        else if (Instance != this)
        {
            Debug.Log("🗑️ Destroying duplicate GameManager");
            Destroy(gameObject);
            return;
        }

        // ✅ NEW: Clear any stale unit lists
        playerUnits.Clear();

        if (GetComponent<AudioSource>() == null)
            gameObject.AddComponent<AudioSource>();
    }

    // ✅ NEW: Add method to completely reset GameManager state
    public void ResetGameState()
    {
        Debug.Log("🔄 Resetting GameManager state for new scene");

        // Clear all unit lists
        playerUnits.Clear();

        // Reset static events
        UnitAI.ResetStaticEvents();

        Debug.Log("✅ GameManager state reset complete");
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
        if (isPlayer && AugmentManager.Instance != null)
        {
            AugmentManager.Instance.OnUnitSpawned(unit);
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

    public void PlayPurchaseSound()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (purchaseSound != null) audioSource.PlayOneShot(purchaseSound);
    }

    // --- Try merging when a new unit is bought ---
    public void TryMergeUnits(UnitAI triggerUnit = null)
    {
        Debug.Log($"🔎 Checking for merges{(triggerUnit ? $" triggered by {triggerUnit.unitName}" : "")}");

        bool anyMergesOccurred = false;
        bool mergesThisPass;

        // Keep checking for merges until no more are possible
        do
        {
            mergesThisPass = ProcessAllPossibleMerges();
            if (mergesThisPass)
            {
                anyMergesOccurred = true;
                Debug.Log("🔄 Checking for additional merges...");
            }
        }
        while (mergesThisPass);

        // Re-evaluate traits after all merging is complete
        if (anyMergesOccurred)
        {
            TraitManager.Instance.EvaluateTraits(playerUnits);
            TraitManager.Instance.ApplyTraits(playerUnits);

            Debug.Log("✅ All possible merges completed! Traits re-evaluated.");
        }
    }

    private bool ProcessAllPossibleMerges()
    {
        bool anyMergesThisPass = false;

        // Check for merges at each star level (1->2, then 2->3)
        for (int starLevel = 1; starLevel <= 2; starLevel++)
        {
            // ✅ FIX: Include both registered player units AND bench units
            List<UnitAI> allPlayerUnits = new List<UnitAI>(playerUnits);

            // Add bench units that aren't already in playerUnits
            HexTile[] allTiles = FindObjectsOfType<HexTile>();
            foreach (var tile in allTiles)
            {
                if (tile.tileType == TileType.Bench && tile.occupyingUnit != null)
                {
                    var benchUnit = tile.occupyingUnit;
                    if (benchUnit.team == Team.Player && !allPlayerUnits.Contains(benchUnit))
                    {
                        allPlayerUnits.Add(benchUnit);
                    }
                }
            }

            // ✅ ENHANCED: Better debugging for merge detection
            var eligibleUnits = allPlayerUnits
                .Where(u => u != null && u.isAlive && u.currentState != UnitState.Combat)
                .ToList();

            Debug.Log($"🔍 Checking {starLevel}★ merges. Total player units: {allPlayerUnits.Count}, Eligible: {eligibleUnits.Count}");

            // Debug: Show what units are being considered
            foreach (var unit in eligibleUnits)
            {
                string location = unit.currentTile ? $"{unit.currentTile.tileType} at {unit.currentTile.gridPosition}" : "no tile";
                Debug.Log($"  📋 {unit.unitName} ({unit.starLevel}★) - {unit.currentState} on {location}");
            }

            var unitGroups = eligibleUnits
                .GroupBy(u => new { u.unitName, u.starLevel })
                .Where(g => g.Key.starLevel == starLevel && g.Count() >= 3)
                .ToList();

            Debug.Log($"🔍 Found {unitGroups.Count} groups of {starLevel}★ units with 3+ copies");

            foreach (var group in unitGroups)
            {
                var availableUnits = group.ToList();

                Debug.Log($"🔍 Group: {group.Key.unitName} ({starLevel}★) - {availableUnits.Count} units available");

                // List the units and their states/positions for debugging
                foreach (var unit in availableUnits)
                {
                    string location = unit.currentTile ? $"{unit.currentTile.tileType} at {unit.currentTile.gridPosition}" : "no tile";
                    Debug.Log($"  - {unit.unitName} on {location}, state: {unit.currentState}");
                }

                while (availableUnits.Count >= 3)
                {
                    var unitsToMerge = availableUnits.Take(3).ToList();

                    // ✅ FIX: Don't skip merging based on tile occupancy
                    int newStarLevel = starLevel + 1;
                    string mergeType = newStarLevel == 2 ? "⭐⭐" : "⭐⭐⭐";

                    Debug.Log($"🌟 Merging 3x {group.Key.unitName} ({starLevel}★) → 1x {group.Key.unitName} ({newStarLevel}★) {mergeType}");

                    UnitAI upgradedUnit = unitsToMerge[0];
                    upgradedUnit.UpgradeStarLevel();

                    // Spawn merge VFX if available
                    if (starUpVFXPrefab != null)
                    {
                        var vfx = Instantiate(starUpVFXPrefab, upgradedUnit.transform.position, Quaternion.identity);
                        Destroy(vfx, 2f);
                    }
                    if (starUpSound != null) GetComponent<AudioSource>().PlayOneShot(starUpSound);

                    // Remove and destroy the other two units
                    for (int i = 1; i < unitsToMerge.Count; i++)
                    {
                        UnitAI unitToRemove = unitsToMerge[i];

                        Debug.Log($"🗑️ Removing {unitToRemove.unitName} from {(unitToRemove.currentTile ? unitToRemove.currentTile.tileType.ToString() : "no tile")}");

                        // ✅ ENHANCED: Proper cleanup before destroying
                        if (unitToRemove.currentTile != null)
                        {
                            unitToRemove.currentTile.Free(unitToRemove);
                        }

                        // Unregister from GameManager
                        UnregisterUnit(unitToRemove);

                        // Remove from available units list for this merge cycle
                        availableUnits.Remove(unitToRemove);

                        // Destroy the GameObject
                        Destroy(unitToRemove.gameObject);
                    }

                    anyMergesThisPass = true;
                    Debug.Log($"✅ Merge completed! {upgradedUnit.unitName} is now {newStarLevel}★ on {(upgradedUnit.currentTile ? upgradedUnit.currentTile.tileType.ToString() : "no tile")}");
                }
            }
        }

        if (anyMergesThisPass)
        {
            Debug.Log("🎉 Merge pass completed with changes!");
        }
        else
        {
            Debug.Log("🔍 No merges found this pass.");
        }

        return anyMergesThisPass;
    }



    // --- Helper method to get merge progress for UI ---
    public string GetMergeProgress(string unitName)
    {
        var unitsOfType = playerUnits
            .Where(u => u != null && u.isAlive && u.unitName == unitName)
            .GroupBy(u => u.starLevel)
            .ToDictionary(g => g.Key, g => g.Count());

        string progress = "";

        // Show 1-star progress (towards 2-star)
        if (unitsOfType.ContainsKey(1))
        {
            int count = unitsOfType[1];
            progress += $"1★: {count}/3";
            if (count >= 3) progress += " ✅";
        }

        // Show 2-star progress (towards 3-star)
        if (unitsOfType.ContainsKey(2))
        {
            int count = unitsOfType[2];
            if (progress != "") progress += " | ";
            progress += $"2★: {count}/3";
            if (count >= 3) progress += " ✅";
        }

        // Show 3-star units (maxed)
        if (unitsOfType.ContainsKey(3))
        {
            int count = unitsOfType[3];
            if (progress != "") progress += " | ";
            progress += $"3★: {count} 🏆";
        }

        return progress;
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
