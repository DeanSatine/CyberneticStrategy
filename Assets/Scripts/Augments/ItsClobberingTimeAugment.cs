using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ItsClobberingTimeAugment : BaseAugment
{
    [Header("Augment Stats")]
    public float lowHealthThreshold = 0.1f;
    public float shockwaveDamageMultiplier = 1.5f;
    public int shockwaveHexRadius = 2;
    public bool grantHaymaker = true;

    private static HashSet<HexTile> reservedTiles = new HashSet<HexTile>();

    public ItsClobberingTimeAugment()
    {
        augmentName = "It's Clobbering Time!";
        description = "Clobbertrons jump to their target on combat start and again at 10% health. Jumps deal shockwave damage in a 2 hex radius. Gain a B.O.P.";
        type = AugmentType.Class;
        augmentColor = Color.blue;
    }

    public override void ApplyAugment()
    {
        Debug.Log($"🔨 Applying {augmentName}");

        reservedTiles.Clear();
        SpawnBOP();
        if (grantHaymaker)
        {
            SpawnHaymaker();
        }
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
    private void SpawnHaymaker()
    {
        GameObject haymakerPrefab = Resources.Load<GameObject>("Haymaker");

        if (haymakerPrefab == null)
        {
            Debug.LogError("❌ Haymaker prefab not found in Resources folder!");
            return;
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
                GameObject unit = Object.Instantiate(haymakerPrefab, freeSlot.position, Quaternion.identity);
                unit.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

                UnitAI newUnit = unit.GetComponent<UnitAI>();
                if (newUnit != null && freeTile.TryClaim(newUnit))
                {
                    newUnit.currentState = UnitAI.UnitState.Bench;
                    GameManager.Instance.RegisterUnit(newUnit, true);
                    Debug.Log("⚡ Haymaker unit granted successfully!");
                }
                else
                {
                    Debug.LogError("❌ Failed to set up Haymaker unit!");
                    Object.Destroy(unit);
                }
            }
            else
            {
                Debug.Log("🪑 Bench is full - Haymaker couldn't be granted!");
            }
        }
    }

    private void ApplyToClobbertrons()
    {
        UnitAI[] allUnits = Object.FindObjectsOfType<UnitAI>();
        int appliedCount = 0;

        foreach (var unit in allUnits.Where(u => u.team == Team.Player && u.traits.Contains(Trait.Clobbertron)))
        {
            ClobbertronTrait clobTrait = unit.GetComponent<ClobbertronTrait>();
            if (clobTrait == null)
            {
                clobTrait = unit.gameObject.AddComponent<ClobbertronTrait>();

                if (TraitManager.Instance != null)
                {
                    clobTrait.crashRadius = TraitManager.Instance.clobbertronCrashRadius;
                    clobTrait.crashDamage = TraitManager.Instance.clobbertronCrashDamage;
                }
            }

            ApplyAugmentToUnit(unit, clobTrait);
            appliedCount++;
        }

        Debug.Log($"🔨 Applied Clobbering Time to {appliedCount} Clobbertrons");
    }

    private void ApplyAugmentToUnit(UnitAI unit, ClobbertronTrait clobTrait)
    {
        if (!unit.traits.Contains(Trait.Clobbertron))
            return;

        ClobbertronJumpBehaviour jumpBehavior = unit.GetComponent<ClobbertronJumpBehaviour>();
        if (jumpBehavior == null)
            jumpBehavior = unit.gameObject.AddComponent<ClobbertronJumpBehaviour>();

        jumpBehavior.lowHealthThreshold = lowHealthThreshold;
        jumpBehavior.augmentColor = augmentColor;
        jumpBehavior.augment = this;
        jumpBehavior.shockwaveDamageMultiplier = shockwaveDamageMultiplier;
        jumpBehavior.shockwaveHexRadius = shockwaveHexRadius;

        // NEW: Get VFX prefab from AugmentConfiguration
        AugmentConfiguration[] configs = Object.FindObjectsOfType<AugmentConfiguration>();
        AugmentConfiguration config = System.Array.Find(configs,
            c => c != null && c.AugmentId == "ClobberingTime");

        if (config != null)
        {
            GameObject jumpVFX = config.GetJumpVFXPrefab();
            if (jumpVFX != null)
            {
                jumpBehavior.jumpVFXPrefab = jumpVFX;
                Debug.Log($"✅ Assigned jump VFX to {unit.unitName}: {jumpVFX.name}");
            }
            else
            {
                Debug.LogWarning("⚠️ Jump VFX prefab not assigned in AugmentConfiguration!");
            }
        }

        jumpBehavior.ResetJumpBehavior();
    }


    public override void OnCombatStart()
    {
        reservedTiles.Clear();

        foreach (var clob in Object.FindObjectsOfType<UnitAI>()
                     .Where(u => u.team == Team.Player && u.traits.Contains(Trait.Clobbertron)))
        {
            ClobbertronJumpBehaviour jump = clob.GetComponent<ClobbertronJumpBehaviour>();
            if (jump != null)
                jump.ResetJumpBehavior();
        }
    }

    public override void OnCombatEnd()
    {
        reservedTiles.Clear();
    }

    public override void OnUnitSpawned(UnitAI unit)
    {
        if (unit.team != Team.Player || !unit.traits.Contains(Trait.Clobbertron))
            return;

        ClobbertronTrait clob = unit.GetComponent<ClobbertronTrait>();
        if (clob == null) return;

        ApplyAugmentToUnit(unit, clob);
    }

    public override void RemoveAugment()
    {
        reservedTiles.Clear();

        foreach (var unit in Object.FindObjectsOfType<UnitAI>()
                     .Where(u => u.team == Team.Player && u.traits.Contains(Trait.Clobbertron)))
        {
            ClobbertronJumpBehaviour jump = unit.GetComponent<ClobbertronJumpBehaviour>();
            if (jump != null)
                Object.Destroy(jump);
        }

        Debug.Log("🔨 Clobbering Time augment removed");
    }

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
            reservedTiles.Remove(tile);
    }

    public bool IsTileReserved(HexTile tile) =>
        tile != null && reservedTiles.Contains(tile);
}
