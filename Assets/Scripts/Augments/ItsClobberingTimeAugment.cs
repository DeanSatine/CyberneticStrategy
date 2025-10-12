using UnityEngine;
using System.Linq;
using System.Collections;

[System.Serializable]
public class ItsClobberingTimeAugment : BaseAugment
{
    [Header("Augment Stats")]
    public float bonusAttackDamage = 20f;
    public float lowHealthThreshold = 0.1f; // 10%

    public ItsClobberingTimeAugment()
    {
        augmentName = "Its Clobbering Time!";
        description = "Clobbertrons jump to their target on combat start and jump once more at 10% health, and gain 20 Attack Damage and no armour instead. Gain a B.O.P.";
        type = AugmentType.Class;
        augmentColor = Color.blue;
    }

    public override void ApplyAugment()
    {
        Debug.Log($"🔨 Applying {augmentName}");

        // Spawn B.O.P unit
        SpawnBOP();

        // Apply changes to all existing Clobbertrons
        ApplyToClobbertrons();
    }

    private void SpawnBOP()
    {
        GameObject bopPrefab = null;

        // Try to get from AugmentConfiguration first
        AugmentConfiguration[] configs = Object.FindObjectsOfType<AugmentConfiguration>();
        AugmentConfiguration config = System.Array.Find(configs,
            c => c != null && c.AugmentId == "ClobberingTime");

        if (config != null && config.GetManaDrivePrefab() != null)
        {
            // Use configured BOP prefab (assuming ManaDrive field is repurposed for BOP)
            bopPrefab = config.GetManaDrivePrefab();
            Debug.Log($"✅ Using configured B.O.P prefab: {bopPrefab.name}");
        }
        else
        {
            // FIXED: Correct path to B.O.P prefab
            bopPrefab = Resources.Load<GameObject>("B.O.P");
            if (bopPrefab == null)
            {
                Debug.LogWarning("⚠️ B.O.P prefab not found in Resources/B.O.P");

                // Alternative: Try loading with different extensions
                bopPrefab = Resources.Load<GameObject>("B.O.P.prefab");
                if (bopPrefab == null)
                {
                    Debug.LogError("❌ B.O.P prefab not found in Resources folder!");
                    return;
                }
            }
            Debug.Log($"✅ Using Resources B.O.P prefab: {bopPrefab.name}");
        }

        // Use existing shop system to add unit to bench
        if (ShopManager.Instance != null)
        {
            // Find first empty bench slot
            Transform[] benchSlots = ShopManager.Instance.benchSlots;
            Transform freeSlot = null;
            HexTile freeTile = null;

            foreach (Transform benchSlot in benchSlots)
            {
                HexTile hexTile = benchSlot.GetComponent<HexTile>();
                if (hexTile != null && hexTile.tileType == TileType.Bench && hexTile.occupyingUnit == null)
                {
                    freeSlot = benchSlot;
                    freeTile = hexTile;
                    break;
                }
            }

            if (freeSlot != null && freeTile != null)
            {
                // Spawn B.O.P unit on bench
                GameObject unit = Object.Instantiate(bopPrefab, freeSlot.position, Quaternion.identity);
                unit.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

                // Set up the unit properly
                UnitAI newUnit = unit.GetComponent<UnitAI>();
                if (newUnit != null && freeTile.TryClaim(newUnit))
                {
                    newUnit.currentState = UnitAI.UnitState.Bench;
                    GameManager.Instance.RegisterUnit(newUnit, true);
                    Debug.Log("🤖 B.O.P unit granted successfully!");
                }
                else
                {
                    Debug.LogError("❌ Failed to set up B.O.P unit - UnitAI component missing or tile claim failed");
                    Object.Destroy(unit);
                }
            }
            else
            {
                Debug.Log("🪑 Bench is full - B.O.P couldn't be granted!");
            }
        }
        else
        {
            Debug.LogError("❌ ShopManager.Instance is null - cannot spawn B.O.P!");
        }
    }

