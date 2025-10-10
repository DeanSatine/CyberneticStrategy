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
        GameObject bopPrefab = Resources.Load<GameObject>("Prefabs/B.O.P");
        if (bopPrefab == null)
        {
            Debug.LogWarning("⚠️ B.O.P prefab not found in Resources/Prefabs/B.O.P");
            return;
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
                if (freeTile.TryClaim(newUnit))
                {
                    newUnit.currentState = UnitAI.UnitState.Bench;
                    GameManager.Instance.RegisterUnit(newUnit, true);
                    Debug.Log("🤖 B.O.P unit granted!");
                }
                else
                {
                    Object.Destroy(unit);
                }
            }
            else
            {
                Debug.Log("🪑 Bench is full - B.O.P couldn't be granted!");
            }
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
    }

    private void ApplyAugmentToUnit(UnitAI unit, ClobbertronTrait clobTrait)
    {
        // Add attack damage
        unit.attackDamage += bonusAttackDamage;

        // Remove armor (set to 0)
        unit.armor = 0;

        // Add jump behavior component
        ClobbertronJumpBehaviour jumpBehavior = unit.GetComponent<ClobbertronJumpBehaviour>();
        if (jumpBehavior == null)
        {
            jumpBehavior = unit.gameObject.AddComponent<ClobbertronJumpBehaviour>();
        }

        jumpBehavior.lowHealthThreshold = lowHealthThreshold;
        jumpBehavior.augmentColor = augmentColor;

        Debug.Log($"🔨 Applied Clobbering Time to {unit.unitName}: +{bonusAttackDamage} AD, No Armor, Jump Behavior");
    }

    public override void OnCombatStart()
    {
        // Make all Clobbertrons jump to their targets
        UnitAI[] clobbertrons = Object.FindObjectsOfType<UnitAI>().Where(u =>
            u.team == Team.Player && u.GetComponent<ClobbertronTrait>() != null).ToArray();

        foreach (var clob in clobbertrons)
        {
            ClobbertronJumpBehaviour jumpBehavior = clob.GetComponent<ClobbertronJumpBehaviour>();
            if (jumpBehavior != null)
            {
                jumpBehavior.JumpToTarget();
            }
        }
    }

    public override void OnCombatEnd() { }

    public override void OnUnitSpawned(UnitAI unit)
    {
        if (unit.team == Team.Player && unit.GetComponent<ClobbertronTrait>() != null)
        {
            ClobbertronTrait clobTrait = unit.GetComponent<ClobbertronTrait>();
            ApplyAugmentToUnit(unit, clobTrait);
        }
    }
}
