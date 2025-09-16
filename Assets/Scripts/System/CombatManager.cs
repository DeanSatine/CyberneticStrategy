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

    public void StartCombat()
    {
        Debug.Log("⚔️ Combat started!");

        // ✅ Reset lingering trait visuals at round start
        EradicatorTrait.ResetAllEradicators();

        // ✅ Take snapshots of all player units before combat
        TakePlayerUnitSnapshots();

        savedPlayerPositions.Clear();

        foreach (var unit in FindObjectsOfType<UnitAI>())
        {
            if (!unit.isAlive || unit.currentState == UnitAI.UnitState.Bench)
                continue;

            if (unit.team == Team.Player && unit.currentTile != null)
                savedPlayerPositions[unit] = unit.currentTile;

            unit.SetState(UnitAI.UnitState.Combat);
            Debug.Log($"✅ Set {unit.team} unit {unit.unitName} to Combat state");
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

        foreach (var snapshot in preCombatPlayerSnapshots)
        {
            RestoreUnitFromSnapshot(snapshot);
        }

        // ✅ Clear snapshots after restoration
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

    private void OnEnable()
    {
        UnitAI.OnAnyUnitDeath += HandleUnitDeath;
    }

    private void OnDisable()
    {
        UnitAI.OnAnyUnitDeath -= HandleUnitDeath;
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
