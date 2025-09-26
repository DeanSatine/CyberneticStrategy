using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class NeedleBotAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;
    private CyberneticVFX vfxManager;

    [Header("Ability Stats")]
    public int baseNeedleCount = 4;
    [SerializeField] public int needlesPerCast; // ✅ FIXED: Made private, controlled by logic
    public float[] damagePerStar = { 100f, 150f, 175f };

    // ✅ FIXED: This should be persistent across rounds
    [Header("Needle Stacking Progress")]
    [SerializeField] private int totalNeedlesThrown = 0; // Persistent needle counter
    [SerializeField] private int stackingThreshold = 10;  // How many needles needed per stack

    [Header("Audio")]
    public AudioClip needleThrowSound;
    [Range(0f, 1f)] public float volume = 0.7f;
    private AudioSource audioSource;

    [Header("Timings")]
    public float startDelay = 0.25f;
    public float throwInterval = 0.1f;
    public float abilityDuration = 1.5f; // NOTE: Actual duration is now dynamic

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        vfxManager = GetComponent<CyberneticVFX>();

        // ✅ FIXED: Calculate needles based on current stacking progress
        CalculateNeedleCount();

        // ✅ Setup audio source for individual needle sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.8f; // 3D spatial sound
        }
    }

    // ✅ NEW: Calculate current needle count based on stacking progress
    private void CalculateNeedleCount()
    {
        int bonusNeedles = totalNeedlesThrown / stackingThreshold;
        needlesPerCast = baseNeedleCount + bonusNeedles;

        Debug.Log($"🎯 {unitAI.unitName} calculated needles: {needlesPerCast} (base: {baseNeedleCount} + bonus: {bonusNeedles} from {totalNeedlesThrown} total thrown)");
    }

    public void Cast(UnitAI target)
    {
        if (!unitAI.isAlive) return;

        unitAI.canAttack = false;
        unitAI.canMove = false;

        if (unitAI.animator != null)
            unitAI.animator.SetTrigger("AbilityTrigger");
        if (target != null && target.currentState == UnitAI.UnitState.Bench)
            return;

        // ✅ REMOVED: Don't call hardcoded VFX method
        // ✅ REMOVED: vfxManager.PlayNeedlebotRapidThrow();

        StartCoroutine(FireNeedlesRoutine());
    }

    private IEnumerator FireNeedlesRoutine()
    {
        yield return new WaitForSeconds(startDelay);

        // ✅ Recalculate needles right before firing (in case of mid-combat stacking)
        CalculateNeedleCount();

        List<UnitAI> targets = FindSmartTargets();
        if (targets.Count == 0)
        {
            Debug.Log($"{unitAI.unitName} tried to cast but found no enemies!");
            EndCast();
            yield break;
        }

        float damage = damagePerStar[Mathf.Clamp(unitAI.starLevel - 1, 0, damagePerStar.Length - 1)];

        Debug.Log($"🎯 {unitAI.unitName} firing {needlesPerCast} needles at targets: {string.Join(", ", targets.ConvertAll(t => t.unitName))}");

        // ✅ FIXED: Fire ALL accumulated needles with proper audio and VFX per needle
        for (int needleIndex = 0; needleIndex < needlesPerCast; needleIndex++)
        {
            UnitAI target = targets[needleIndex % targets.Count]; // Alternate between targets

            if (target != null && target.isAlive && target.currentState != UnitState.Bench)
            {
                // ✅ Play individual needle sound
                PlayNeedleSound();

                // ✅ Add muzzle flash for each needle
                Vector3 spawnPos = unitAI.firePoint != null ?
                    unitAI.firePoint.position :
                    transform.position + Vector3.up * 1.5f;

                if (vfxManager != null && vfxManager.vfxConfig.autoAttackMuzzleFlash != null)
                {
                    GameObject muzzleFlash = Instantiate(vfxManager.vfxConfig.autoAttackMuzzleFlash, spawnPos, Quaternion.identity);
                    Destroy(muzzleFlash, 0.5f);
                }

                // ✅ Fire projectile (uses VFX system's projectile as fallback)
                StartCoroutine(FireNeedleProjectile(spawnPos, target, damage));

                // ✅ FIXED: Increment counter AFTER successful needle fire
                totalNeedlesThrown++;

                // ✅ Check if we unlocked another needle for NEXT cast
                if (totalNeedlesThrown % stackingThreshold == 0)
                {
                    Debug.Log($"🎯 {unitAI.unitName} unlocked bonus needle! Next cast will have {baseNeedleCount + (totalNeedlesThrown / stackingThreshold)} needles (total thrown: {totalNeedlesThrown})");
                }
            }

            // ✅ Wait between each needle for sequential firing
            yield return new WaitForSeconds(throwInterval);
        }

        yield return new WaitForSeconds(0.2f);
        EndCast();
    }

    // ✅ NEW: Play sound for individual needle throw
    private void PlayNeedleSound()
    {
        if (needleThrowSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(needleThrowSound, volume);
        }
        else if (vfxManager != null && vfxManager.vfxConfig.autoAttackSound != null)
        {
            // Fallback to auto attack sound
            audioSource.PlayOneShot(vfxManager.vfxConfig.autoAttackSound, volume);
        }
    }

    // ✅ IMPROVED: Fire individual needle projectile with better fallback
    private IEnumerator FireNeedleProjectile(Vector3 startPos, UnitAI target, float damage)
    {
        if (target == null) yield break;

        // ✅ Try to get projectile from VFX system first, then unitAI
        GameObject projectilePrefab = null;

        if (vfxManager != null && vfxManager.vfxConfig.autoAttackProjectile != null)
        {
            projectilePrefab = vfxManager.vfxConfig.autoAttackProjectile;
        }
        else if (unitAI.projectilePrefab != null)
        {
            projectilePrefab = unitAI.projectilePrefab;
        }
        else
        {
            // Fallback: Apply damage directly if no projectile available
            Debug.LogWarning($"No projectile prefab found for {unitAI.unitName}, applying damage directly");
            target.TakeDamage(damage + unitAI.attackDamage);
            yield break;
        }

        GameObject needle = Instantiate(projectilePrefab, startPos, Quaternion.identity);
        float speed = 20f;

        while (needle != null && target != null && target.isAlive && target.currentState != UnitState.Bench)
        {
            Vector3 targetPos = target.transform.position + Vector3.up * 1.2f;
            Vector3 direction = (targetPos - needle.transform.position).normalized;

            needle.transform.position += direction * speed * Time.deltaTime;
            needle.transform.rotation = Quaternion.LookRotation(direction);

            if (Vector3.Distance(needle.transform.position, targetPos) < 0.3f)
            {
                target.TakeDamage(damage + unitAI.attackDamage);

                // Spawn hit effect
                if (vfxManager != null && vfxManager.vfxConfig.autoAttackHitEffect != null)
                {
                    GameObject hitEffect = Instantiate(vfxManager.vfxConfig.autoAttackHitEffect, targetPos, Quaternion.identity);
                    Destroy(hitEffect, 1f);
                }

                Debug.Log($"🎯 {unitAI.unitName} needle hit {target.unitName} for {damage + unitAI.attackDamage} dmg!");
                Destroy(needle);
                yield break;
            }

            yield return null;
        }

        if (needle != null) Destroy(needle);
    }

    // ✅ NEW: Public methods for debugging/UI
    public int GetCurrentNeedleCount() => needlesPerCast;
    public int GetTotalNeedlesThrown() => totalNeedlesThrown;
    public int GetNextStackAt() => ((totalNeedlesThrown / stackingThreshold) + 1) * stackingThreshold;

    // Rest of the methods remain the same...
    private List<UnitAI> FindSmartTargets()
    {
        List<UnitAI> targets = new List<UnitAI>();

        UnitAI currentTarget = unitAI.GetCurrentTarget();
        if (currentTarget != null && currentTarget.isAlive && currentTarget.team != unitAI.team)
        {
            targets.Add(currentTarget);
        }

        if (currentTarget != null)
        {
            UnitAI secondaryTarget = FindClosestEnemyTo(currentTarget);
            if (secondaryTarget != null && secondaryTarget != currentTarget)
            {
                targets.Add(secondaryTarget);
            }
        }

        if (targets.Count == 0)
        {
            targets = FindNearestEnemies(2);
        }
        else if (targets.Count == 1)
        {
            UnitAI nearestToSelf = FindNearestEnemyToSelf(targets[0]);
            if (nearestToSelf != null)
            {
                targets.Add(nearestToSelf);
            }
        }

        return targets;
    }

    private UnitAI FindClosestEnemyTo(UnitAI referenceUnit)
    {
        if (referenceUnit == null) return null;

        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        UnitAI closest = null;
        float closestDistance = float.MaxValue;

        foreach (var unit in allUnits)
        {
            if (unit == referenceUnit || !unit.isAlive || unit.team == unitAI.team)
                continue;

            float distance = Vector3.Distance(referenceUnit.transform.position, unit.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = unit;
            }
        }

        return closest;
    }

    private UnitAI FindNearestEnemyToSelf(UnitAI excludeUnit)
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        UnitAI closest = null;
        float closestDistance = float.MaxValue;

        foreach (var unit in allUnits)
        {
            if (unit == unitAI || unit == excludeUnit || !unit.isAlive || unit.team == unitAI.team)
                continue;

            float distance = Vector3.Distance(unitAI.transform.position, unit.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = unit;
            }
        }

        return closest;
    }

    private void EndCast()
    {
        unitAI.currentMana = 0f;
        if (unitAI.unitUIPrefab != null)
            unitAI.GetComponentInChildren<UnitUI>()?.UpdateMana(unitAI.currentMana);

        unitAI.canAttack = true;
        unitAI.canMove = true;
    }

    private List<UnitAI> FindNearestEnemies(int count)
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        List<UnitAI> enemies = new List<UnitAI>();

        foreach (var u in allUnits)
        {
            if (u != unitAI && u.isAlive && u.team != unitAI.team)
                enemies.Add(u);
        }

        enemies.Sort((a, b) =>
            Vector3.Distance(unitAI.transform.position, a.transform.position)
            .CompareTo(Vector3.Distance(unitAI.transform.position, b.transform.position))
        );

        return enemies.GetRange(0, Mathf.Min(count, enemies.Count));
    }
}
