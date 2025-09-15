using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class HaymakerAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;
    private int soulCount = 0;

    [Header("Ability Stats")]
    public float[] slashDamage = { 30f, 40f, 500f };
    public float[] slamDamage = { 125f, 250f, 999f };
    public float[] temporaryArmor = { 160f, 180f, 190f }; // 80/90/95% damage reduction

    [Header("Ability Mechanics")]
    public float slashDuration = 3f;                    // Total slashing time
    public float slashesPerAttackSpeed = 10f;           // 1 slash per 0.10 attack speed (10 = 1.0 AS)
    public float dashSpeed = 8f;                        // Speed of dash movement
    public float slashAnimationSpeedMultiplier = 5f;    // ✅ INCREASED: Much faster slashing (was 3f)

    [Header("Passive Clone")]
    public GameObject clonePrefab;
    private GameObject cloneInstance;

    [Header("VFX")]
    public GameObject slashVFX;        // VFX for each slash
    public GameObject slamVFX;         // VFX for clone slam
    public GameObject dashStartVFX;    // VFX when starting dash
    public GameObject dashEndVFX;      // VFX when arriving at target

    // Ability state tracking
    private bool isPerformingAbility = false;
    private Vector3 originalPosition;
    private float originalArmor;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        unitAI.OnStateChanged += HandleStateChanged;
        UnitAI.OnAnyUnitDeath += OnUnitDeath;

        // Store original armor
        originalArmor = unitAI.armor;
    }

    private void OnDestroy()
    {
        unitAI.OnStateChanged -= HandleStateChanged;
        UnitAI.OnAnyUnitDeath -= OnUnitDeath;
    }

    private void HandleStateChanged(UnitState state)
    {
        Debug.Log($"[HaymakerAbility] Haymaker state → {state}");

        if (state == UnitState.BoardIdle)
        {
            if (cloneInstance == null)
                SpawnClone();
        }
        else if (state == UnitState.Bench || !unitAI.isAlive)
        {
            if (cloneInstance != null)
                DestroyClone();

            // ✅ FIX: Stop any ongoing ability when state changes
            if (isPerformingAbility)
            {
                StopAllCoroutines();
                ResetAbilityState();
            }
        }
    }

    // ✅ NEW: Reset ability state method
    private void ResetAbilityState()
    {
        isPerformingAbility = false;

        // Reset animation speed
        if (unitAI.animator)
        {
            unitAI.animator.speed = 1f;
        }

        // Reset armor
        unitAI.armor = originalArmor;

        Debug.Log("[HaymakerAbility] Ability state reset");
    }

    private void SpawnClone()
    {
        HexTile targetTile = FindClosestEmptyHexTile();
        if (targetTile == null)
        {
            Debug.LogWarning("[HaymakerAbility] No empty hex tile found for clone!");
            return;
        }

        Vector3 spawnPos = targetTile.transform.position;
        spawnPos.y = 0.6f;

        cloneInstance = Instantiate(clonePrefab, spawnPos, Quaternion.identity);
        cloneInstance.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        cloneInstance.name = $"{unitAI.unitName} Clone";

        var cloneAI = cloneInstance.GetComponent<UnitAI>();
        cloneAI.AssignToTile(targetTile);

        // Scale stats
        cloneAI.maxHealth = unitAI.maxHealth * 0.25f;
        cloneAI.currentHealth = cloneAI.maxHealth;
        cloneAI.attackDamage = unitAI.attackDamage * 0.25f;
        cloneAI.starLevel = unitAI.starLevel;
        cloneAI.team = unitAI.team;
        cloneAI.teamID = unitAI.teamID;
        cloneAI.currentMana = 0f;
        cloneAI.isAlive = true;

        cloneAI.SetState(UnitState.BoardIdle);

        // Prevent infinite cloning
        var selfAbility = cloneInstance.GetComponent<HaymakerAbility>();
        if (selfAbility) Destroy(selfAbility);

        // Disable traits
        foreach (var mb in cloneInstance.GetComponents<MonoBehaviour>())
        {
            if (mb.GetType().Name.Contains("Trait"))
                mb.enabled = false;
        }

        GameManager.Instance.RegisterUnit(cloneAI, cloneAI.team == Team.Player);
        Debug.Log($"[HaymakerAbility] Clone spawned at {targetTile.gridPosition}");
    }

    private void DestroyClone()
    {
        if (cloneInstance != null)
        {
            var cloneAI = cloneInstance.GetComponent<UnitAI>();

            if (cloneAI != null)
            {
                cloneAI.ClearTile();
                GameManager.Instance.UnregisterUnit(cloneAI);
            }

            Destroy(cloneInstance);
            cloneInstance = null;
            Debug.Log("[HaymakerAbility] Clone destroyed.");
        }
    }

    private HexTile FindClosestEmptyHexTile()
    {
        if (BoardManager.Instance == null) return null;

        List<HexTile> playerTiles = BoardManager.Instance.GetPlayerTiles();
        if (playerTiles == null || playerTiles.Count == 0) return null;

        List<HexTile> emptyTiles = new List<HexTile>();
        foreach (var tile in playerTiles)
        {
            if (tile.occupyingUnit == null)
                emptyTiles.Add(tile);
        }

        if (emptyTiles.Count == 0) return null;

        emptyTiles.Sort((a, b) =>
            Vector3.Distance(transform.position, a.transform.position)
            .CompareTo(Vector3.Distance(transform.position, b.transform.position))
        );

        return emptyTiles[0];
    }

    // Passive: absorb souls when units die
    private void OnUnitDeath(UnitAI deadUnit)
    {
        if (!unitAI.isAlive) return;
        if (deadUnit.team == unitAI.team) return;

        soulCount++;
        if (soulCount % 5 == 0 && cloneInstance != null)
        {
            var cloneAI = cloneInstance.GetComponent<UnitAI>();
            cloneAI.maxHealth *= 1.01f;
            cloneAI.attackDamage *= 1.01f;
            Debug.Log($"💀 Clone empowered! Souls: {soulCount}");
        }
    }

    // Active ability
    public void Cast(UnitAI target)
    {
        // ✅ FIX: Check if we're in the right phase and state
        if (isPerformingAbility)
        {
            Debug.Log("[HaymakerAbility] Already performing ability, ignoring cast");
            return;
        }

        // ✅ FIX: Only allow ability during combat phase
        if (StageManager.Instance != null && StageManager.Instance.currentPhase != StageManager.GamePhase.Combat)
        {
            Debug.Log("[HaymakerAbility] Cannot cast ability outside of combat phase");
            return;
        }

        // ✅ FIX: Only allow ability when in combat state
        if (unitAI.currentState != UnitState.Combat && unitAI.currentState != UnitState.BoardIdle)
        {
            Debug.Log($"[HaymakerAbility] Cannot cast ability in state: {unitAI.currentState}");
            return;
        }

        StartCoroutine(PerformFuryOfSlashes());
    }

    private IEnumerator PerformFuryOfSlashes()
    {
        isPerformingAbility = true;
        originalPosition = transform.position;

        Debug.Log($"⚡ [HaymakerAbility] Starting Fury of Slashes!");

        // ✅ PHASE 1: Dash to enemy clump
        Vector3 targetClumpPosition = FindBestClumpPosition(6f);
        if (targetClumpPosition == Vector3.zero)
        {
            // No enemies found, end ability
            Debug.Log("[HaymakerAbility] No enemies found, ending ability");
            isPerformingAbility = false;
            yield break;
        }

        // Spawn dash start VFX
        if (dashStartVFX != null)
        {
            var startVFX = Instantiate(dashStartVFX, transform.position, Quaternion.identity);
            Destroy(startVFX, 2f);
        }

        Debug.Log($"🏃 Phase 1: Dashing to clump at {targetClumpPosition}");

        // Dash to target position
        yield return StartCoroutine(DashToPosition(targetClumpPosition));

        // Spawn dash end VFX
        if (dashEndVFX != null)
        {
            var endVFX = Instantiate(dashEndVFX, transform.position, Quaternion.identity);
            Destroy(endVFX, 2f);
        }

        // ✅ PHASE 2: Fury of Slashes (3 seconds)
        Debug.Log("⚔️ Phase 2: Unleashing Fury of Slashes!");

        // Apply massive armor for damage reduction
        int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, temporaryArmor.Length - 1);
        unitAI.armor = temporaryArmor[starIndex];
        Debug.Log($"🛡️ Temporary armor active: {temporaryArmor[starIndex]} ({80 + (starIndex * 10)}% damage reduction)");

        // Calculate number of slashes based on attack speed
        float attackSpeed = unitAI.attackSpeed;
        int totalSlashes = Mathf.RoundToInt(attackSpeed * slashesPerAttackSpeed * slashDuration);
        float timeBetweenSlashes = slashDuration / totalSlashes;

        Debug.Log($"🗡️ Will perform {totalSlashes} slashes over {slashDuration} seconds (1 every {timeBetweenSlashes:F2}s)");

        // Much faster slashing animation
        if (unitAI.animator)
        {
            unitAI.animator.speed = slashAnimationSpeedMultiplier;
        }

        // Perform slashes
        for (int i = 0; i < totalSlashes; i++)
        {
            // Check if ability should be interrupted
            if (!isPerformingAbility || !unitAI.isAlive)
            {
                Debug.Log("[HaymakerAbility] Ability interrupted, stopping slashes");
                break;
            }

            // ✅ FIX: Check phase during slashing too
            if (StageManager.Instance != null && StageManager.Instance.currentPhase != StageManager.GamePhase.Combat)
            {
                Debug.Log("[HaymakerAbility] Phase changed during slashing, stopping ability");
                break;
            }

            // Find closest enemy for this slash
            UnitAI slashTarget = FindClosestEnemy(2.5f);

            if (slashTarget != null)
            {
                // Trigger attack animation
                if (unitAI.animator)
                {
                    unitAI.animator.SetTrigger("AttackTrigger");
                }

                // Apply slash damage
                float slashDmg = slashDamage[Mathf.Clamp(unitAI.starLevel - 1, 0, slashDamage.Length - 1)];
                slashTarget.TakeDamage(slashDmg);

                // Spawn slash VFX around Haymaker's position
                if (slashVFX != null)
                {
                    // Create VFX in a circle around Haymaker
                    float angle = (float)i / totalSlashes * 360f; // Distribute around circle
                    Vector3 offset = new Vector3(
                        Mathf.Cos(angle * Mathf.Deg2Rad) * 1f, // 1 unit radius
                        0.5f, // Slightly above ground
                        Mathf.Sin(angle * Mathf.Deg2Rad) * 1f
                    );
                    Vector3 vfxPos = transform.position + offset;

                    var slashEffect = Instantiate(slashVFX, vfxPos, Quaternion.LookRotation(offset));
                    Destroy(slashEffect, 1f);
                }

                Debug.Log($"💥 Slash {i + 1}/{totalSlashes}: {slashDmg} damage to {slashTarget.unitName}");
            }

            yield return new WaitForSeconds(timeBetweenSlashes);
        }

        // Reset animation speed and armor
        if (unitAI.animator)
        {
            unitAI.animator.speed = 1f;
        }
        unitAI.armor = originalArmor;

        // ✅ PHASE 3: Clone slam on final target (with retargeting)
        Debug.Log("💥 Phase 3: Clone slam!");

        // ✅ FIX: Check if we're still in combat before clone slam
        if (StageManager.Instance != null && StageManager.Instance.currentPhase == StageManager.GamePhase.Combat)
        {
            yield return StartCoroutine(PerformCloneSlamWithRetargeting());
        }
        else
        {
            Debug.Log("[HaymakerAbility] Phase changed, skipping clone slam");
        }

        // ✅ PHASE 4: Dash back to original position (ONLY if still in combat)
        if (StageManager.Instance != null && StageManager.Instance.currentPhase == StageManager.GamePhase.Combat &&
            unitAI.isAlive && isPerformingAbility)
        {
            Debug.Log("🏃 Phase 4: Dashing back to original position");
            yield return StartCoroutine(DashToPosition(originalPosition));
        }
        else
        {
            Debug.Log("[HaymakerAbility] Skipping return dash - phase changed or unit state invalid");
        }

        isPerformingAbility = false;
        Debug.Log("✅ Fury of Slashes complete!");
    }

    // ✅ NEW: Clone slam with automatic retargeting
    private IEnumerator PerformCloneSlamWithRetargeting()
    {
        if (cloneInstance == null)
        {
            Debug.Log("[HaymakerAbility] No clone available for slam");
            yield break;
        }

        // ✅ FIX: Find best target for slam (retarget if needed)
        UnitAI slamTarget = FindClosestEnemy(5f); // Larger search radius for slam

        if (slamTarget == null)
        {
            Debug.Log("[HaymakerAbility] No valid target found for clone slam");
            yield break;
        }

        Debug.Log($"🎯 Clone targeting {slamTarget.unitName} for slam");

        Vector3 cloneStartPos = cloneInstance.transform.position;
        Vector3 slamPosition = slamTarget.transform.position;

        // Clone jumps up and slams down
        float slamDuration = 0.8f;
        float jumpHeight = 3f;

        float elapsed = 0f;
        while (elapsed < slamDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slamDuration;

            // ✅ Check if target is still valid during slam
            if (slamTarget == null || !slamTarget.isAlive)
            {
                // ✅ Retarget mid-slam if needed
                UnitAI newTarget = FindClosestEnemy(5f);
                if (newTarget != null)
                {
                    slamTarget = newTarget;
                    slamPosition = slamTarget.transform.position;
                    Debug.Log($"🔄 Clone retargeted to {slamTarget.unitName} mid-slam");
                }
            }

            // Arc movement for clone
            Vector3 pos = Vector3.Lerp(cloneStartPos, slamPosition, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;
            cloneInstance.transform.position = pos;

            yield return null;
        }

        // ✅ Final target validation before damage
        if (slamTarget != null && slamTarget.isAlive)
        {
            // Apply slam damage
            float slamDmg = slamDamage[Mathf.Clamp(unitAI.starLevel - 1, 0, slamDamage.Length - 1)];
            slamTarget.TakeDamage(slamDmg);

            // Spawn slam VFX
            if (slamVFX != null)
            {
                var slamEffect = Instantiate(slamVFX, slamPosition, Quaternion.identity);
                Destroy(slamEffect, 2f);
            }

            Debug.Log($"🌪️ Clone slam: {slamDmg} damage to {slamTarget.unitName}");
        }
        else
        {
            Debug.Log("[HaymakerAbility] Clone slam target became invalid, no damage applied");

            // Still spawn VFX at slam position for visual feedback
            if (slamVFX != null)
            {
                var slamEffect = Instantiate(slamVFX, slamPosition, Quaternion.identity);
                Destroy(slamEffect, 2f);
            }
        }

        // Return clone to original position
        yield return StartCoroutine(MoveCloneToPosition(cloneStartPos));
    }

    private IEnumerator DashToPosition(Vector3 targetPosition)
    {
        Vector3 startPos = transform.position;
        float distance = Vector3.Distance(startPos, targetPosition);
        float dashTime = distance / dashSpeed;

        float elapsed = 0f;
        while (elapsed < dashTime)
        {
            // ✅ Enhanced interruption checks
            if (!isPerformingAbility || !unitAI.isAlive)
            {
                Debug.Log("[HaymakerAbility] Dash interrupted - ability stopped or unit died");
                yield break;
            }

            // ✅ FIX: Check phase during dash
            if (StageManager.Instance != null && StageManager.Instance.currentPhase != StageManager.GamePhase.Combat)
            {
                Debug.Log("[HaymakerAbility] Dash interrupted - phase changed to non-combat");
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / dashTime;

            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
    }


    private IEnumerator PerformCloneSlam(UnitAI target)
    {
        if (cloneInstance == null) yield break;

        Vector3 cloneStartPos = cloneInstance.transform.position;
        Vector3 slamPosition = target.transform.position;

        // Clone jumps up and slams down
        float slamDuration = 0.8f;
        float jumpHeight = 3f;

        float elapsed = 0f;
        while (elapsed < slamDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slamDuration;

            // Arc movement for clone
            Vector3 pos = Vector3.Lerp(cloneStartPos, slamPosition, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;
            cloneInstance.transform.position = pos;

            yield return null;
        }

        // Apply slam damage
        float slamDmg = slamDamage[Mathf.Clamp(unitAI.starLevel - 1, 0, slamDamage.Length - 1)];
        target.TakeDamage(slamDmg);

        // Spawn slam VFX
        if (slamVFX != null)
        {
            var slamEffect = Instantiate(slamVFX, slamPosition, Quaternion.identity);
            Destroy(slamEffect, 2f);
        }

        Debug.Log($"🌪️ Clone slam: {slamDmg} damage to {target.unitName}");

        // Return clone to original position
        yield return StartCoroutine(MoveCloneToPosition(cloneStartPos));
    }

    private IEnumerator MoveCloneToPosition(Vector3 targetPos)
    {
        if (cloneInstance == null) yield break;

        Vector3 startPos = cloneInstance.transform.position;
        float moveTime = 0.5f;

        float elapsed = 0f;
        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveTime;

            cloneInstance.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        cloneInstance.transform.position = targetPos;
    }

    private Vector3 FindBestClumpPosition(float searchRadius)
    {
        List<UnitAI> enemies = FindEnemiesInRadius(searchRadius);
        if (enemies.Count == 0) return Vector3.zero;

        // Find enemy with most neighbors (clump center)
        UnitAI bestCenter = enemies[0];
        int bestCount = 0;

        foreach (var enemy in enemies)
        {
            int nearbyCount = 0;
            foreach (var other in enemies)
            {
                if (other == enemy) continue;
                if (Vector3.Distance(enemy.transform.position, other.transform.position) <= 2.5f)
                    nearbyCount++;
            }

            if (nearbyCount > bestCount)
            {
                bestCount = nearbyCount;
                bestCenter = enemy;
            }
        }

        return bestCenter.transform.position;
    }

    private UnitAI FindClosestEnemy(float maxDistance)
    {
        List<UnitAI> enemies = FindEnemiesInRadius(maxDistance);
        if (enemies.Count == 0) return null;

        UnitAI closest = enemies[0];
        float minDistance = Vector3.Distance(transform.position, closest.transform.position);

        foreach (var enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = enemy;
            }
        }

        return closest;
    }

    private List<UnitAI> FindEnemiesInRadius(float radius, Vector3? center = null)
    {
        if (center == null) center = transform.position;

        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        List<UnitAI> enemies = new List<UnitAI>();

        foreach (var unit in allUnits)
        {
            if (unit == unitAI || !unit.isAlive || unit.team == unitAI.team || unit.currentState == UnitState.Bench)
                continue;

            if (Vector3.Distance(center.Value, unit.transform.position) <= radius)
                enemies.Add(unit);
        }

        return enemies;
    }
}
