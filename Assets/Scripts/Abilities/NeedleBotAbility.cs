using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class NeedleBotAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;
    private CyberneticVFX vfxManager;
    private Coroutine castRoutine;
    private readonly List<Coroutine> projectileCoroutines = new();

    [Header("Ability Stats")]
    public int baseNeedleCount = 3;
    [SerializeField] public int needlesPerCast;
    public float[] damagePerStar = { 100f, 150f, 175f };

    [Header("Needle Stacking Progress")]
    [SerializeField] private int totalNeedlesThrown = 0;
    [SerializeField] private int stackingThreshold = 10;

    [Header("Audio")]
    public AudioClip needleThrowSound;
    [Range(0f, 1f)] public float volume = 0.7f;
    private AudioSource audioSource;

    [Header("Timings")]
    public float startDelay = 0.25f;
    public float throwInterval = 0.1f;
    public float abilityDuration = 1.5f;

    [Header("Targeting")]
    public int maxHexRange = 6;

    private int activeProjectileCount = 0;
    private bool isCastingAbility = false;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        vfxManager = GetComponent<CyberneticVFX>();

        CalculateNeedleCount();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.8f;
        }
    }

    private void CalculateNeedleCount()
    {
        int bonusNeedles = totalNeedlesThrown / stackingThreshold;
        needlesPerCast = baseNeedleCount + bonusNeedles;

        Debug.Log($"🎯 {unitAI.unitName} calculated needles: {needlesPerCast} (base: {baseNeedleCount} + bonus: {bonusNeedles} from {totalNeedlesThrown} total thrown)");
    }

    public void Cast(UnitAI target)
    {
        if (!unitAI.isAlive)
        {
            Debug.Log($"🎯 {unitAI.unitName} cannot cast - unit is dead");
            return;
        }

        if (isCastingAbility)
        {
            Debug.Log($"🎯 {unitAI.unitName} already casting ability, ignoring new cast request");
            return;
        }

        isCastingAbility = true;
        unitAI.isCastingAbility = true;
        unitAI.canAttack = false;
        unitAI.canMove = false;

        if (unitAI.animator) unitAI.animator.SetTrigger("AbilityTrigger");

        if (target != null && target.currentState == UnitAI.UnitState.Bench)
        {
            Debug.Log($"🎯 {unitAI.unitName} target is on bench, ending cast");
            EndCast();
            return;
        }

        castRoutine = StartCoroutine(FireNeedlesRoutine());
    }

    public void OnRoundEnd()
    {
        if (isCastingAbility)
        {
            Debug.Log($"🎯 Round ended while {unitAI.unitName} was casting");
            ForceEndCast();
        }
    }

    private void OnDisable()
    {
        ForceEndCast();
    }

    private void ForceEndCast()
    {
        StopAllCoroutines();
        isCastingAbility = false;
        activeProjectileCount = 0;

        if (castRoutine != null)
        {
            castRoutine = null;
        }

        projectileCoroutines.Clear();

        if (unitAI != null)
        {
            unitAI.isCastingAbility = false;
            unitAI.canAttack = true;
            unitAI.canMove = true;
        }

        Debug.Log($"🎯 Force ended cast for {unitAI?.unitName}");
    }

    private IEnumerator FireNeedlesRoutine()
    {
        if (!isCastingAbility || unitAI == null || !unitAI.isAlive)
        {
            Debug.Log($"🎯 FireNeedlesRoutine early exit - isCasting: {isCastingAbility}, unitAI: {unitAI != null}, isAlive: {unitAI?.isAlive}");
            EndCast();
            yield break;
        }

        yield return new WaitForSeconds(startDelay);

        CalculateNeedleCount();

        List<UnitAI> targets = FindHexBasedTargets();
        if (targets.Count == 0)
        {
            Debug.Log($"🎯 {unitAI.unitName} tried to cast but found no enemies within range!");
            EndCast();
            yield break;
        }

        float damage = damagePerStar[Mathf.Clamp(unitAI.starLevel - 1, 0, damagePerStar.Length - 1)] + (unitAI.attackDamage * 0.6f);

        Debug.Log($"🎯 {unitAI.unitName} firing {needlesPerCast} needles at targets: {string.Join(", ", targets.ConvertAll(t => t.unitName))}");

        activeProjectileCount = 0;

        for (int needleIndex = 0; needleIndex < needlesPerCast; needleIndex++)
        {
            if (!unitAI.isAlive)
            {
                Debug.Log($"🎯 {unitAI.unitName} died mid-cast, stopping needles");
                break;
            }

            targets.RemoveAll(t => t == null || !t.isAlive || t.currentState == UnitState.Bench);

            if (targets.Count == 0)
            {
                Debug.Log($"🎯 {unitAI.unitName} retargeting - all targets died!");
                targets = FindHexBasedTargets();

                if (targets.Count == 0)
                {
                    Debug.Log($"🎯 {unitAI.unitName} no valid retarget found, ending ability early.");
                    break;
                }

                Debug.Log($"🎯 {unitAI.unitName} retargeted to: {string.Join(", ", targets.ConvertAll(t => t.unitName))}");
            }

            UnitAI target = targets[needleIndex % targets.Count];

            if (target != null && target.isAlive && target.currentState != UnitState.Bench)
            {
                PlayNeedleSound();

                Vector3 spawnPos = unitAI.firePoint != null ?
                    unitAI.firePoint.position :
                    transform.position + Vector3.up * 1.5f;

                if (vfxManager != null && vfxManager.vfxConfig != null && vfxManager.vfxConfig.autoAttackMuzzleFlash != null)
                {
                    GameObject muzzleFlash = Instantiate(vfxManager.vfxConfig.autoAttackMuzzleFlash, spawnPos, Quaternion.identity);
                    Destroy(muzzleFlash, 0.5f);
                }

                activeProjectileCount++;
                Coroutine proj = StartCoroutine(FireNeedleProjectile(spawnPos, target, damage));
                projectileCoroutines.Add(proj);

                totalNeedlesThrown++;

                if (totalNeedlesThrown % stackingThreshold == 0)
                {
                    Debug.Log($"🎯 {unitAI.unitName} unlocked bonus needle! Next cast will have {baseNeedleCount + (totalNeedlesThrown / stackingThreshold)} needles (total thrown: {totalNeedlesThrown})");
                }
            }

            yield return new WaitForSeconds(throwInterval);
        }

        StartCoroutine(WaitForAllProjectilesToComplete());
    }

    private IEnumerator WaitForAllProjectilesToComplete()
    {
        Debug.Log($"🎯 Waiting for {activeProjectileCount} projectiles to complete...");

        float timeout = 5f;
        float elapsed = 0f;

        while (activeProjectileCount > 0 && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (activeProjectileCount > 0)
        {
            Debug.LogWarning($"⚠️ TIMEOUT! {activeProjectileCount} projectiles stuck, forcing ability end");
            activeProjectileCount = 0;
        }

        Debug.Log($"🎯 All projectiles completed! Ending cast.");
        yield return new WaitForSeconds(0.2f);
        EndCast();
    }

    private List<UnitAI> FindHexBasedTargets()
    {
        List<UnitAI> targets = new List<UnitAI>();

        HexTile needleBotTile = BoardManager.Instance.GetTileFromWorld(transform.position);
        if (needleBotTile == null)
        {
            Debug.LogWarning($"🎯 {unitAI.unitName} not on a valid hex tile! Using fallback targeting.");
            return FindNearestEnemies(2);
        }

        UnitAI currentTarget = unitAI.GetCurrentTarget();
        if (currentTarget != null && currentTarget.isAlive && currentTarget.team != unitAI.team && currentTarget.currentState != UnitState.Bench)
        {
            HexTile targetTile = BoardManager.Instance.GetTileFromWorld(currentTarget.transform.position);
            if (targetTile != null)
            {
                int hexDistance = CalculateHexDistance(needleBotTile.gridPosition, targetTile.gridPosition);
                if (hexDistance <= maxHexRange)
                {
                    targets.Add(currentTarget);
                    Debug.Log($"🎯 Primary target: {currentTarget.unitName} (hex distance: {hexDistance})");
                }
                else
                {
                    Debug.Log($"🎯 Primary target {currentTarget.unitName} too far (hex distance: {hexDistance} > {maxHexRange})");
                }
            }
        }

        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        List<UnitAI> enemiesInRange = new List<UnitAI>();

        foreach (var unit in allUnits)
        {
            if (unit == unitAI || !unit.isAlive || unit.team == unitAI.team || unit.currentState == UnitState.Bench || targets.Contains(unit))
                continue;

            HexTile unitTile = BoardManager.Instance.GetTileFromWorld(unit.transform.position);
            if (unitTile != null)
            {
                int hexDistance = CalculateHexDistance(needleBotTile.gridPosition, unitTile.gridPosition);
                if (hexDistance <= maxHexRange)
                {
                    enemiesInRange.Add(unit);
                    Debug.Log($"🎯 Enemy in range: {unit.unitName} (hex distance: {hexDistance})");
                }
            }
        }

        enemiesInRange.Sort((a, b) => {
            HexTile tileA = BoardManager.Instance.GetTileFromWorld(a.transform.position);
            HexTile tileB = BoardManager.Instance.GetTileFromWorld(b.transform.position);
            int distA = CalculateHexDistance(needleBotTile.gridPosition, tileA.gridPosition);
            int distB = CalculateHexDistance(needleBotTile.gridPosition, tileB.gridPosition);
            return distA.CompareTo(distB);
        });

        foreach (var enemy in enemiesInRange)
        {
            if (targets.Count >= 2) break;
            if (!targets.Contains(enemy))
            {
                targets.Add(enemy);
            }
        }

        Debug.Log($"🎯 Final targets: {targets.Count} enemies within {maxHexRange} hex range");
        return targets;
    }

    private int CalculateHexDistance(Vector2Int a, Vector2Int b)
    {
        int x1 = a.x;
        int z1 = a.y;
        int y1 = -x1 - z1;

        int x2 = b.x;
        int z2 = b.y;
        int y2 = -x2 - z2;

        return Mathf.Max(Mathf.Abs(x1 - x2), Mathf.Abs(y1 - y2), Mathf.Abs(z1 - z2));
    }

    private void PlayNeedleSound()
    {
        if (needleThrowSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(needleThrowSound, volume);
        }
        else if (vfxManager != null && vfxManager.vfxConfig != null && vfxManager.vfxConfig.autoAttackSound != null)
        {
            audioSource.PlayOneShot(vfxManager.vfxConfig.autoAttackSound, volume);
        }
    }

    private IEnumerator FireNeedleProjectile(Vector3 startPos, UnitAI target, float damage)
    {
        if (target == null || !target.isAlive)
        {
            Debug.Log($"🎯 Projectile target invalid at fire time, decrementing counter");
            activeProjectileCount--;
            yield break;
        }

        GameObject projectilePrefab = null;

        if (vfxManager != null && vfxManager.vfxConfig != null && vfxManager.vfxConfig.abilityProjectile != null)
        {
            projectilePrefab = vfxManager.vfxConfig.abilityProjectile;
        }
        else if (vfxManager != null && vfxManager.vfxConfig != null && vfxManager.vfxConfig.autoAttackProjectile != null)
        {
            projectilePrefab = vfxManager.vfxConfig.autoAttackProjectile;
        }
        else if (unitAI.projectilePrefab != null)
        {
            projectilePrefab = unitAI.projectilePrefab;
            Debug.LogWarning($"⚠️ {unitAI.unitName} missing VFX config projectile - using unitAI.projectilePrefab: {projectilePrefab.name}");
        }
        else
        {
            Debug.LogError($"❌ No projectile prefab found for {unitAI.unitName}!");
            if (target != null && target.isAlive)
            {
                target.TakeDamage(damage + unitAI.attackDamage);
                Debug.Log($"🎯 {unitAI.unitName} applied {damage + unitAI.attackDamage} damage directly to {target.unitName} (no projectile visual)");
            }
            activeProjectileCount--;
            yield break;
        }

        GameObject needle = Instantiate(projectilePrefab, startPos, Quaternion.identity);
        float speed = 20f;
        float maxTravelTime = 3f;
        float travelTime = 0f;

        while (needle != null && travelTime < maxTravelTime)
        {
            if (target == null || !target.isAlive || target.currentState == UnitState.Bench)
            {
                Debug.Log($"🎯 Target became invalid mid-flight, destroying projectile");
                if (needle != null) Destroy(needle);
                activeProjectileCount--;
                yield break;
            }

            Vector3 targetPos = target.transform.position + Vector3.up * 1.2f;
            Vector3 direction = (targetPos - needle.transform.position).normalized;

            needle.transform.position += direction * speed * Time.deltaTime;
            needle.transform.rotation = Quaternion.LookRotation(direction);

            if (Vector3.Distance(needle.transform.position, targetPos) < 0.3f)
            {
                target.TakeDamage(damage + unitAI.attackDamage);

                if (vfxManager != null && vfxManager.vfxConfig != null && vfxManager.vfxConfig.autoAttackHitEffect != null)
                {
                    GameObject hitEffect = Instantiate(vfxManager.vfxConfig.autoAttackHitEffect, targetPos, Quaternion.identity);
                    Destroy(hitEffect, 1f);
                }

                Debug.Log($"🎯 {unitAI.unitName} needle hit {target.unitName} for {damage + unitAI.attackDamage} dmg!");
                Destroy(needle);
                activeProjectileCount--;
                yield break;
            }

            travelTime += Time.deltaTime;
            yield return null;
        }

        if (needle != null)
        {
            Debug.LogWarning($"⚠️ Needle timeout after {travelTime}s, destroying");
            Destroy(needle);
        }
        activeProjectileCount--;
    }

    private void EndCast()
    {
        if (!isCastingAbility)
        {
            Debug.Log($"🎯 EndCast called but already ended for {unitAI.unitName}");
            return;
        }

        if (castRoutine != null)
        {
            StopCoroutine(castRoutine);
            castRoutine = null;
        }

        foreach (var proj in projectileCoroutines)
        {
            if (proj != null)
                StopCoroutine(proj);
        }
        projectileCoroutines.Clear();

        isCastingAbility = false;
        unitAI.isCastingAbility = false;
        activeProjectileCount = 0;

        unitAI.currentMana = 0f;
        if (unitAI.ui != null)
        {
            unitAI.ui.UpdateMana(unitAI.currentMana);
        }

        unitAI.canAttack = true;
        unitAI.canMove = true;

        Debug.Log($"🎯 {unitAI.unitName} ability cast complete! Ready to attack again.");
    }

    public int GetCurrentNeedleCount() => needlesPerCast;
    public int GetTotalNeedlesThrown() => totalNeedlesThrown;
    public int GetNextStackAt() => ((totalNeedlesThrown / stackingThreshold) + 1) * stackingThreshold;

    private List<UnitAI> FindNearestEnemies(int count)
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        List<UnitAI> enemies = new List<UnitAI>();

        foreach (var u in allUnits)
        {
            if (u != unitAI && u.isAlive && u.team != unitAI.team && u.currentState != UnitState.Bench)
                enemies.Add(u);
        }

        enemies.Sort((a, b) =>
            Vector3.Distance(unitAI.transform.position, a.transform.position)
            .CompareTo(Vector3.Distance(unitAI.transform.position, b.transform.position))
        );

        return enemies.GetRange(0, Mathf.Min(count, enemies.Count));
    }
}
