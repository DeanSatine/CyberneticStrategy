using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ItsClobberingTimeAugment : BaseAugment
{
    [Header("Augment Stats")]
    public float bonusAttackDamage = 20f;
    public float lowHealthThreshold = 0.1f;

    // Track reserved landing tiles to prevent collisions
    private static HashSet<HexTile> reservedTiles = new HashSet<HexTile>();

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

        reservedTiles.Clear();

        SpawnBOP();
        ApplyToClobbertrons();
    }

    private void SpawnBOP()
    {
        GameObject bopPrefab = null;

        AugmentConfiguration[] configs = Object.FindObjectsOfType<AugmentConfiguration>();
        AugmentConfiguration config = System.Array.Find(configs,
            c => c != null && c.AugmentId == "ClobberingTime");

        if (config != null && config.GetManaDrivePrefab() != null)
        {
            bopPrefab = config.GetManaDrivePrefab();
            Debug.Log($"✅ Using configured B.O.P prefab: {bopPrefab.name}");
        }
        else
        {
            bopPrefab = Resources.Load<GameObject>("B.O.P");
            if (bopPrefab == null)
            {
                Debug.LogWarning("⚠️ B.O.P prefab not found in Resources/B.O.P");
                bopPrefab = Resources.Load<GameObject>("B.O.P.prefab");
                if (bopPrefab == null)
                {
                    Debug.LogError("❌ B.O.P prefab not found in Resources folder!");
                    return;
                }
            }
            Debug.Log($"✅ Using Resources B.O.P prefab: {bopPrefab.name}");
        }

        if (ShopManager.Instance != null)
        {
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
                GameObject unit = Object.Instantiate(bopPrefab, freeSlot.position, Quaternion.identity);
                unit.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

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
            if (unit.team == Team.Player && unit.traits.Contains(Trait.Clobbertron))
            {
                ClobbertronTrait clobTrait = unit.GetComponent<ClobbertronTrait>();
                if (clobTrait != null)
                {
                    ApplyAugmentToUnit(unit, clobTrait);
                }
            }
        }

        Debug.Log($"🔨 Applied Clobbering Time to {allUnits.Count(u => u.team == Team.Player && u.traits.Contains(Trait.Clobbertron))} Clobbertrons");
    }

    private void ApplyAugmentToUnit(UnitAI unit, ClobbertronTrait clobTrait)
    {
        if (!unit.traits.Contains(Trait.Clobbertron))
        {
            Debug.LogWarning($"⚠️ {unit.unitName} does not have active Clobbertron trait - skipping augment application");
            return;
        }

        // FIX BUG 1: Force apply trait bonuses first if not already applied
        if (!clobTrait.traitsApplied)
        {
            Debug.Log($"🔨 Forcing trait bonus application for {unit.unitName} before augment");
            clobTrait.ApplyTraitBonusesPublic();
        }

        // Remove Clobbertron's attack damage bonus
        if (clobTrait.appliedAttackBonus > 0)
        {
            unit.attackDamage -= clobTrait.appliedAttackBonus;
            Debug.Log($"🔨 Removed Clobbertron attack bonus ({clobTrait.appliedAttackBonus}) from {unit.unitName}");
            clobTrait.bonusAttackDamage = 0f;
            clobTrait.appliedAttackBonus = 0f;
        }

        // Add augment attack damage bonus
        unit.attackDamage += bonusAttackDamage;
        Debug.Log($"🔨 Added augment attack bonus (+{bonusAttackDamage}) to {unit.unitName}");

        // Remove armor bonus
        if (clobTrait.appliedArmorBonus > 0)
        {
            unit.armor -= clobTrait.appliedArmorBonus;
            clobTrait.bonusArmor = 0f;
            clobTrait.appliedArmorBonus = 0f;
            Debug.Log($"🔨 Removed Clobbertron armor bonus from {unit.unitName}");
        }
        else
        {
            unit.armor = 0;
            Debug.Log($"🔨 Set base armor to 0 for {unit.unitName}");
        }

        // Add jump behavior component
        ClobbertronJumpBehaviour jumpBehavior = unit.GetComponent<ClobbertronJumpBehaviour>();
        if (jumpBehavior == null)
        {
            jumpBehavior = unit.gameObject.AddComponent<ClobbertronJumpBehaviour>();
        }

        jumpBehavior.lowHealthThreshold = lowHealthThreshold;
        jumpBehavior.augmentColor = augmentColor;
        jumpBehavior.augment = this; // Give access to augment for tile reservation

        jumpBehavior.ResetJumpBehavior();

        // Force state refresh if in combat
        if (unit.currentState == UnitAI.UnitState.Combat)
        {
            Debug.Log($"🔨 {unit.unitName} is in combat - forcing state refresh to trigger jump");

            if (AugmentManager.Instance != null)
            {
                AugmentManager.Instance.StartCoroutine(ForceStateRefreshForJump(unit));
            }
        }

        Debug.Log($"🔨 Applied Clobbering Time to {unit.unitName}: +{bonusAttackDamage} AD (total: {unit.attackDamage}), No Armor, Jump Behavior");
    }

    private System.Collections.IEnumerator ForceStateRefreshForJump(UnitAI unit)
    {
        if (unit == null || !unit.isAlive) yield break;

        Debug.Log($"🔨 Starting state refresh for {unit.unitName}");

        var originalState = unit.currentState;
        unit.SetState(UnitAI.UnitState.BoardIdle);

        yield return null;

        unit.SetState(originalState);

        Debug.Log($"🔨 {unit.unitName} state refresh completed ({UnitAI.UnitState.BoardIdle} → {originalState})");
    }

    public override void OnCombatStart()
    {
        reservedTiles.Clear();

        UnitAI[] clobbertrons = Object.FindObjectsOfType<UnitAI>().Where(u =>
            u.team == Team.Player && u.traits.Contains(Trait.Clobbertron)).ToArray();

        Debug.Log($"🔨 Combat started - resetting jump behavior for {clobbertrons.Length} Clobbertrons");

        foreach (var clob in clobbertrons)
        {
            ClobbertronJumpBehaviour jumpBehavior = clob.GetComponent<ClobbertronJumpBehaviour>();
            if (jumpBehavior != null)
            {
                jumpBehavior.ResetJumpBehavior();
                Debug.Log($"🔨 Reset jump behavior for {clob.unitName}");
            }
        }
    }

    public override void OnCombatEnd()
    {
        reservedTiles.Clear();
        Debug.Log("🔨 Clobbering Time combat ended");
    }

    public override void OnUnitSpawned(UnitAI unit)
    {
        if (unit.team == Team.Player && unit.traits.Contains(Trait.Clobbertron))
        {
            ClobbertronTrait clobTrait = unit.GetComponent<ClobbertronTrait>();
            if (clobTrait != null)
            {
                ApplyAugmentToUnit(unit, clobTrait);
                Debug.Log($"🔨 Applied Clobbering Time to newly spawned {unit.unitName}");
            }
        }
    }

    public override void RemoveAugment()
    {
        reservedTiles.Clear();

        UnitAI[] allUnits = Object.FindObjectsOfType<UnitAI>();
        foreach (var unit in allUnits)
        {
            if (unit.team == Team.Player && unit.GetComponent<ClobbertronTrait>() != null)
            {
                unit.attackDamage -= bonusAttackDamage;
                unit.armor = 10;

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

    // FIX BUG 2: Tile reservation system
    public bool TryReserveTile(HexTile tile)
    {
        if (tile == null || reservedTiles.Contains(tile))
            return false;

        reservedTiles.Add(tile);
        return true;
    }

    public void UnreserveTile(HexTile tile)
    {
        if (tile != null)
        {
            reservedTiles.Remove(tile);
        }
    }

    public bool IsTileReserved(HexTile tile)
    {
        return tile != null && reservedTiles.Contains(tile);
    }
}
