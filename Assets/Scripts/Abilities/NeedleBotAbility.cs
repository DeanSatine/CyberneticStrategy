using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class NeedleBotAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;
    private CyberneticVFX vfxManager;

    [Header("Ability Stats")]
    public int baseNeedleCount = 3;
    [SerializeField] public int needlesPerCast;
    public float[] damagePerStar = { 100f, 150f, 175f };

    // ✅ Needle stacking progress (persistent across rounds)
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
    public int maxHexRange = 6; // ✅ NEW: Max hex distance for secondary targets

    // ✅ NEW: Projectile completion tracking
    private int activeProjectileCount = 0;
    private bool isCastingAbility = false;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        vfxManager = GetComponent<CyberneticVFX>();

        CalculateNeedleCount();

        // Setup audio source
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
        if (!unitAI.isAlive || isCastingAbility) return;

        isCastingAbility = true;
        unitAI.canAttack = false;
        unitAI.canMove = false;

        if (unitAI.animator != null)
            unitAI.animator.SetTrigger("AbilityTrigger");
        if (target != null && target.currentState == UnitAI.UnitState.Bench)
        {
            EndCast();
            return;
        }

        StartCoroutine(FireNeedlesRoutine());
    }

    private IEnumerator FireNeedlesRoutine()
    {
        yield return new WaitForSeconds(startDelay);

        CalculateNeedleCount();

        // ✅ IMPROVED: Get hex-based targets
        List<UnitAI> targets = FindHexBasedTargets();
        if (targets.Count == 0)
        {
            Debug.Log($"{unitAI.unitName} tried to cast but found no enemies within range!");
            EndCast();
            yield break;
        }

        float damage = damagePerStar[Mathf.Clamp(unitAI.starLevel - 1, 0, damagePerStar.Length - 1)];

        Debug.Log($"🎯 {unitAI.unitName} firing {needlesPerCast} needles at targets: {string.Join(", ", targets.ConvertAll(t => t.unitName))}");

        // ✅ Reset projectile counter
        activeProjectileCount = 0;

        // Fire all needles
        for (int needleIndex = 0; needleIndex < needlesPerCast; needleIndex++)
        {
            UnitAI target = targets[needleIndex % targets.Count];

            if (target != null && target.isAlive && target.currentState != UnitState.Bench)
            {
                PlayNeedleSound();

                Vector3 spawnPos = unitAI.firePoint != null ?
                    unitAI.firePoint.position :
                    transform.position + Vector3.up * 1.5f;

                // Muzzle flash
                if (vfxManager != null && vfxManager.vfxConfig.autoAttackMuzzleFlash != null)
                {
                    GameObject muzzleFlash = Instantiate(vfxManager.vfxConfig.autoAttackMuzzleFlash, spawnPos, Quaternion.identity);
                    Destroy(muzzleFlash, 0.5f);
                }

                // ✅ NEW: Track each projectile
                activeProjectileCount++;
                StartCoroutine(FireNeedleProjectile(spawnPos, target, damage));

                totalNeedlesThrown++;

                if (totalNeedlesThrown % stackingThreshold == 0)
                {
                    Debug.Log($"🎯 {unitAI.unitName} unlocked bonus needle! Next cast will have {baseNeedleCount + (totalNeedlesThrown / stackingThreshold)} needles (total thrown: {totalNeedlesThrown})");
                }
            }

            yield return new WaitForSeconds(throwInterval);
        }

        // ✅ NEW: Wait for all projectiles to complete before ending cast
        StartCoroutine(WaitForAllProjectilesToComplete());
    }

    // ✅ NEW: Wait until all projectiles have hit before allowing attacks
    private IEnumerator WaitForAllProjectilesToComplete()
    {
        Debug.Log($"🎯 Waiting for {activeProjectileCount} projectiles to complete...");

        while (activeProjectileCount > 0)
        {
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
        }

        Debug.Log($"🎯 All projectiles completed! Ending cast.");
        yield return new WaitForSeconds(0.2f); // Small buffer
        EndCast();
    }

    // ✅ NEW: Hex-based targeting system
    private List<UnitAI> FindHexBasedTargets()
    {
        List<UnitAI> targets = new List<UnitAI>();

        // Get NeedleBot's current tile
        HexTile needleBotTile = BoardManager.Instance.GetTileFromWorld(transform.position);
        if (needleBotTile == null)
        {
            Debug.LogWarning($"🎯 {unitAI.unitName} not on a valid hex tile! Using fallback targeting.");
            return FindNearestEnemies(2); // Fallback to distance-based
        }

        // Priority 1: Current target (if within hex range)
        UnitAI currentTarget = unitAI.GetCurrentTarget();
        if (currentTarget != null && currentTarget.isAlive && currentTarget.team != unitAI.team)
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

        // Priority 2: Find all enemies within hex range
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        List<UnitAI> enemiesInRange = new List<UnitAI>();

        foreach (var unit in allUnits)
        {
            if (unit == unitAI || !unit.isAlive || unit.team == unitAI.team || targets.Contains(unit))
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

        // Sort enemies by hex distance (closest first)
        enemiesInRange.Sort((a, b) => {
            HexTile tileA = BoardManager.Instance.GetTileFromWorld(a.transform.position);
            HexTile tileB = BoardManager.Instance.GetTileFromWorld(b.transform.position);
            int distA = CalculateHexDistance(needleBotTile.gridPosition, tileA.gridPosition);
            int distB = CalculateHexDistance(needleBotTile.gridPosition, tileB.gridPosition);
            return distA.CompareTo(distB);
        });

        // Add enemies to targets (up to 2 total targets)
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

    // ✅ NEW: Calculate hex distance using axial coordinates
    private int CalculateHexDistance(Vector2Int a, Vector2Int b)
    {
        // Convert axial to cube coordinates for distance calculation
        // Axial (q, r) -> Cube (x, y, z) where x = q, z = r, y = -x-z
        int x1 = a.x;
        int z1 = a.y;
        int y1 = -x1 - z1;

        int x2 = b.x;
        int z2 = b.y;
        int y2 = -x2 - z2;

        // Hex distance = max(|dx|, |dy|, |dz|)
        return Mathf.Max(Mathf.Abs(x1 - x2), Mathf.Abs(y1 - y2), Mathf.Abs(z1 - z2));
    }

    private void PlayNeedleSound()
    {
        if (needleThrowSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(needleThrowSound, volume);
        }
        else if (vfxManager != null && vfxManager.vfxConfig.autoAttackSound != null)
        {
            audioSource.PlayOneShot(vfxManager.vfxConfig.autoAttackSound, volume);
        }
    }

    // ✅ UPDATED: Notify when projectile completes
    private IEnumerator FireNeedleProjectile(Vector3 startPos, UnitAI target, float damage)
    {
        if (target == null)
        {
            activeProjectileCount--; // Decrement counter
            yield break;
        }

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
            Debug.LogWarning($"No projectile prefab found for {unitAI.unitName}, applying damage directly");
            target.TakeDamage(damage + unitAI.attackDamage);
            activeProjectileCount--; // Decrement counter
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

                if (vfxManager != null && vfxManager.vfxConfig.autoAttackHitEffect != null)
                {
                    GameObject hitEffect = Instantiate(vfxManager.vfxConfig.autoAttackHitEffect, targetPos, Quaternion.identity);
                    Destroy(hitEffect, 1f);
                }

                Debug.Log($"🎯 {unitAI.unitName} needle hit {target.unitName} for {damage + unitAI.attackDamage} dmg!");
                Destroy(needle);
                activeProjectileCount--; // ✅ Decrement when hit
                yield break;
            }

            yield return null;
        }

        if (needle != null) Destroy(needle);
        activeProjectileCount--; // ✅ Decrement when projectile destroyed/missed
    }

    // ✅ UPDATED: Only end cast when all projectiles complete
    private void EndCast()
    {
        isCastingAbility = false;
        unitAI.currentMana = 0f;
        if (unitAI.unitUIPrefab != null)
            unitAI.GetComponentInChildren<UnitUI>()?.UpdateMana(unitAI.currentMana);

        unitAI.canAttack = true;
        unitAI.canMove = true;

        Debug.Log($"🎯 {unitAI.unitName} ability cast complete! Ready to attack again.");
    }

    // Public methods for debugging/UI
    public int GetCurrentNeedleCount() => needlesPerCast;
    public int GetTotalNeedlesThrown() => totalNeedlesThrown;
    public int GetNextStackAt() => ((totalNeedlesThrown / stackingThreshold) + 1) * stackingThreshold;

    // Fallback method for non-hex targeting
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
