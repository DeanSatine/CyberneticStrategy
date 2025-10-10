using UnityEngine;
using System.Linq;
using System.Collections;

[System.Serializable]
public class EradicateTheWeakAugment : BaseAugment
{
    private UnitAI linkedUnit;
    private GameObject linkVFX;
    private LineRenderer linkLine;

    [Header("Augment Stats")]
    public float bonusAttackDamage = 10f;
    public float bonusAttackSpeed = 0.1f; // 10%
    public float healPercentage = 0.1f; // 10%

    public EradicateTheWeakAugment()
    {
        augmentName = "Eradicate the Weak";
        description = "Your strongest Eradicator unit is linked to the Hydraulic Press. They gain 10 attack damage, 10% attack speed and heal 10% of their maximum health whenever the press executes an enemy. Gain a ManaDrive.";
        type = AugmentType.Origin;
        augmentColor = Color.red;
    }

    public override void ApplyAugment()
    {
        Debug.Log($"🔗 Applying {augmentName}");

        // Spawn ManaDrive unit
        SpawnManaDrive();

        // Find and link strongest Eradicator
        LinkStrongestEradicator();

        // Subscribe to Eradicator execution events
        SubscribeToExecutionEvents();
    }

    private void SpawnManaDrive()
    {
        // Load ManaDrive prefab from your project
        GameObject manaDrivePrefab = Resources.Load<GameObject>("Prefabs/ManaDrive");
        if (manaDrivePrefab == null)
        {
            Debug.LogWarning("⚠️ ManaDrive prefab not found in Resources/Prefabs/ManaDrive");
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
                // Spawn ManaDrive unit on bench
                GameObject unit = Object.Instantiate(manaDrivePrefab, freeSlot.position, Quaternion.identity);
                unit.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

                // Set up the unit properly
                UnitAI newUnit = unit.GetComponent<UnitAI>();
                if (freeTile.TryClaim(newUnit))
                {
                    newUnit.currentState = UnitAI.UnitState.Bench;
                    GameManager.Instance.RegisterUnit(newUnit, true);
                    Debug.Log("🤖 ManaDrive unit granted!");
                }
                else
                {
                    Object.Destroy(unit);
                }
            }
            else
            {
                Debug.Log("🪑 Bench is full - ManaDrive couldn't be granted!");
            }
        }
    }

    private void LinkStrongestEradicator()
    {
        UnitAI[] playerUnits = Object.FindObjectsOfType<UnitAI>().Where(u => u.team == Team.Player).ToArray();
        UnitAI strongestEradicator = null;

        foreach (var unit in playerUnits)
        {
            EradicatorTrait eradicatorTrait = unit.GetComponent<EradicatorTrait>();
            if (eradicatorTrait != null)
            {
                if (strongestEradicator == null || IsStronger(unit, strongestEradicator))
                {
                    strongestEradicator = unit;
                }
            }
        }

        if (strongestEradicator != null)
        {
            linkedUnit = strongestEradicator;
            CreateVisualLink();
            ApplyLinkedUnitBuffs();
            Debug.Log($"🔗 Linked to strongest Eradicator: {linkedUnit.unitName}");
        }
    }

    private bool IsStronger(UnitAI unit1, UnitAI unit2)
    {
        // Priority: Star level first, then base attack damage as proxy for cost
        if (unit1.starLevel != unit2.starLevel)
        {
            return unit1.starLevel > unit2.starLevel;
        }

        // Use base attack damage as cost proxy
        return unit1.attackDamage > unit2.attackDamage;
    }

    private void CreateVisualLink()
    {
        if (linkedUnit == null) return;

        // Create link line renderer
        GameObject linkObject = new GameObject("EradicatorLink");
        linkLine = linkObject.AddComponent<LineRenderer>();
        linkLine.material = new Material(Shader.Find("Sprites/Default"));

        // Fix LineRenderer color access
        if (linkLine.material != null)
        {
            linkLine.material.color = augmentColor;
        }

        linkLine.startWidth = 0.1f;
        linkLine.endWidth = 0.1f;
        linkLine.positionCount = 2;

        // Start coroutine to update link position
        if (AugmentManager.Instance != null)
        {
            AugmentManager.Instance.StartCoroutine(UpdateLinkPosition());
        }
    }

    private IEnumerator UpdateLinkPosition()
    {
        while (linkedUnit != null && linkLine != null)
        {
            Vector3 pressPosition = new Vector3(0, 5f, -12f); // Default hydraulic press position
            GameObject press = GameObject.FindWithTag("HydraulicPress");
            if (press != null)
            {
                pressPosition = press.transform.position;
            }

            linkLine.SetPosition(0, linkedUnit.transform.position + Vector3.up * 2f);
            linkLine.SetPosition(1, pressPosition + Vector3.up * 1f);

            yield return null;
        }
    }

    private void ApplyLinkedUnitBuffs()
    {
        if (linkedUnit == null) return;

        linkedUnit.attackDamage += bonusAttackDamage;
        linkedUnit.attackSpeed *= (1f + bonusAttackSpeed);

        Debug.Log($"🔗 Applied buffs to {linkedUnit.unitName}: +{bonusAttackDamage} AD, +{bonusAttackSpeed * 100}% AS");
    }

    private void SubscribeToExecutionEvents()
    {
        // This would need integration with EradicatorTrait to notify when executions happen
        // For now, we'll check in combat updates
    }

    public void OnEradicatorExecution()
    {
        if (linkedUnit != null && linkedUnit.isAlive)
        {
            float healAmount = linkedUnit.maxHealth * healPercentage;
            linkedUnit.currentHealth = Mathf.Min(linkedUnit.maxHealth, linkedUnit.currentHealth + healAmount);

            // Update UI
            if (linkedUnit.ui != null)
            {
                linkedUnit.ui.UpdateHealth(linkedUnit.currentHealth);
            }

            // Play heal VFX
            PlayHealVFX();

            Debug.Log($"💚 {linkedUnit.unitName} healed for {healAmount} from execution!");
        }
    }

    private void PlayHealVFX()
    {
        if (linkedUnit != null && augmentVFXPrefab != null)
        {
            GameObject vfx = Object.Instantiate(augmentVFXPrefab, linkedUnit.transform.position, Quaternion.identity);
            Object.Destroy(vfx, 2f);
        }
    }

    public override void OnCombatStart() { }
    public override void OnCombatEnd() { }
    public override void OnUnitSpawned(UnitAI unit)
    {
        // Check if this new unit should be the linked unit
        if (unit.team == Team.Player && unit.GetComponent<EradicatorTrait>() != null)
        {
            if (linkedUnit == null || IsStronger(unit, linkedUnit))
            {
                LinkStrongestEradicator();
            }
        }
    }
}
