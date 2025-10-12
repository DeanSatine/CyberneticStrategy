// Complete /Assets/Scripts/Augments/EradicateTheWeakAugment.cs
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

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

    // Track original stats to prevent stacking
    private float originalAttackDamage;
    private float originalAttackSpeed;
    private bool buffsApplied = false;

    // Execution tracking
    private static HashSet<EradicateTheWeakAugment> activeInstances = new HashSet<EradicateTheWeakAugment>();
    private Coroutine executionWatcher;
    private Coroutine periodicChecker;

    // VFX for linked unit
    private GameObject linkedUnitVFX;

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

        // Load configured settings from AugmentManager
        LoadConfiguredSettings();

        // Spawn ManaDrive unit
        SpawnManaDrive();

        // Find and link strongest Eradicator (with delay to ensure stats are ready)
        if (AugmentManager.Instance != null)
        {
            AugmentManager.Instance.StartCoroutine(DelayedStrongestCheck(0.1f, "Initial augment application"));
        }

        // Register this instance for execution tracking
        activeInstances.Add(this);

        // Start watching for executions
        StartExecutionWatcher();

        // Start periodic strongest check
        if (AugmentManager.Instance != null)
        {
            periodicChecker = AugmentManager.Instance.StartCoroutine(PeriodicStrongestCheck());
        }
    }

    private void LoadConfiguredSettings()
    {
        // Find the configuration for this augment
        AugmentConfiguration[] configs = Object.FindObjectsOfType<AugmentConfiguration>();
        AugmentConfiguration config = System.Array.Find(configs,
            c => c != null && c.AugmentId == "EradicateTheWeak");

        if (config != null)
        {
            bonusAttackDamage = config.GetBonusAttackDamage();
            bonusAttackSpeed = config.GetBonusAttackSpeed();
            healPercentage = config.GetHealPercentage();

            Debug.Log($"🔗 Loaded configuration: +{bonusAttackDamage} AD, +{bonusAttackSpeed * 100}% AS, {healPercentage * 100}% heal");
        }
    }

    private void StartExecutionWatcher()
    {
        if (AugmentManager.Instance != null)
        {
            executionWatcher = AugmentManager.Instance.StartCoroutine(WatchForExecutions());
        }
    }

    private IEnumerator WatchForExecutions()
    {
        int lastEnemyCount = 0;

        while (true)
        {
            // Count live enemies
            UnitAI[] enemies = Object.FindObjectsOfType<UnitAI>().Where(u =>
                u != null && u.isAlive && u.team == Team.Enemy).ToArray();

            int currentEnemyCount = enemies.Length;

            // If enemy count decreased, an execution might have happened
            if (currentEnemyCount < lastEnemyCount)
            {
                Debug.Log($"🔍 Enemy count decreased from {lastEnemyCount} to {currentEnemyCount} - checking for execution");

                // Small delay to let death animations process
                yield return new WaitForSeconds(0.1f);

                // Check if hydraulic press just completed an execution
                if (IsHydraulicPressActive())
                {
                    Debug.Log($"⚡ Hydraulic Press execution detected! Triggering healing for linked unit");
                    OnEradicatorExecution();
                }
            }

            lastEnemyCount = currentEnemyCount;
            yield return new WaitForSeconds(0.1f); // Check 10 times per second
        }
    }

    // In EradicateTheWeakAugment.cs, replace the IsHydraulicPressActive method:

    private bool IsHydraulicPressActive()
    {
        // Look for hydraulic press GameObject with better detection
        GameObject press = GameObject.FindWithTag("HydraulicPress");
        if (press == null)
        {
            // Try to find by name if tag doesn't exist
            press = GameObject.Find("HydraulicPress") ??
                    GameObject.Find("Hydraulic Press") ??
                    GameObject.Find("Press");
        }

        if (press != null)
        {
            // Better detection: Check if press has moved recently or is in motion
            Rigidbody pressRB = press.GetComponent<Rigidbody>();
            if (pressRB != null)
            {
                // Check if press is moving down or has recently impacted
                bool isDescending = pressRB.velocity.y < -0.1f;
                bool hasImpactedRecently = press.transform.position.y < 3f && pressRB.velocity.magnitude < 0.5f;

                return isDescending || hasImpactedRecently;
            }

            // Fallback: check position only
            return press.transform.position.y < 3f;
        }

        return false;
    }
    private IEnumerator ResetPressTracking()
    {
        yield return new WaitForSeconds(2f); // Wait for press to fully reset

        // Force check for press position reset
        GameObject press = GameObject.FindWithTag("HydraulicPress");
        if (press == null)
        {
            press = GameObject.Find("HydraulicPress") ?? GameObject.Find("Hydraulic Press") ?? GameObject.Find("Press");
        }

        if (press != null && press.transform.position.y > 5f)
        {
            Debug.Log("🔄 Hydraulic Press reset detected, ready for next execution");
        }
    }


    private void SpawnManaDrive()
    {
        // Get configured ManaDrive prefab
        AugmentConfiguration[] configs = Object.FindObjectsOfType<AugmentConfiguration>();
        AugmentConfiguration config = System.Array.Find(configs,
            c => c != null && c.AugmentId == "EradicateTheWeak");

        GameObject manaDrivePrefab = null;
        if (config != null && config.GetManaDrivePrefab() != null)
        {
            manaDrivePrefab = config.GetManaDrivePrefab();
        }
        else
        {
            // Fallback to Resources
            manaDrivePrefab = Resources.Load<GameObject>("Prefabs/ManaDrive");
        }

        if (manaDrivePrefab == null)
        {
            Debug.LogWarning("⚠️ ManaDrive prefab not found in configuration or Resources/Prefabs/ManaDrive");
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

    // Add periodic check for stronger units (in case we miss events)
    private IEnumerator PeriodicStrongestCheck()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f); // Check every 5 seconds

            if (linkedUnit != null && linkedUnit.isAlive)
            {
                UnitAI currentStrongest = FindStrongestEradicator();
                if (currentStrongest != null && currentStrongest != linkedUnit && IsStronger(currentStrongest, linkedUnit))
                {
                    Debug.Log($"🔍 Periodic check found stronger unit: {currentStrongest.unitName}");
                    LinkToSpecificEradicator(currentStrongest);
                }
            }
            else if (linkedUnit == null)
            {
                // Try to find an eradicator if we don't have one linked
                UnitAI currentStrongest = FindStrongestEradicator();
                if (currentStrongest != null)
                {
                    Debug.Log($"🔍 Periodic check found eradicator when none was linked: {currentStrongest.unitName}");
                    LinkToSpecificEradicator(currentStrongest);
                }
            }
        }
    }

    // Add this new coroutine for delayed checks
    private IEnumerator DelayedStrongestCheck(float delay, string reason)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log($"🔍 Performing delayed strongest check: {reason}");

        // Find current strongest and compare
        UnitAI currentStrongest = FindStrongestEradicator();

        if (currentStrongest != null)
        {
            if (linkedUnit == null)
            {
                Debug.Log($"🔗 No linked unit - linking to {currentStrongest.unitName}");
                LinkToSpecificEradicator(currentStrongest);
            }
            else if (currentStrongest != linkedUnit && IsStronger(currentStrongest, linkedUnit))
            {
                Debug.Log($"🔗 Found stronger Eradicator: {currentStrongest.unitName} (was: {linkedUnit.unitName})");
                LinkToSpecificEradicator(currentStrongest);
            }
            else
            {
                Debug.Log($"🔗 Current linked unit {linkedUnit.unitName} is still strongest");
            }
        }
    }

    // Add this helper method to find the strongest without linking
    private UnitAI FindStrongestEradicator()
    {
        UnitAI[] playerUnits = Object.FindObjectsOfType<UnitAI>().Where(u =>
            u != null && u.team == Team.Player && u.isAlive).ToArray();

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

        return strongestEradicator;
    }

    // Add this method to link to a specific unit
    private void LinkToSpecificEradicator(UnitAI targetUnit)
    {
        if (targetUnit == null) return;

        // Remove buffs from previous linked unit
        RemoveBuffsFromLinkedUnit();

        // Reset buff tracking for new unit
        buffsApplied = false;

        linkedUnit = targetUnit;
        CreateVisualLink();
        CreateLinkedUnitVFX();
        ApplyLinkedUnitBuffs();

        Debug.Log($"🔗 Successfully linked to: {linkedUnit.unitName} (AD: {linkedUnit.attackDamage}, Star: {linkedUnit.starLevel})");
    }


    // Update the LinkStrongestEradicator method to use the new helper
    private void LinkStrongestEradicator()
    {
        UnitAI strongestEradicator = FindStrongestEradicator();

        if (strongestEradicator != null)
        {
            LinkToSpecificEradicator(strongestEradicator);
            Debug.Log($"🔗 Initial link to strongest Eradicator: {linkedUnit.unitName}");
        }
        else
        {
            Debug.LogWarning("⚠️ No Eradicator units found to link to!");
        }
    }

    // Enhanced IsStronger method with better debugging
    private bool IsStronger(UnitAI unit1, UnitAI unit2)
    {
        if (unit1 == null || unit2 == null) return unit1 != null;

        // Priority: Star level first, then base attack damage as proxy for cost
        bool result = false;
        string reason = "";

        if (unit1.starLevel != unit2.starLevel)
        {
            result = unit1.starLevel > unit2.starLevel;
            reason = $"star level ({unit1.starLevel} vs {unit2.starLevel})";
        }
        else
        {
            result = unit1.attackDamage > unit2.attackDamage;
            reason = $"attack damage ({unit1.attackDamage} vs {unit2.attackDamage})";
        }

        Debug.Log($"🔍 Comparing {unit1.unitName} vs {unit2.unitName}: {(result ? "STRONGER" : "weaker")} due to {reason}");
        return result;
    }

    private void CreateVisualLink()
    {
        if (linkedUnit == null) return;

        // Clean up existing link
        if (linkLine != null)
        {
            Object.Destroy(linkLine.gameObject);
        }

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

    private void CreateLinkedUnitVFX()
    {
        if (linkedUnit == null) return;

        // Clean up existing VFX
        if (linkedUnitVFX != null)
        {
            Object.Destroy(linkedUnitVFX);
        }

        // Get configured VFX prefab
        AugmentConfiguration[] configs = Object.FindObjectsOfType<AugmentConfiguration>();
        AugmentConfiguration config = System.Array.Find(configs,
            c => c != null && c.AugmentId == "EradicateTheWeak");

        GameObject vfxPrefab = null;
        if (config != null && config.GetLinkedUnitVFXPrefab() != null)
        {
            vfxPrefab = config.GetLinkedUnitVFXPrefab();
            Debug.Log($"✨ Using configured linked unit VFX: {vfxPrefab.name}");
        }

        if (vfxPrefab != null)
        {
            // Use configured VFX prefab
            linkedUnitVFX = Object.Instantiate(vfxPrefab, linkedUnit.transform);
            linkedUnitVFX.transform.localPosition = Vector3.zero;
        }
        else
        {
            // Fallback: Create simple default VFX
            CreateDefaultLinkedUnitVFX();
        }

        Debug.Log($"✨ Created link VFX for {linkedUnit.unitName}");
    }

    private void CreateDefaultLinkedUnitVFX()
    {
        // Create a simple glowing aura around the linked unit (fallback)
        linkedUnitVFX = new GameObject("EradicatorLinkVFX");
        linkedUnitVFX.transform.SetParent(linkedUnit.transform);
        linkedUnitVFX.transform.localPosition = Vector3.up * 0.1f;

        // Create multiple rings for a more impressive effect
        for (int i = 0; i < 3; i++)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.SetParent(linkedUnitVFX.transform);
            ring.transform.localPosition = Vector3.up * (i * 0.3f);
            ring.transform.localRotation = Quaternion.Euler(90, 0, 0);
            ring.transform.localScale = Vector3.one * (1.5f + i * 0.3f);

            // Remove collider
            Collider collider = ring.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            // Set up material
            Renderer renderer = ring.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Sprites/Default"));
                material.color = new Color(augmentColor.r, augmentColor.g, augmentColor.b, 0.3f + i * 0.1f);
                renderer.material = material;
            }

            ring.name = $"LinkRing_{i}";
        }

        // Start rotation animation
        if (AugmentManager.Instance != null)
        {
            AugmentManager.Instance.StartCoroutine(AnimateLinkedUnitVFX());
        }
    }

    private IEnumerator AnimateLinkedUnitVFX()
    {
        while (linkedUnitVFX != null && linkedUnit != null)
        {
            if (linkedUnitVFX != null)
            {
                linkedUnitVFX.transform.Rotate(Vector3.up, 30f * Time.deltaTime);
            }
            yield return null;
        }
    }

    private IEnumerator UpdateLinkPosition()
    {
        while (linkedUnit != null && linkLine != null)
        {
            Vector3 pressPosition = new Vector3(0, 5f, -12f); // Default hydraulic press position
            GameObject press = GameObject.FindWithTag("HydraulicPress");
            if (press == null)
            {
                press = GameObject.Find("HydraulicPress") ?? GameObject.Find("Hydraulic Press");
            }

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

        // Only apply buffs if not already applied
        if (buffsApplied)
        {
            Debug.Log($"🔗 Buffs already applied to {linkedUnit.unitName}, skipping");
            return;
        }

        // Store current stats as "original" before any augment modifications
        // This assumes the unit's current stats are their base stats when first linked
        originalAttackDamage = linkedUnit.attackDamage;
        originalAttackSpeed = linkedUnit.attackSpeed;

        // Apply buffs as bonuses to current stats
        linkedUnit.attackDamage = originalAttackDamage + bonusAttackDamage;
        linkedUnit.attackSpeed = originalAttackSpeed * (1f + bonusAttackSpeed);

        buffsApplied = true;

        // Update UI using correct UnitUI methods
        if (linkedUnit.ui != null)
        {
            linkedUnit.ui.UpdateHealth(linkedUnit.currentHealth);
            linkedUnit.ui.SetMaxHealth(linkedUnit.maxHealth);
        }

        Debug.Log($"🔗 Applied buffs to {linkedUnit.unitName}:");
        Debug.Log($"   Attack Damage: {originalAttackDamage} → {linkedUnit.attackDamage} (+{bonusAttackDamage})");
        Debug.Log($"   Attack Speed: {originalAttackSpeed:F2} → {linkedUnit.attackSpeed:F2} (+{bonusAttackSpeed * 100}%)");
    }
    private float CalculateBaseAttackDamage(UnitAI unit)
    {
        // Try to estimate base attack damage by checking unit cost/star level
        float baseAD = unit.attackDamage;

        // If this unit already has augment buffs, try to subtract them
        // This is a fallback method - ideally use UnitData
        if (buffsApplied)
        {
            baseAD = unit.attackDamage - bonusAttackDamage;
        }

        return Mathf.Max(1f, baseAD); // Ensure it's at least 1
    }
    private float CalculateBaseAttackSpeed(UnitAI unit)
    {
        // Try to estimate base attack speed
        float baseAS = unit.attackSpeed;

        // If this unit already has augment buffs, try to subtract them
        if (buffsApplied)
        {
            baseAS = unit.attackSpeed / (1f + bonusAttackSpeed);
        }

        return Mathf.Max(0.1f, baseAS); // Ensure it's at least 0.1
    }
    private void RemoveBuffsFromLinkedUnit()
    {
        if (linkedUnit != null && buffsApplied)
        {
            // Restore original stats (subtract the bonuses we added)
            linkedUnit.attackDamage = originalAttackDamage;
            linkedUnit.attackSpeed = originalAttackSpeed;

            Debug.Log($"🔗 Removed buffs from {linkedUnit.unitName} (restored to: AD={originalAttackDamage}, AS={originalAttackSpeed:F2})");
        }

        // Clean up VFX
        if (linkedUnitVFX != null)
        {
            Object.Destroy(linkedUnitVFX);
            linkedUnitVFX = null;
        }

        buffsApplied = false;
    }


    public void OnEradicatorExecution()
    {
        if (linkedUnit != null && linkedUnit.isAlive)
        {
            float healAmount = linkedUnit.maxHealth * healPercentage;
            float oldHealth = linkedUnit.currentHealth;
            linkedUnit.currentHealth = Mathf.Min(linkedUnit.maxHealth, linkedUnit.currentHealth + healAmount);

            // Update UI using correct UnitUI method
            if (linkedUnit.ui != null)
            {
                linkedUnit.ui.UpdateHealth(linkedUnit.currentHealth);
            }

            // Play heal VFX
            PlayHealVFX();

            Debug.Log($"💚 {linkedUnit.unitName} healed for {(int)healAmount}!");
            Debug.Log($"   Health: {(int)oldHealth} → {(int)linkedUnit.currentHealth}/{(int)linkedUnit.maxHealth}");
        }
    }

    private void PlayHealVFX()
    {
        if (linkedUnit == null) return;

        // Get configured heal VFX prefab
        AugmentConfiguration[] configs = Object.FindObjectsOfType<AugmentConfiguration>();
        AugmentConfiguration config = System.Array.Find(configs,
            c => c != null && c.AugmentId == "EradicateTheWeak");

        GameObject healVFXPrefab = null;
        if (config != null && config.GetHealVFXPrefab() != null)
        {
            healVFXPrefab = config.GetHealVFXPrefab();
            Debug.Log($"💚 Using configured heal VFX: {healVFXPrefab.name}");
        }

        if (healVFXPrefab != null)
        {
            // Use configured VFX prefab
            GameObject vfx = Object.Instantiate(healVFXPrefab, linkedUnit.transform.position + Vector3.up * 1f, Quaternion.identity);
            Object.Destroy(vfx, 3f); // Destroy after 3 seconds
        }
        else
        {
            // Fallback: Create simple healing effect
            CreateDefaultHealVFX();
        }
    }

    private void CreateDefaultHealVFX()
    {
        // Create simple green healing effect as fallback
        GameObject healEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        healEffect.transform.position = linkedUnit.transform.position + Vector3.up * 2f;
        healEffect.transform.localScale = Vector3.one * 0.8f;

        Renderer renderer = healEffect.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Sprites/Default"));
            material.color = Color.green;
            renderer.material = material;
        }

        // Remove collider to avoid physics issues
        Collider collider = healEffect.GetComponent<Collider>();
        if (collider != null)
        {
            Object.Destroy(collider);
        }

        // Animate the healing effect
        if (AugmentManager.Instance != null)
        {
            AugmentManager.Instance.StartCoroutine(AnimateHealEffect(healEffect));
        }
    }

    private IEnumerator AnimateHealEffect(GameObject effect)
    {
        Vector3 startPos = effect.transform.position;
        Vector3 endPos = startPos + Vector3.up * 2f;
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration && effect != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Move up and fade out
            effect.transform.position = Vector3.Lerp(startPos, endPos, t);

            Renderer renderer = effect.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color.a = 1f - t;
                renderer.material.color = color;
            }

            yield return null;
        }

        if (effect != null)
        {
            Object.Destroy(effect);
        }
    }

    public override void OnCombatStart()
    {
        Debug.Log($"🔗 {augmentName} - Combat started");
    }

    public override void OnCombatEnd()
    {
        Debug.Log($"🔗 {augmentName} - Combat ended, checking for strongest unit");

        // Re-evaluate strongest unit after combat (stats might have changed)
        if (AugmentManager.Instance != null)
        {
            AugmentManager.Instance.StartCoroutine(DelayedStrongestCheck(0.2f, "Combat ended"));
        }
    }

    public override void OnUnitSpawned(UnitAI unit)
    {
        // Check if this new unit should be the linked unit
        if (unit.team == Team.Player && unit.GetComponent<EradicatorTrait>() != null)
        {
            Debug.Log($"🔍 New Eradicator spawned: {unit.unitName} - checking if stronger in 0.5s");

            // Delay the check to allow stats to be fully applied
            if (AugmentManager.Instance != null)
            {
                AugmentManager.Instance.StartCoroutine(DelayedStrongestCheck(0.5f, "New unit spawned"));
            }
        }
    }

    public override void RemoveAugment()
    {
        // Clean up execution watcher
        if (executionWatcher != null && AugmentManager.Instance != null)
        {
            AugmentManager.Instance.StopCoroutine(executionWatcher);
        }

        // Clean up periodic checker
        if (periodicChecker != null && AugmentManager.Instance != null)
        {
            AugmentManager.Instance.StopCoroutine(periodicChecker);
        }

        // Remove buffs from linked unit
        RemoveBuffsFromLinkedUnit();

        // Remove from active instances
        activeInstances.Remove(this);

        // Clean up visual link
        if (linkLine != null)
        {
            Object.Destroy(linkLine.gameObject);
        }

        // Clean up VFX
        if (linkedUnitVFX != null)
        {
            Object.Destroy(linkedUnitVFX);
        }

        Debug.Log($"🔗 {augmentName} removed and cleaned up");
    }
}