    private void ApplyToClobbertrons()
    {
        UnitAI[] allUnits = Object.FindObjectsOfType<UnitAI>();
        foreach (var unit in allUnits)
        {
            if (unit.team == Team.Player)
            {
                ClobbertronTrait clobTrait = unit.GetComponent<ClobbertronTrait>();
                if (clobTrait != null)
                {
                    ApplyAugmentToUnit(unit, clobTrait);
                }
            }
        }

        Debug.Log($"🔨 Applied Clobbering Time to {allUnits.Count(u => u.team == Team.Player && u.GetComponent<ClobbertronTrait>() != null)} Clobbertrons");
    }

    private void ApplyAugmentToUnit(UnitAI unit, ClobbertronTrait clobTrait)
    {
        // FIX 1: Check if Clobbertron trait is actually active (unit has Clobbertron in traits list)
        if (!unit.traits.Contains(Trait.Clobbertron))
        {
            Debug.LogWarning($"⚠️ {unit.unitName} does not have active Clobbertron trait - skipping augment application");
            return;
        }

        // FIX: Remove Clobbertron's attack damage bonus BEFORE adding augment bonus
        if (clobTrait != null && clobTrait.appliedAttackBonus > 0)
        {
            // Remove the Clobbertron's applied attack bonus
            unit.attackDamage -= clobTrait.appliedAttackBonus;
            Debug.Log($"🔨 Removed Clobbertron attack bonus ({clobTrait.appliedAttackBonus}) from {unit.unitName}");

            // Update the trait to reflect no attack bonus
            clobTrait.bonusAttackDamage = 0f;
            clobTrait.appliedAttackBonus = 0f;
        }

        // NOW add augment attack damage bonus
        unit.attackDamage += bonusAttackDamage;
        Debug.Log($"🔨 Added augment attack bonus (+{bonusAttackDamage}) to {unit.unitName}");

        // FIX: Remove bonus armor from ClobbertronTrait properly
        if (clobTrait != null && clobTrait.appliedArmorBonus > 0)
        {
            // Remove the Clobbertron's applied armor bonus
            unit.armor -= clobTrait.appliedArmorBonus;
            // Update the trait to reflect no armor bonus
            clobTrait.bonusArmor = 0f;
            clobTrait.appliedArmorBonus = 0f;
            Debug.Log($"🔨 Removed Clobbertron armor bonus from {unit.unitName}");
        }
        else
        {
            // Fallback: set base armor to 0 if no trait found
            unit.armor = 0;
            Debug.Log($"🔨 Set base armor to 0 for {unit.unitName} (no active ClobbertronTrait found)");
        }

        // Add jump behavior component
        ClobbertronJumpBehaviour jumpBehavior = unit.GetComponent<ClobbertronJumpBehaviour>();
        if (jumpBehavior == null)
        {
            jumpBehavior = unit.gameObject.AddComponent<ClobbertronJumpBehaviour>();
        }

        jumpBehavior.lowHealthThreshold = lowHealthThreshold;
        jumpBehavior.augmentColor = augmentColor;

        // Reset jump behavior first
        jumpBehavior.ResetJumpBehavior();

        // Force state refresh to trigger jump behavior for existing combat units
        if (unit.currentState == UnitAI.UnitState.Combat)
        {
            Debug.Log($"🔨 {unit.unitName} is in combat - forcing state refresh to trigger jump");

            // Use AugmentManager to handle the coroutine
            if (AugmentManager.Instance != null)
            {
                AugmentManager.Instance.StartCoroutine(ForceStateRefreshForJump(unit));
            }
        }

        Debug.Log($"🔨 Applied Clobbering Time to {unit.unitName}: +{bonusAttackDamage} AD (total: {unit.attackDamage}), No Armor, Jump Behavior");
    }

