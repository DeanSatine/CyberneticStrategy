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

    // ✅ NEW: TFT-style unit persistence system
    private List<UnitSnapshot> preCombatPlayerSnapshots = new List<UnitSnapshot>();
    private bool combatSnapshotTaken = false;

    // ✅ NEW: Current phase reference for death tracking
    private StageManager.GamePhase currentPhase => StageManager.Instance?.currentPhase ?? StageManager.GamePhase.Prep;

    public void StartCombat()
    {
        Debug.Log("⚔️ Starting Combat");

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

    // ✅ NEW: Track when player units die during combat
    private void TrackCombatDeath(UnitAI deadUnit)
    {
        if (deadUnit.team == Team.Player && currentPhase == StageManager.GamePhase.Combat)
        {
            if (!unitsDeadThisCombat.Contains(deadUnit))
            {
                unitsDeadThisCombat.Add(deadUnit);
                Debug.Log($"📋 Tracked combat death: {deadUnit.unitName}");
            }
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
        CheckForRoundEnd();
    }

    private void CheckForRoundEnd()
    {
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

        if (!anyEnemiesAlive)
        {
            StageManager.Instance.OnCombatEnd(true);
        }
        else if (!anyPlayersAlive)
        {
            StageManager.Instance.OnCombatEnd(false);
        }
    }
}
