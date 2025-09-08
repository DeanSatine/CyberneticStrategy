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
        if (target == null) yield break;

        if (unitAI.animator) unitAI.animator.SetTrigger("StabTrigger");
        yield return new WaitForSeconds(0.3f);
        float dmg = stabDamage[Mathf.Clamp(unitAI.starLevel - 1, 0, stabDamage.Length - 1)];
        target.TakeDamage(dmg + unitAI.attackDamage);

        if (unitAI.animator) unitAI.animator.SetTrigger("JumpTrigger");
        yield return new WaitForSeconds(0.5f);

        List<UnitAI> enemies = FindEnemiesInRadius(5f);
        if (enemies.Count == 0) yield break;

        UnitAI center = enemies[0];
        List<UnitAI> aoeTargets = FindEnemiesInRadius(2f, center.transform.position);

        float slamDmg = slamDamage[Mathf.Clamp(unitAI.starLevel - 1, 0, slamDamage.Length - 1)];
        if (unitAI.animator) unitAI.animator.SetTrigger("SlamTrigger");

        yield return new WaitForSeconds(0.2f);

        foreach (var e in aoeTargets)
            e.TakeDamage(slamDmg + unitAI.attackDamage);
    }

    private List<UnitAI> FindEnemiesInRadius(float radius, Vector3? center = null)
    {
        if (center == null) center = transform.position;
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        List<UnitAI> enemies = new List<UnitAI>();

        foreach (var u in allUnits)
        {
            if (u == unitAI || !u.isAlive || u.team == unitAI.team) continue;
            if (Vector3.Distance(center.Value, u.transform.position) <= radius)
                enemies.Add(u);
        }
        return enemies;
    }
}