    // Add this helper coroutine to ItsClobberingTimeAugment class
    private System.Collections.IEnumerator ForceStateRefreshForJump(UnitAI unit)
    {
        if (unit == null || !unit.isAlive) yield break;

        Debug.Log($"🔨 Starting state refresh for {unit.unitName}");

        // Store original state
        var originalState = unit.currentState;

        // Briefly change to BoardIdle (this resets jump behavior flags)
        unit.SetState(UnitAI.UnitState.BoardIdle);

        // Wait one frame for state to fully process
        yield return null;

        // Restore to original state (this triggers the Combat state transition in jump behavior)
        unit.SetState(originalState);

        Debug.Log($"🔨 {unit.unitName} state refresh completed ({UnitAI.UnitState.BoardIdle} → {originalState})");
    }

    public override void OnCombatStart()
    {
        // Reset jump behaviors for new combat
        UnitAI[] clobbertrons = Object.FindObjectsOfType<UnitAI>().Where(u =>
            u.team == Team.Player && u.GetComponent<ClobbertronTrait>() != null).ToArray();

        Debug.Log($"🔨 Combat started - resetting jump behavior for {clobbertrons.Length} Clobbertrons");

        foreach (var clob in clobbertrons)
        {
            ClobbertronJumpBehaviour jumpBehavior = clob.GetComponent<ClobbertronJumpBehaviour>();
            if (jumpBehavior != null)
            {
                // Reset the jump behavior for new combat
                jumpBehavior.ResetJumpBehavior();
                Debug.Log($"🔨 Reset jump behavior for {clob.unitName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ {clob.unitName} has ClobbertronTrait but no JumpBehaviour component!");
            }
        }

        // Note: The actual combat start jump will be triggered by the ClobbertronJumpBehaviour 
        // components themselves when they receive the combat start event
    }

    public override void OnCombatEnd()
    {
        Debug.Log("🔨 Clobbering Time combat ended");
    }

    public override void OnUnitSpawned(UnitAI unit)
    {
        if (unit.team == Team.Player && unit.GetComponent<ClobbertronTrait>() != null)
        {
            ClobbertronTrait clobTrait = unit.GetComponent<ClobbertronTrait>();
            ApplyAugmentToUnit(unit, clobTrait);
            Debug.Log($"🔨 Applied Clobbering Time to newly spawned {unit.unitName}");
        }
    }

    public override void RemoveAugment()
    {
        // Remove augment effects from all Clobbertrons
        UnitAI[] allUnits = Object.FindObjectsOfType<UnitAI>();
        foreach (var unit in allUnits)
        {
            if (unit.team == Team.Player && unit.GetComponent<ClobbertronTrait>() != null)
            {
                // Remove attack damage bonus
                unit.attackDamage -= bonusAttackDamage;

                // Restore armor (this might need adjustment based on original armor values)
                unit.armor = 10; 

                // Remove jump behavior component
                ClobbertronJumpBehaviour jumpBehavior = unit.GetComponent<ClobbertronJumpBehaviour>();
                if (jumpBehavior != null)
                {
                    Object.Destroy(jumpBehavior);
                }

                Debug.Log($"🔨 Removed Clobbering Time effects from {unit.unitName}");
            }
        }

        Debug.Log("🔨 Clobbering Time augment removed");
    }

    // Debug method to test B.O.P spawning manually
    [ContextMenu("Test B.O.P Spawn")]
    public void TestBOPSpawn()
    {
        Debug.Log("🔨 Testing B.O.P spawn manually...");
        SpawnBOP();
    }

    // Debug method to check current Clobbertrons
    [ContextMenu("Debug Clobbertrons")]
    public void DebugClobbertrons()
    {
        UnitAI[] clobbertrons = Object.FindObjectsOfType<UnitAI>().Where(u =>
            u.team == Team.Player && u.GetComponent<ClobbertronTrait>() != null).ToArray();

        Debug.Log($"🔨 Found {clobbertrons.Length} Clobbertrons in the scene:");

        foreach (var clob in clobbertrons)
        {
            ClobbertronJumpBehaviour jumpBehavior = clob.GetComponent<ClobbertronJumpBehaviour>();
            Debug.Log($"   - {clob.unitName}: AD={clob.attackDamage}, Armor={clob.armor}, HasJumpBehavior={jumpBehavior != null}");
        }
    }
}
