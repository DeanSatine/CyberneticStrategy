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
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        if (GetComponent<AudioSource>() == null) gameObject.AddComponent<AudioSource>();
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

    // --- Process all possible merges in one pass ---
    private bool ProcessAllPossibleMerges()
    {
        bool anyMergesThisPass = false;

        // Check for merges at each star level (1->2, then 2->3)
        for (int starLevel = 1; starLevel <= 2; starLevel++)
        {
            // ✅ FIX: Less strict filtering - include all alive units regardless of state
            var unitGroups = playerUnits
                .Where(u => u != null && u.isAlive && u.currentState != UnitState.Combat) // Only exclude combat units
                .GroupBy(u => new { u.unitName, u.starLevel })
                .Where(g => g.Key.starLevel == starLevel && g.Count() >= 3)
                .ToList();

            foreach (var group in unitGroups)
            {
                var availableUnits = group.ToList();

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

                        // Free the tile properly
                        if (unitToRemove.currentTile != null)
                        {
                            unitToRemove.currentTile.Free(unitToRemove);
                        }

                        // Unregister and destroy
                        UnregisterUnit(unitToRemove);
                        availableUnits.Remove(unitToRemove);
                        Destroy(unitToRemove.gameObject);
                    }

                    availableUnits.Remove(upgradedUnit);
                    anyMergesThisPass = true;

                    Debug.Log($"✅ {upgradedUnit.unitName} upgraded to {upgradedUnit.starLevel} stars!");

                    if (upgradedUnit.starLevel == 3)
                    {
                        Debug.Log($"🏆 LEGENDARY! {upgradedUnit.unitName} reached maximum power (3★)!");
                    }
                }
            }
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
