using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class HaymakerAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;
    private int soulCount = 0;

    [Header("Ability Stats")]
    public float[] stabDamage = { 125f, 250f, 999f };
    public float[] slamDamage = { 200f, 250f, 400f };

    [Header("Passive Clone")]
    public GameObject clonePrefab;   // assign in Inspector
    private GameObject cloneInstance;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        unitAI.OnStateChanged += HandleStateChanged;
        UnitAI.OnAnyUnitDeath += OnUnitDeath;
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
        }
    }

    private void SpawnClone()
    {
        // ✅ Find the actual hex tile to spawn on
        HexTile targetTile = FindClosestEmptyHexTile();
        if (targetTile == null)
        {
            Debug.LogWarning("[HaymakerAbility] No empty hex tile found for clone!");
            return;
        }

        // ✅ Spawn at the hex tile position
        Vector3 spawnPos = targetTile.transform.position;
        spawnPos.y = 0.6f; // Keep clone above ground like other units

        cloneInstance = Instantiate(clonePrefab, spawnPos, Quaternion.identity);

        // ✅ Force facing direction
        cloneInstance.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        cloneInstance.name = $"{unitAI.unitName} Clone";

        var cloneAI = cloneInstance.GetComponent<UnitAI>();

        // ✅ CRITICAL: Assign clone to the hex tile
        cloneAI.AssignToTile(targetTile);

        // scale stats
        cloneAI.maxHealth = unitAI.maxHealth * 0.25f;
        cloneAI.currentHealth = cloneAI.maxHealth;
        cloneAI.attackDamage = unitAI.attackDamage * 0.25f;
        cloneAI.starLevel = unitAI.starLevel;
        cloneAI.team = unitAI.team;
        cloneAI.teamID = unitAI.teamID;
        cloneAI.currentMana = 0f;
        cloneAI.isAlive = true;

        // ✅ Put the clone directly into BoardIdle so it participates in combat
        cloneAI.SetState(UnitState.BoardIdle);

        // prevent infinite cloning
        var selfAbility = cloneInstance.GetComponent<HaymakerAbility>();
        if (selfAbility) Destroy(selfAbility);

        // disable traits so clone doesn't affect synergies
        foreach (var mb in cloneInstance.GetComponents<MonoBehaviour>())
        {
            if (mb.GetType().Name.Contains("Trait"))
                mb.enabled = false;
        }

        // ✅ Register with GameManager AFTER tile assignment
        GameManager.Instance.RegisterUnit(cloneAI, cloneAI.team == Team.Player);

        Debug.Log($"[HaymakerAbility] Clone spawned and assigned to hex tile {targetTile.gridPosition}");
    }

    private void DestroyClone()
    {
        if (cloneInstance != null)
        {
            var cloneAI = cloneInstance.GetComponent<UnitAI>();
            
            // ✅ Properly clean up tile assignment
            if (cloneAI != null)
            {
                cloneAI.ClearTile();
                GameManager.Instance.UnregisterUnit(cloneAI);
            }
            
            Destroy(cloneInstance);
            cloneInstance = null;
            Debug.Log("[HaymakerAbility] Clone destroyed and tile cleared.");
        }
    }

    // ✅ NEW: Find an actual empty hex tile near the Haymaker
    private HexTile FindClosestEmptyHexTile()
    {
        if (BoardManager.Instance == null)
        {
            Debug.LogError("[HaymakerAbility] BoardManager not found!");
            return null;
        }

        // ✅ Get player tiles (where the clone should spawn)
        List<HexTile> playerTiles = BoardManager.Instance.GetPlayerTiles();
        if (playerTiles == null || playerTiles.Count == 0)
        {
            Debug.LogError("[HaymakerAbility] No player tiles found!");
            return null;
        }

        // ✅ Find empty tiles and sort by distance to Haymaker
        List<HexTile> emptyTiles = new List<HexTile>();
        foreach (var tile in playerTiles)
        {
            if (tile.occupyingUnit == null)
            {
                emptyTiles.Add(tile);
            }
        }

        if (emptyTiles.Count == 0)
        {
            Debug.LogWarning("[HaymakerAbility] No empty player tiles available for clone!");
            return null;
        }

        // ✅ Sort by distance to Haymaker and return closest
        emptyTiles.Sort((a, b) =>
            Vector3.Distance(transform.position, a.transform.position)
            .CompareTo(Vector3.Distance(transform.position, b.transform.position))
        );

        HexTile chosenTile = emptyTiles[0];
        Debug.Log($"[HaymakerAbility] Found empty tile for clone at {chosenTile.gridPosition}");
        return chosenTile;
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
            Debug.Log($"Clone empowered! Souls: {soulCount}");
        }
    }

    // Active ability
    public void Cast(UnitAI target)
    {
        StartCoroutine(PerformAbility());
    }

    private IEnumerator PerformAbility()
    {
        UnitAI target = unitAI.GetCurrentTarget();
        if (target == null || !target.isAlive || target.currentState == UnitState.Bench) yield break;

        // --- STAB ---
        if (unitAI.animator) unitAI.animator.SetTrigger("StabTrigger");

        // Lunge forward + back while stab animation is playing
        Vector3 stabStart = transform.position;
        Vector3 stabForward = stabStart + transform.forward * 0.5f; // small lunge
        float stabDuration = 0.4f; // should match animation length
        float halfDuration = stabDuration * 0.5f;

        // forward
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(stabStart, stabForward, elapsed / halfDuration);
            yield return null;
        }

        // backward
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(stabForward, stabStart, elapsed / halfDuration);
            yield return null;
        }

        // Apply stab damage
        float dmg = stabDamage[Mathf.Clamp(unitAI.starLevel - 1, 0, stabDamage.Length - 1)];
        target.TakeDamage(dmg + unitAI.attackDamage);

        yield return new WaitForSeconds(0.1f); // small delay before leap

        // --- JUMP / SLAM ---
        if (unitAI.animator) unitAI.animator.SetTrigger("JumpTrigger");

        // Find clump of enemies to leap toward
        Vector3 leapTarget = FindBestClumpPosition(5f); // 5 hex search radius
        if (leapTarget == Vector3.zero) leapTarget = transform.position; // fallback

        Vector3 jumpStart = transform.position;
        Vector3 jumpEnd = leapTarget;
        float jumpHeight = 2.5f;
        float jumpDuration = 0.6f; // match animation

        elapsed = 0f;
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;

            // arc
            Vector3 pos = Vector3.Lerp(jumpStart, jumpEnd, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;
            transform.position = pos;

            yield return null;
        }

        // Slam on landing
        // Slam on landing
        if (unitAI.animator) unitAI.animator.SetTrigger("SlamTrigger");

        yield return new WaitForSeconds(0.2f); // delay before damage lands

        float slamDmg = slamDamage[Mathf.Clamp(unitAI.starLevel - 1, 0, slamDamage.Length - 1)];
        List<UnitAI> aoeTargets = FindEnemiesInRadius(2.5f, jumpEnd);

        foreach (var e in aoeTargets)
            e.TakeDamage(slamDmg + unitAI.attackDamage);

        // ✅ Retarget after leap/slam
        UnitAI newTarget = null;
        float minDist = Mathf.Infinity;
        foreach (var enemy in aoeTargets)
        {
            if (!enemy.isAlive) continue;
            float dist = Vector3.Distance(unitAI.transform.position, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                newTarget = enemy;
            }
        }

        // Set new target if found
        if (newTarget != null)
        {
            unitAI.currentTarget = newTarget.transform;
            unitAI.SetState(UnitAI.UnitState.Combat); // make sure AI resumes properly
        }

    }

    private Vector3 FindBestClumpPosition(float searchRadius)
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        List<UnitAI> enemies = new List<UnitAI>();

        foreach (var u in allUnits)
        {
            if (u == unitAI || !u.isAlive || u.team == unitAI.team || u.currentState == UnitState.Bench) continue;
            if (Vector3.Distance(transform.position, u.transform.position) <= searchRadius)
                enemies.Add(u);
        }

        if (enemies.Count == 0) return Vector3.zero;

        // Find enemy that has the most neighbors within 2 units (clump center)
        UnitAI bestCenter = enemies[0];
        int bestCount = 0;

        foreach (var e in enemies)
        {
            int nearby = 0;
            foreach (var other in enemies)
            {
                if (other == e) continue;
                if (Vector3.Distance(e.transform.position, other.transform.position) <= 2f)
                    nearby++;
            }

            if (nearby > bestCount)
            {
                bestCount = nearby;
                bestCenter = e;
            }
        }

        return bestCenter.transform.position;
    }


    private List<UnitAI> FindEnemiesInRadius(float radius, Vector3? center = null)
    {
        if (center == null) center = transform.position;
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        List<UnitAI> enemies = new List<UnitAI>();

        foreach (var u in allUnits)
        {
            if (u == unitAI || !u.isAlive || u.team == unitAI.team || u.currentState == UnitState.Bench) continue;
            if (Vector3.Distance(center.Value, u.transform.position) <= radius)
                enemies.Add(u);
        }
        return enemies;
    }
}
