using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class HaymakerAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;
    private UnitAI clone;
    private int soulCount = 0;
    private GameObject cloneInstance;

    [Header("Ability Stats")]
    public float[] stabDamage = { 125f, 250f, 999f };
    public float[] slamDamage = { 200f, 250f, 400f };

    [Header("Passive Clone")]
    public GameObject clonePrefab;   // assign in Inspector
    private bool cloneSpawned = false;
    public Transform benchSpawnPoint;
    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        unitAI.OnStateChanged += HandleStateChanged;
    }
    private void OnEnable()
    {
        unitAI = GetComponent<UnitAI>();
        unitAI.OnStateChanged += HandleStateChanged;

        // If you want pre-placed Haymaker in scene to also spawn:
        if (!cloneSpawned && unitAI.currentState == UnitState.BoardIdle)
            SpawnClone();
    }

    private void OnDisable()
    {
        if (unitAI != null)
            unitAI.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(UnitState state)
    {
        Debug.Log($"[HaymakerAbility] State changed to {state}");
        if (!cloneSpawned && state == UnitState.BoardIdle)
        {
            SpawnClone();
        }
    }

    private void SpawnClone()
    {
        if (cloneSpawned) return;

        // choose source object
        GameObject source = clonePrefab != null ? clonePrefab : gameObject;

        // pick a spawn position (bench or beside Haymaker)
        Vector3 spawnPos = benchSpawnPoint != null
            ? benchSpawnPoint.position
            : transform.position + Vector3.right * 1.5f;

        GameObject cloneObj = Instantiate(source, spawnPos, Quaternion.identity);
        cloneObj.name = $"{unitAI.unitName} Clone";

        var cloneAI = cloneObj.GetComponent<UnitAI>();

        // if we duplicated ourselves, strip ability so the clone won't spawn another clone
        if (clonePrefab == null)
        {
            var selfAbility = cloneObj.GetComponent<HaymakerAbility>();
            if (selfAbility) Destroy(selfAbility);
        }

        // stat scaling
        cloneAI.maxHealth = unitAI.maxHealth * 0.25f;
        cloneAI.currentHealth = cloneAI.maxHealth;
        cloneAI.attackDamage = unitAI.attackDamage * 0.25f;
        cloneAI.starLevel = unitAI.starLevel;
        cloneAI.team = unitAI.team;
        cloneAI.currentMana = 0f;

        // clone should be placeable like any other unit → start it on Bench
        cloneAI.currentState = UnitState.Bench;

        // optional: disable trait scripts so it doesn’t contribute to synergies
        foreach (var mb in cloneObj.GetComponents<MonoBehaviour>())
        {
            var typeName = mb.GetType().Name;
            if (typeName.Contains("Trait"))
                mb.enabled = false;
        }

        cloneSpawned = true;
        Debug.Log("[HaymakerAbility] Clone spawned and set to Bench.");
    }

    private void Start()
    {
        UnitAI.OnAnyUnitDeath += OnUnitDeath;
    }
    private void Update()
    {
        // Spawn clone once when placed on board (not bench)
        if (unitAI.currentState == UnitState.BoardIdle && cloneInstance == null)
        {
            SpawnClone();
        }
        // ✅ If Haymaker is back on bench → destroy clone
        else if (unitAI.currentState == UnitState.Bench && cloneInstance != null)
        {
            Destroy(cloneInstance);
            cloneInstance = null;
            Debug.Log("[HaymakerAbility] Clone removed because Haymaker returned to Bench.");
        }

    }

  

    // Passive: absorb souls when units die
    public void OnUnitDeath(UnitAI deadUnit)
    {
        if (!unitAI.isAlive) return;
        if (deadUnit.team == unitAI.team) return; // absorb only enemies?

        soulCount++;
        if (soulCount % 5 == 0 && clone != null)
        {
            clone.maxHealth *= 1.01f;
            clone.attackDamage *= 1.01f;
            Debug.Log($"Clone empowered! Souls: {soulCount} → {clone.maxHealth} HP / {clone.attackDamage} dmg");
        }
    }

    // Active: Stab + Slam
    public void Cast()
    {
        StartCoroutine(PerformAbility());
    }

    private IEnumerator PerformAbility()
    {
        UnitAI target = unitAI.GetCurrentTarget();
        if (target == null) yield break;

        // Stab
        if (unitAI.animator) unitAI.animator.SetTrigger("StabTrigger");
        yield return new WaitForSeconds(0.3f); // wait for stab animation impact
        float dmg = stabDamage[Mathf.Clamp(unitAI.starLevel - 1, 0, stabDamage.Length - 1)];
        target.TakeDamage(dmg + unitAI.attackDamage);

        // Jump & Slam
        if (unitAI.animator) unitAI.animator.SetTrigger("JumpTrigger");
        yield return new WaitForSeconds(0.5f); // travel time

        // Find largest group of enemies
        List<UnitAI> enemies = FindEnemiesInRadius(5f); // search radius
        if (enemies.Count == 0) yield break;

        UnitAI center = enemies[0];
        List<UnitAI> aoeTargets = FindEnemiesInRadius(2f, center.transform.position);

        float slamDmg = slamDamage[Mathf.Clamp(unitAI.starLevel - 1, 0, slamDamage.Length - 1)];
        if (unitAI.animator) unitAI.animator.SetTrigger("SlamTrigger");

        yield return new WaitForSeconds(0.2f); // impact frame

        foreach (var e in aoeTargets)
        {
            e.TakeDamage(slamDmg + unitAI.attackDamage);
        }

        // Check for kill
        bool killed = aoeTargets.Exists(e => !e.isAlive);
        if (killed)
        {
            foreach (var e in aoeTargets)
            {
                e.TakeDamage((slamDmg * 0.5f) + unitAI.attackDamage);
            }
        }
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
