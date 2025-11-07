using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class SightlineAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;

    [Header("Active Ability - Laser Turret")]
    public GameObject laserTurretPrefab;
    public float[] damagePerShot = { 40f, 70f, 200f };
    public float[] durationPerStar = { 5f, 5f, 5f };
    public float turretSpawnRadius = 1.5f;
    public float turretFireRate = 1f;

    [Header("Passive Stats")]
    public float attackSpeedPerStack = 0.03f;
    private int currentStacks = 0;
    private float baseAttackSpeed;

    private List<LaserTurret> activeTurrets = new List<LaserTurret>();

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();

        if (unitAI != null)
        {
            baseAttackSpeed = unitAI.attackSpeed;
            Debug.Log($"⚡ [SIGHTLINE] Base attack speed: {baseAttackSpeed}");
        }
    }

    private void OnEnable()
    {
        if (unitAI != null)
        {
            unitAI.OnAttackEvent += OnBasicAttack;
        }
    }

    private void OnDisable()
    {
        if (unitAI != null)
        {
            unitAI.OnAttackEvent -= OnBasicAttack;
        }

        DestroyAllTurrets();
    }

    private void OnBasicAttack(UnitAI target)
    {
        if (unitAI.currentState == UnitAI.UnitState.Bench)
            return;

        currentStacks++;
        UpdateAttackSpeed();

        Debug.Log($"⚡ [SIGHTLINE] Attack speed stack gained! Now at {currentStacks} stacks (+{currentStacks * attackSpeedPerStack * 100:F0}% AS)");
    }

    private void UpdateAttackSpeed()
    {
        if (unitAI == null)
            return;

        float bonus = currentStacks * attackSpeedPerStack;
        unitAI.attackSpeed = baseAttackSpeed + bonus;

        Debug.Log($"⚡ [SIGHTLINE] Attack speed updated: {unitAI.attackSpeed:F2} (base: {baseAttackSpeed:F2} + {bonus:F2})");
    }

    public void Cast(UnitAI target)
    {
        if (!unitAI.isAlive)
            return;

        int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, 2);
        float damage = damagePerShot[starIndex];
        float duration = durationPerStar[starIndex];

        SpawnLaserTurret(damage, duration);

        unitAI.currentMana = 0f;
        if (unitAI.ui != null)
            unitAI.ui.UpdateMana(0f);
    }

    private void SpawnLaserTurret(float damage, float duration)
    {
        Vector3 spawnOffset = Random.insideUnitCircle.normalized * turretSpawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(spawnOffset.x, 0.5f, spawnOffset.y);

        GameObject turretObj = null;

        if (laserTurretPrefab != null)
        {
            turretObj = Instantiate(laserTurretPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            turretObj = new GameObject($"LaserTurret_{activeTurrets.Count}");
            turretObj.transform.position = spawnPos;
        }

        LaserTurret turret = turretObj.GetComponent<LaserTurret>();
        if (turret == null)
        {
            turret = turretObj.AddComponent<LaserTurret>();
        }

        turret.Initialize(unitAI, damage, duration, turretFireRate);
        activeTurrets.Add(turret);

        Debug.Log($"🔴 [SIGHTLINE] Laser turret spawned! Damage: {damage}, Duration: {duration}s. Total turrets: {activeTurrets.Count}");

        StartCoroutine(RemoveTurretAfterDuration(turret, duration));
    }

    private IEnumerator RemoveTurretAfterDuration(LaserTurret turret, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (turret != null)
        {
            activeTurrets.Remove(turret);
            Destroy(turret.gameObject);
            Debug.Log($"🔴 [SIGHTLINE] Turret expired. Remaining turrets: {activeTurrets.Count}");
        }
    }

    private void DestroyAllTurrets()
    {
        foreach (var turret in activeTurrets)
        {
            if (turret != null)
                Destroy(turret.gameObject);
        }
        activeTurrets.Clear();
    }

    public void OnRoundEnd()
    {
        currentStacks = 0;
        DestroyAllTurrets();

        if (unitAI != null)
        {
            unitAI.attackSpeed = baseAttackSpeed;
            unitAI.isCastingAbility = false;
            unitAI.canAttack = true;
        }
    }

    public int GetCurrentStacks() => currentStacks;
    public int GetActiveTurretCount() => activeTurrets.Count;
}
