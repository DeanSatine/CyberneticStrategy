using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;
    [System.Serializable]
    public class UnitSnapshot
    {
        public string unitName;
        public int starLevel;
        public Vector3 position;
        public HexTile assignedTile;
        public Team team;
        public int teamID;

        // Core stats
        public float maxHealth;
        public float attackDamage;
        public float attackSpeed;
        public float armor;
        public float attackRange;
        public float maxMana;

        // Runtime state
        public float currentHealth;
        public float currentMana;
        public bool wasAlive;
        public UnitAI.UnitState originalState;

        // Traits
        public List<Trait> traits;

        // Prefab reference for restoration
        public GameObject originalPrefab;
        public UnitAI originalUnit; // Keep reference to update existing unit

        public UnitSnapshot(UnitAI unit)
        {
            unitName = unit.unitName;
            starLevel = unit.starLevel;
            position = unit.transform.position;
            assignedTile = unit.currentTile;
            team = unit.team;
            teamID = unit.teamID;

            maxHealth = unit.maxHealth;
            attackDamage = unit.attackDamage;
            attackSpeed = unit.attackSpeed;
            armor = unit.armor;
            attackRange = unit.attackRange;
            maxMana = unit.maxMana;

            currentHealth = unit.currentHealth;
            currentMana = unit.currentMana;
            wasAlive = unit.isAlive;
            originalState = unit.currentState;

            traits = new List<Trait>(unit.traits);
            originalUnit = unit;

            Debug.Log($"📸 Snapshot created for {unitName} at {position} (HP: {currentHealth}/{maxHealth})");
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    public Dictionary<UnitAI, HexTile> savedPlayerPositions = new Dictionary<UnitAI, HexTile>();
    private bool isCheckingForRoundEnd = false;
    private Coroutine roundEndCheckCoroutine = null;
    private float deathProcessingDelay = 1.0f;
    // ✅ NEW: TFT-style unit persistence system
    private List<UnitSnapshot> preCombatPlayerSnapshots = new List<UnitSnapshot>();
    private bool combatSnapshotTaken = false;

    // ✅ NEW: Current phase reference for death tracking
    private StageManager.GamePhase currentPhase => StageManager.Instance?.currentPhase ?? StageManager.GamePhase.Prep;

    public void StartCombat()
    {
        Debug.Log("⚔️ Starting Combat");

        // ✅ Reset round end checking state
        isCheckingForRoundEnd = false;
        if (roundEndCheckCoroutine != null)
        {
            StopCoroutine(roundEndCheckCoroutine);
            roundEndCheckCoroutine = null;
        }

        // ✅ Clear any previous death tracking
        unitsDeadThisCombat.Clear();

        // ✅ Take snapshots RIGHT before setting combat state
        TakePlayerUnitSnapshots();

        SavePlayerPositions();

        foreach (var unit in GameManager.Instance.GetPlayerUnits())
        {
            if (unit != null && unit.currentState != UnitAI.UnitState.Bench)
            {
                unit.SetState(UnitAI.UnitState.Combat);
            }
        }

        foreach (var enemy in EnemyWaveManager.Instance.GetActiveEnemies())
        {
            if (enemy != null && enemy.isAlive)
            {
                enemy.SetState(UnitAI.UnitState.Combat);
            }
        }

        StartCoroutine(MonitorCombat());
    }

    // ✅ Add this method
    private void SavePlayerPositions()
    {
        savedPlayerPositions.Clear();

        foreach (var unit in GameManager.Instance.GetPlayerUnits())
        {
            if (unit != null && unit.currentState != UnitAI.UnitState.Bench)
            {
                savedPlayerPositions[unit] = unit.currentTile;
                Debug.Log($"📍 Saved position for {unit.unitName}: {unit.currentTile?.name}");
            }
        }

        Debug.Log($"📍 Saved {savedPlayerPositions.Count} player unit positions");
    }

    // ✅ Add this method
    private System.Collections.IEnumerator MonitorCombat()
    {
        yield return new WaitForSeconds(0.5f); // Initial delay

        while (true)
        {
            // Subscribe to unit death events for round end checking
            UnitAI.OnAnyUnitDeath -= HandleUnitDeath;
            UnitAI.OnAnyUnitDeath += HandleUnitDeath;

            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
        }
    }



    // ✅ NEW: Take snapshots of all player units before combat
    private void TakePlayerUnitSnapshots()
    {
        preCombatPlayerSnapshots.Clear();
        combatSnapshotTaken = true;

        foreach (var unit in GameManager.Instance.GetPlayerUnits())
        {
            if (unit != null && unit.currentState != UnitAI.UnitState.Bench)
            {
                var snapshot = new UnitSnapshot(unit);
                preCombatPlayerSnapshots.Add(snapshot);
                Debug.Log($"📸 Snapshotted player unit: {unit.unitName} (HP: {unit.currentHealth}/{unit.maxHealth})");
            }
        }

        Debug.Log($"📸 Took {preCombatPlayerSnapshots.Count} player unit snapshots before combat");
    }

    // ✅ NEW: Restore all player units from snapshots (TFT-style)
    public void RestorePlayerUnitsFromSnapshots()
    {
        if (!combatSnapshotTaken || preCombatPlayerSnapshots.Count == 0)
        {
            Debug.LogWarning("⚠️ No player unit snapshots to restore from!");
            return;
        }

        Debug.Log("🔄 Restoring player units from pre-combat snapshots...");

        // ✅ First restore all snapshotted units
        foreach (var snapshot in preCombatPlayerSnapshots)
        {
            RestoreUnitFromSnapshot(snapshot);
        }

        // ✅ NEW: Handle units that died during combat but need restoration
        foreach (var deadUnit in unitsDeadThisCombat)
        {
            if (deadUnit != null)
            {
                // Find the snapshot for this dead unit
                var matchingSnapshot = preCombatPlayerSnapshots.FirstOrDefault(s => s.originalUnit == deadUnit);
                if (matchingSnapshot != null)
                {
                    Debug.Log($"🔄 Force-restoring late death: {deadUnit.unitName}");

                    // Force restore this unit even if it was hidden
                    deadUnit.gameObject.SetActive(true);
                    deadUnit.isAlive = true;

                    RestoreExistingUnit(deadUnit, matchingSnapshot);
                }
            }
        }

        // ✅ Clear tracking lists
        unitsDeadThisCombat.Clear();
        preCombatPlayerSnapshots.Clear();
        combatSnapshotTaken = false;

        Debug.Log("✅ All player units restored from snapshots!");
    }


    // ✅ NEW: Restore individual unit from snapshot
    private void RestoreUnitFromSnapshot(UnitSnapshot snapshot)
    {
        UnitAI existingUnit = snapshot.originalUnit;

        // Check if the original unit still exists and is valid
        if (existingUnit != null)
        {
            // ✅ Restore existing unit (preferred method)
            RestoreExistingUnit(existingUnit, snapshot);
        }
        else
        {
            // ✅ Recreate unit if original was destroyed
            RecreateUnitFromSnapshot(snapshot);
        }
    }

    // ✅ NEW: Restore existing unit from snapshot data
    private void RestoreExistingUnit(UnitAI unit, UnitSnapshot snapshot)
    {
        Debug.Log($"🔄 Restoring existing unit: {snapshot.unitName}");

        // Restore core properties
        unit.isAlive = snapshot.wasAlive;
        unit.currentHealth = snapshot.currentHealth;
        unit.maxHealth = snapshot.maxHealth;
        unit.attackDamage = snapshot.attackDamage;
        unit.attackSpeed = snapshot.attackSpeed;
        unit.armor = snapshot.armor;
        unit.currentMana = snapshot.currentMana;
        unit.starLevel = snapshot.starLevel;

        // Restore position and tile
        unit.transform.position = snapshot.position;
        if (snapshot.assignedTile != null)
        {
            unit.AssignToTile(snapshot.assignedTile);
        }

        // Restore state
        unit.SetState(snapshot.originalState);

        // Reset combat flags
        unit.currentTarget = null;
        unit.isCastingAbility = false;

        // ✅ Ensure unit is properly re-registered
        if (!GameManager.Instance.GetPlayerUnits().Contains(unit))
        {
            GameManager.Instance.RegisterUnit(unit, unit.team == Team.Player);
        }

        // Restore visual state
        RestoreUnitVisuals(unit);

        Debug.Log($"✅ Restored existing unit: {unit.unitName} (HP: {unit.currentHealth}/{unit.maxHealth})");
    }

    // ✅ NEW: Recreate unit from snapshot if original was destroyed
    private void RecreateUnitFromSnapshot(UnitSnapshot snapshot)
    {
        Debug.LogWarning($"⚠️ Original unit destroyed, cannot recreate {snapshot.unitName} without prefab reference");
        // Note: To fully implement this, you'd need to store prefab references in snapshots
        // For now, this serves as a fallback that logs the issue
    }

    // ✅ NEW: Restore unit visual state
    private void RestoreUnitVisuals(UnitAI unit)
    {
        // Reset material transparency
        Renderer[] renderers = unit.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.materials)
            {
                if (material.HasProperty("_Color"))
                {
                    Color color = material.color;
                    color.a = 1f; // Full opacity
                    material.color = color;
                }
            }
        }

        // Reset animations
        if (unit.animator)
        {
            unit.animator.SetBool("isDead", false);
            unit.animator.Rebind();
            unit.animator.Update(0f);
        }

        // Update UI
        if (unit.ui != null)
        {
            unit.ui.UpdateHealth(unit.currentHealth);
            unit.ui.gameObject.SetActive(true);
        }
    }
    // ✅ NEW: Track units that died during this combat round
    private List<UnitAI> unitsDeadThisCombat = new List<UnitAI>();

    // ✅ NEW: Subscribe to death events to track deaths during combat
    private void OnEnable()
    {
        UnitAI.OnAnyUnitDeath += TrackCombatDeath;
    }

    private void OnDisable()
    {
        UnitAI.OnAnyUnitDeath -= TrackCombatDeath;
    }

    // ✅ ENHANCED: Track when player units die during combat
    private void TrackCombatDeath(UnitAI deadUnit)
    {
        Debug.Log($"📋 TrackCombatDeath called for {deadUnit.unitName} (Team: {deadUnit.team})");

        if (deadUnit.team == Team.Player && currentPhase == StageManager.GamePhase.Combat)
        {
            if (!unitsDeadThisCombat.Contains(deadUnit))
            {
                unitsDeadThisCombat.Add(deadUnit);
                Debug.Log($"📋 ✅ Tracked combat death: {deadUnit.unitName} (Total tracked: {unitsDeadThisCombat.Count})");
            }
            else
            {
                Debug.Log($"📋 ⚠️ {deadUnit.unitName} already tracked");
            }
        }
        else
        {
            Debug.Log($"📋 ⚠️ Not tracking {deadUnit.unitName} - Team: {deadUnit.team}, Phase: {currentPhase}");
        }
    }

    public void ClearProjectiles()
    {
        GameObject[] projectiles = GameObject.FindGameObjectsWithTag("Projectile");

        foreach (var proj in projectiles)
        {
            Destroy(proj);
        }

        Debug.Log($"🧹 Cleared {projectiles.Length} projectiles at round reset");
    }

    public Dictionary<UnitAI, HexTile> GetSavedPlayerPositions()
    {
        return savedPlayerPositions;
    }

    private void HandleUnitDeath(UnitAI deadUnit)
    {
        Debug.Log($"💀 HandleUnitDeath called for {deadUnit.unitName}");

        // ✅ Start delayed round end check (or restart if already checking)
        StartDelayedRoundEndCheck();
    }
    private void StartDelayedRoundEndCheck()
    {
        // ✅ Cancel any existing round end check
        if (roundEndCheckCoroutine != null)
        {
            StopCoroutine(roundEndCheckCoroutine);
            Debug.Log("🔄 Restarting round end check timer due to new death");
        }

        // ✅ Start new delayed check
        roundEndCheckCoroutine = StartCoroutine(DelayedRoundEndCheck());
    }

    // ✅ NEW: Wait for all death events before checking round end
    private IEnumerator DelayedRoundEndCheck()
    {
        isCheckingForRoundEnd = true;
        Debug.Log($"⏳ Starting delayed round end check - waiting {deathProcessingDelay} seconds for all deaths...");

        // ✅ Wait for death processing delay
        yield return new WaitForSeconds(deathProcessingDelay);

        // ✅ Final check for round end
        Debug.Log("🔍 Death processing delay complete - performing final round end check");
        CheckForRoundEnd();

        isCheckingForRoundEnd = false;
        roundEndCheckCoroutine = null;
    }
    private void CheckForRoundEnd()
    {
        // ✅ Only check during combat phase
        if (StageManager.Instance?.currentPhase != StageManager.GamePhase.Combat)
        {
            Debug.Log("🚫 Skipping round end check - not in combat phase");
            return;
        }

        List<UnitAI> allUnits = FindObjectsOfType<UnitAI>().ToList();

        // Only count units that are alive AND not benched
        bool anyPlayersAlive = allUnits.Any(u =>
            u.team == Team.Player &&
            u.isAlive &&
            u.currentState != UnitAI.UnitState.Bench);

        bool anyEnemiesAlive = allUnits.Any(u =>
            u.team == Team.Enemy &&
            u.isAlive &&
            u.currentState != UnitAI.UnitState.Bench);

        Debug.Log($"🔍 Round End Check: Players Alive: {anyPlayersAlive}, Enemies Alive: {anyEnemiesAlive}");
        Debug.Log($"📋 Deaths tracked this combat: {unitsDeadThisCombat.Count}");

        // ✅ Log all tracked deaths
        foreach (var deadUnit in unitsDeadThisCombat)
        {
            if (deadUnit != null)
            {
                Debug.Log($"💀 Tracked death: {deadUnit.unitName}");
            }
        }

        if (!anyEnemiesAlive)
        {
            Debug.Log("🏆 Player Victory - All enemies defeated!");
            StageManager.Instance.OnCombatEnd(true);
        }
        else if (!anyPlayersAlive)
        {
            Debug.Log("💔 Player Defeat - All players defeated!");
            StageManager.Instance.OnCombatEnd(false);
        }
        else
        {
            Debug.Log("⚔️ Combat continues - both sides have units alive");
        }
    }

}
