using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class SightlineAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;

    [Header("Active Ability - Laser Turret")]
    public GameObject laserTurretPrefab;
    public float[] damagePerSecond = { 40f, 70f, 200f };
    [Tooltip("AD scaling ratios per star level (50/60/200%)")]
    public float[] dpsADRatio = { 0.5f, 0.6f, 2.0f };
    public float[] durationPerStar = { 5f, 5f, 5f };
    public float turretSpawnRadius = 2f;
    public float turretScale = 1.5f;

    [Header("Passive Stats")]
    public float attackSpeedPerStack = 0.03f;
    private int currentStacks = 0;
    private float baseAttackSpeed;
    private int castsThisRound = 0;

    private List<LaserTurret> activeTurrets = new List<LaserTurret>();

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();

        if (unitAI != null)
        {
            baseAttackSpeed = unitAI.attackSpeed;
            Debug.Log($"⚡ [SIGHTLINE] Awake - Base attack speed: {baseAttackSpeed}");
        }
        else
        {
            Debug.LogError($"🔴 [SIGHTLINE] Awake - UnitAI component NOT FOUND!");
        }
    }

    private void OnEnable()
    {
        Debug.Log($"🔴 [SIGHTLINE] OnEnable called on {gameObject.name}");

        if (unitAI != null)
        {
            unitAI.OnAttackEvent += OnBasicAttack;
        }
    }

    private void OnDisable()
    {
        Debug.Log($"🔴 [SIGHTLINE] OnDisable called - cleaning up turrets");

        if (unitAI != null)
        {
            unitAI.OnAttackEvent -= OnBasicAttack;
        }

        CleanupAllTurrets();
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
    }

    public void Cast(UnitAI target)
    {
        if (!unitAI.isAlive)
            return;

        int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, 2);
        float baseDPS = damagePerSecond[starIndex];
        float adRatio = dpsADRatio[starIndex];
        float totalDPS = baseDPS + (unitAI.attackDamage * adRatio);
        float duration = durationPerStar[starIndex];

        castsThisRound++;

        Debug.Log($"🔴 [SIGHTLINE] Cast #{castsThisRound} - DPS: {totalDPS:F0} ({baseDPS} + {adRatio * 100:F0}% AD)");

        for (int i = 0; i < castsThisRound; i++)
        {
            SpawnLaserTurret(totalDPS, duration);
        }

        unitAI.currentMana = 0f;
        if (unitAI.ui != null)
            unitAI.ui.UpdateMana(0f);
    }

    private void SpawnLaserTurret(float dps, float duration)
    {
        Debug.Log($"🔴 [SIGHTLINE] >>> SpawnLaserTurret START - current count: {activeTurrets.Count}");

        CleanupNullTurrets();

        int turretIndex = activeTurrets.Count;
        float angleStep = 360f / Mathf.Max(1, turretIndex + 1);
        float angle = angleStep * turretIndex * Mathf.Deg2Rad;

        Vector3 spawnOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * turretSpawnRadius;
        Vector3 spawnPos = transform.position + spawnOffset + Vector3.up * 1f;

        Debug.Log($"🔴 [SIGHTLINE] Spawning turret #{turretIndex + 1} at {spawnPos}");

        GameObject turretObj = null;

        if (laserTurretPrefab != null)
        {
            Quaternion upright = Quaternion.Euler(90f, 0f, 0f);
            turretObj = Instantiate(laserTurretPrefab, spawnPos, upright);
            turretObj.transform.localScale = Vector3.one * turretScale;
            turretObj.name = $"LaserTurret_{turretIndex + 1}";
            Debug.Log($"🔴 [SIGHTLINE] Instantiated turret GameObject: {turretObj.name}");
        }
        else
        {
            turretObj = new GameObject($"LaserTurret_{turretIndex}");
            turretObj.transform.position = spawnPos;
            turretObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            turretObj.transform.localScale = Vector3.one * turretScale;
            Debug.LogWarning("⚠️ [SIGHTLINE] No laser turret prefab assigned! Creating empty GameObject");
        }

        LaserTurret turret = turretObj.GetComponent<LaserTurret>();
        if (turret == null)
        {
            turret = turretObj.AddComponent<LaserTurret>();
            Debug.Log($"🔴 [SIGHTLINE] Added LaserTurret component");
        }
        else
        {
            Debug.Log($"🔴 [SIGHTLINE] LaserTurret component already exists on prefab");
        }

        turret.Initialize(unitAI, dps, duration, this);
        Debug.Log($"🔴 [SIGHTLINE] Initialized turret with {dps:F0} DPS for {duration}s");

        activeTurrets.Add(turret);
        Debug.Log($"🔴 [SIGHTLINE] Added to activeTurrets list. New count: {activeTurrets.Count}");

        RepositionExistingTurrets();

        Debug.Log($"🔴🔴🔴 [SIGHTLINE] ===== TURRET #{turretIndex + 1} FULLY SPAWNED! Total: {activeTurrets.Count}, Total DPS: {dps * activeTurrets.Count:F0}/s =====");
    }

    private void RepositionExistingTurrets()
    {
        CleanupNullTurrets();

        int totalTurrets = activeTurrets.Count;
        Debug.Log($"🔴 [SIGHTLINE] Repositioning {totalTurrets} turrets");

        if (totalTurrets <= 1)
        {
            Debug.Log($"🔴 [SIGHTLINE] Only {totalTurrets} turret(s), skipping reposition");
            return;
        }

        float angleStep = 360f / totalTurrets;

        for (int i = 0; i < totalTurrets; i++)
        {
            if (activeTurrets[i] == null)
                continue;

            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * turretSpawnRadius;
            Vector3 newPos = transform.position + newOffset + Vector3.up * 1f;

            activeTurrets[i].transform.position = newPos;
        }

        Debug.Log($"🔴 [SIGHTLINE] Repositioned {totalTurrets} turrets in circle formation");
    }

    public void OnTurretExpired(LaserTurret turret)
    {
        Debug.Log($"🔴 [SIGHTLINE] OnTurretExpired callback received");

        if (activeTurrets.Contains(turret))
        {
            activeTurrets.Remove(turret);
            RepositionExistingTurrets();
            Debug.Log($"🔴 [SIGHTLINE] Turret removed from list. Remaining: {activeTurrets.Count}");
        }
    }

    private void CleanupNullTurrets()
    {
        int beforeCount = activeTurrets.Count;
        activeTurrets.RemoveAll(t => t == null);
        int afterCount = activeTurrets.Count;

        if (beforeCount != afterCount)
        {
            Debug.LogWarning($"🔴 [SIGHTLINE] Cleaned up {beforeCount - afterCount} null turrets! ({beforeCount} -> {afterCount})");
        }
    }

    private void CleanupAllTurrets()
    {
        Debug.Log($"🔴 [SIGHTLINE] CleanupAllTurrets called - destroying {activeTurrets.Count} turrets");

        foreach (var turret in activeTurrets)
        {
            if (turret != null)
            {
                Debug.Log($"🔴 [SIGHTLINE] Destroying turret: {turret.gameObject.name}");
                Destroy(turret.gameObject);
            }
        }
        activeTurrets.Clear();
        Debug.Log($"🔴 [SIGHTLINE] All turrets cleaned up. List cleared.");
    }

    public void OnRoundEnd()
    {
        castsThisRound = 0;

        Debug.Log($"🔴 [SIGHTLINE] ===== OnRoundEnd called =====");

        if (StageManager.Instance != null && StageManager.Instance.currentPhase == StageManager.GamePhase.Prep)
        {
            Debug.Log($"🔴 [SIGHTLINE] Prep phase detected - clearing all turrets and resetting stacks");
            currentStacks = 0;

            if (unitAI != null)
            {
                unitAI.attackSpeed = baseAttackSpeed;
            }

            CleanupAllTurrets();
        }
        else
        {
            Debug.Log($"🔴 [SIGHTLINE] Combat phase - resetting stacks but turrets persist");
            currentStacks = 0;

            if (unitAI != null)
            {
                unitAI.attackSpeed = baseAttackSpeed;
            }

            CleanupNullTurrets();
            Debug.Log($"🔴 [SIGHTLINE] After cleanup: {activeTurrets.Count} active turrets remain");
        }
    }

    public int GetCurrentStacks() => currentStacks;
    public int GetActiveTurretCount() => activeTurrets.Count;
}
