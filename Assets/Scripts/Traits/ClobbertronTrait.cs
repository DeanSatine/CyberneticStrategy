using UnityEngine;
using System.Collections;

public class ClobbertronTrait : MonoBehaviour
{
    [HideInInspector] public float bonusArmor = 10f;
    [HideInInspector] public float bonusAttackDamage = 10f;
    [HideInInspector] public float crashRadius = 2f;
    [HideInInspector] public float crashDamage = 200f;

    private UnitAI unitAI;
    public bool traitsApplied = false;
    private bool hasCrashed = false;
    private bool isCrashing = false;

    public float appliedArmorBonus = 0f;
    public float appliedAttackBonus = 0f;
    private float baseAttackDamage = 0f;

    [Header("VFX / Animation")]
    public GameObject slamEffectPrefab;
    public float jumpHeight = 2f;
    public float jumpDuration = 0.3f;
    public float slamDuration = 0.2f;
    public float cameraShakeIntensity = 0.4f;
    public float cameraShakeDuration = 0.25f;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
    }

    private void Update()
    {
        if (unitAI == null || !unitAI.isAlive) return;

        // CRITICAL - Deactivate trait if unit is benched
        if (unitAI.currentState == UnitAI.UnitState.Bench)
        {
            if (traitsApplied)
            {
                Debug.Log($"🏟️ {unitAI.unitName} moved to bench - removing Clobbertron bonuses");
                RemoveTraitBonuses();
            }

            Destroy(this);
            return;
        }

        // Apply bonuses immediately when unit is placed on board
        if (unitAI.currentState == UnitAI.UnitState.BoardIdle && !traitsApplied)
        {
            ApplyTraitBonuses();
        }

        // Trigger crash once when hitting 50% HP (only during combat)
        if (!hasCrashed && unitAI.currentState == UnitAI.UnitState.Combat &&
            unitAI.currentHealth <= unitAI.maxHealth * 0.5f && !isCrashing)
        {
            StartCoroutine(CrashSequence());
        }
    }
    private void ApplyTraitBonuses()
    {
        if (traitsApplied) return;

        Debug.Log($"🔍 [CLOBBERTRON DEBUG] {unitAI.unitName} - bonusArmor: {bonusArmor}, bonusAttackDamage: {bonusAttackDamage}");

        // Store the base values before applying bonuses
        baseAttackDamage = unitAI.attackDamage;

        // FIX: Apply flat bonuses
        appliedArmorBonus = bonusArmor;        // +10 armor
        appliedAttackBonus = bonusAttackDamage; // +10 attack damage (flat)

        // Apply bonuses
        unitAI.armor += appliedArmorBonus;
        unitAI.attackDamage += appliedAttackBonus;

        traitsApplied = true;

        Debug.Log($"🔨 {unitAI.unitName} Clobbertron activated! +{appliedArmorBonus} armor, +{appliedAttackBonus} attack damage");
        Debug.Log($"🔨 Final stats: {unitAI.armor} armor, {unitAI.attackDamage} attack damage");
    }

    private void RemoveTraitBonuses()
    {
        if (!traitsApplied) return;

        Debug.Log($"🔍 [CLOBBERTRON REMOVE] Removing bonuses from {unitAI.unitName}");

        // Remove the exact bonuses that were applied
        unitAI.armor -= appliedArmorBonus;
        unitAI.attackDamage -= appliedAttackBonus;

        // Reset tracking variables
        traitsApplied = false;
        appliedArmorBonus = 0f;
        appliedAttackBonus = 0f;
        baseAttackDamage = 0f;

        Debug.Log($"💔 {unitAI.unitName} Clobbertron deactivated! -{appliedArmorBonus} armor, -{appliedAttackBonus} attack damage");
        Debug.Log($"🔨 Final stats: {unitAI.armor} armor, {unitAI.attackDamage} attack damage");
    }

    // Rest of the methods remain the same...
    private IEnumerator CrashSequence()
    {
        isCrashing = true;
        hasCrashed = true;

        Vector3 startPos = transform.position;
        Vector3 jumpPos = startPos + Vector3.up * jumpHeight;

        Debug.Log($"🔨 {unitAI.unitName} starting crash sequence at 50% HP!");

        // Jump up
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / jumpDuration;
            transform.position = Vector3.Lerp(startPos, jumpPos, t);
            yield return null;
        }

        // Slam down
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / slamDuration;
            transform.position = Vector3.Lerp(jumpPos, startPos, t);
            yield return null;
        }

        // Slam FX
        if (slamEffectPrefab != null)
        {
            GameObject fx = Instantiate(slamEffectPrefab, startPos, Quaternion.identity);
            Destroy(fx, 2f);
        }
        CameraShakeManager.Instance.Shake(cameraShakeIntensity, cameraShakeDuration);

        // Do AoE crash damage
        Collider[] hits = Physics.OverlapSphere(startPos, crashRadius);
        int hitCount = 0;
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<UnitAI>();
            if (enemy != null && enemy.team != unitAI.team && enemy.isAlive)
            {
                enemy.TakeDamage(crashDamage);
                hitCount++;
                Debug.Log($"🔨 {unitAI.unitName} crash damaged {enemy.unitName} for {crashDamage}!");
            }
        }

        Debug.Log($"🔨 {unitAI.unitName} crash sequence completed - hit {hitCount} enemies");
        isCrashing = false;
    }


    public void ApplyTraitBonusesPublic()
    {
        ApplyTraitBonuses();
    }

    public void RemoveTraitBonusesPublic()
    {
        RemoveTraitBonuses();
    }

    public static void ResetAllClobbertrons()
    {
        ClobbertronTrait[] allClobbertrons = FindObjectsOfType<ClobbertronTrait>();
        Debug.Log($"🔍 [CLOBBERTRON RESET] Found {allClobbertrons.Length} Clobbertron components to reset");

        foreach (var clobbertron in allClobbertrons)
        {
            if (clobbertron != null && clobbertron.unitAI != null)
            {
                Debug.Log($"🔍 [CLOBBERTRON RESET] Resetting Clobbertron for {clobbertron.unitAI.unitName}");
                clobbertron.RemoveTraitBonuses();
                Destroy(clobbertron);
            }
        }

        Debug.Log("🔄 All Clobbertron traits reset");
    }

    [ContextMenu("Apply Clobbertron Bonuses")]
    public void DebugApplyBonuses()
    {
        ApplyTraitBonuses();
    }

    [ContextMenu("Remove Clobbertron Bonuses")]
    public void DebugRemoveBonuses()
    {
        RemoveTraitBonuses();
    }
    public void ForceRefreshBonuses()
    {
        if (unitAI == null) return;

        if (traitsApplied)
            RemoveTraitBonuses();

        ApplyTraitBonuses();

        Debug.Log($"♻️ [ClobbertronTrait] Force refreshed bonuses for {unitAI.unitName}: " +
                  $"+{bonusAttackDamage} AD, +{bonusArmor} Armor");
    }


    [ContextMenu("Debug Clobbertron State")]
    public void DebugClobbertronState()
    {
        Debug.Log($"🔨 {unitAI.unitName} Clobbertron State:");
        Debug.Log($"   - Traits Applied: {traitsApplied}");
        Debug.Log($"   - Applied Armor Bonus: {appliedArmorBonus}");
        Debug.Log($"   - Applied Attack Bonus: {appliedAttackBonus}");
        Debug.Log($"   - Base Attack Damage: {baseAttackDamage}");
        Debug.Log($"   - Current Armor: {unitAI.armor}");
        Debug.Log($"   - Current Attack Damage: {unitAI.attackDamage}");
        Debug.Log($"   - Current State: {unitAI.currentState}");
        Debug.Log($"   - Has Crashed: {hasCrashed}");
        Debug.Log($"   - Is Crashing: {isCrashing}");
    }
}
