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
        // pick closest hex next to Haymaker
        Vector3 spawnPos = FindClosestEmptyHex(transform.position);

        cloneInstance = Instantiate(clonePrefab, spawnPos, Quaternion.identity);
        cloneInstance.name = $"{unitAI.unitName} Clone";

        var cloneAI = cloneInstance.GetComponent<UnitAI>();

        // scale stats
        cloneAI.maxHealth = unitAI.maxHealth * 0.25f;
        cloneAI.currentHealth = cloneAI.maxHealth;
        cloneAI.attackDamage = unitAI.attackDamage * 0.25f;
        cloneAI.starLevel = unitAI.starLevel;
        cloneAI.team = unitAI.team;
        cloneAI.currentMana = 0f;
        cloneAI.isAlive = true;

        // put the clone directly into BoardIdle so it participates in combat
        cloneAI.currentState = UnitState.BoardIdle;

        // prevent infinite cloning
        var selfAbility = cloneInstance.GetComponent<HaymakerAbility>();
        if (selfAbility) Destroy(selfAbility);

        // disable traits so clone doesn’t affect synergies
        foreach (var mb in cloneInstance.GetComponents<MonoBehaviour>())
        {
            if (mb.GetType().Name.Contains("Trait"))
                mb.enabled = false;
        }

        // register with GameManager
        GameManager.Instance.RegisterUnit(cloneAI, cloneAI.team == Team.Player);

        Debug.Log("[HaymakerAbility] Clone spawned next to Haymaker.");
    }

    private void DestroyClone()
    {
        if (cloneInstance != null)
        {
            GameManager.Instance.UnregisterUnit(cloneInstance.GetComponent<UnitAI>());
            Destroy(cloneInstance);
            cloneInstance = null;
            Debug.Log("[HaymakerAbility] Clone destroyed.");
        }
    }

    private Vector3 FindClosestEmptyHex(Vector3 haymakerPos)
    {
        // 🔑 You can expand this with your HexTile system.
        // For now: spawn 1.5 units to the right of Haymaker.
        return haymakerPos + Vector3.right * 1.5f;
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

    // Active ability (unchanged)
    public void Cast()
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
