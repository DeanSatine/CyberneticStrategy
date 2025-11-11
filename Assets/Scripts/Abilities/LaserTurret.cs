using System.Collections;
using UnityEngine;

public class LaserTurret : MonoBehaviour
{
    private UnitAI ownerUnit;
    private float damagePerSecond;
    private UnitAI currentTarget;
    private float damageTimer;
    private float lifetime;
    private float lifetimeTimer;
    private SightlineAbility parentAbility;

    [Header("Beam Settings")]
    public float rotationSpeed = 180f;
    public float damageTickRate = 0.25f;
    public float targetAcquisitionRange = 15f;

    [Header("Piercing Settings")]
    [Tooltip("Enable piercing damage to units 1 hex behind the primary target")]
    public bool enablePiercing = true;

    [Tooltip("Maximum distance in world units to consider for piercing (for alignment check)")]
    public float piercingAlignmentTolerance = 1.5f;

    [Header("VFX Children (optional)")]
    public Transform beamVisual;
    public ParticleSystem[] beamParticles;

    private void Awake()
    {
        if (beamVisual == null)
        {
            beamVisual = transform.Find("Shoot");
        }

        beamParticles = GetComponentsInChildren<ParticleSystem>();
    }

    public void Initialize(UnitAI owner, float dps, float duration, SightlineAbility ability)
    {
        this.ownerUnit = owner;
        this.damagePerSecond = dps;
        this.damageTimer = 0f;
        this.lifetime = duration;
        this.lifetimeTimer = 0f;
        this.parentAbility = ability;

        Debug.Log($"⚡ [TURRET] Initialized! DPS: {dps}, Duration: {duration}s, Piercing: {enablePiercing}");

        StartCoroutine(AcquireAndAttackTargets());
    }

    private void Update()
    {
        lifetimeTimer += Time.deltaTime;

        if (lifetimeTimer >= lifetime)
        {
            Debug.Log($"⚡ [TURRET] Lifetime expired ({lifetimeTimer:F1}s / {lifetime}s), destroying self");

            if (parentAbility != null)
            {
                parentAbility.OnTurretExpired(this);
            }

            Destroy(gameObject);
            return;
        }

        if (currentTarget != null && currentTarget.isAlive)
        {
            RotateTowardsTarget(currentTarget);
        }
    }

    private IEnumerator AcquireAndAttackTargets()
    {
        while (ownerUnit != null && ownerUnit.isAlive && lifetimeTimer < lifetime)
        {
            currentTarget = FindTarget();

            if (currentTarget != null && currentTarget.isAlive)
            {
                EnableBeam(true);
                yield return StartCoroutine(DamageTarget(currentTarget));
            }
            else
            {
                EnableBeam(false);
                yield return new WaitForSeconds(0.2f);
            }
        }

        EnableBeam(false);
    }

    private IEnumerator DamageTarget(UnitAI target)
    {
        damageTimer = 0f;

        while (target != null && target.isAlive && ownerUnit != null && ownerUnit.isAlive && lifetimeTimer < lifetime)
        {
            damageTimer += Time.deltaTime;

            if (damageTimer >= damageTickRate)
            {
                float damageThisTick = damagePerSecond * damageTickRate;
                target.TakeDamage(damageThisTick);
                damageTimer = 0f;

                Debug.Log($"⚡ [TURRET] Dealt {damageThisTick:F1} damage to {target.unitName} ({damagePerSecond} DPS)");

                if (enablePiercing)
                {
                    UnitAI piercingTarget = FindPiercingTarget(target);
                    if (piercingTarget != null)
                    {
                        piercingTarget.TakeDamage(damageThisTick);
                        Debug.Log($"⚡🎯 [TURRET PIERCE] Dealt {damageThisTick:F1} damage to {piercingTarget.unitName} (behind {target.unitName})");
                    }
                }
            }

            yield return null;
        }
    }

    private UnitAI FindPiercingTarget(UnitAI primaryTarget)
    {
        if (primaryTarget == null || primaryTarget.currentTile == null)
            return null;

        Vector3 directionToTarget = (primaryTarget.transform.position - transform.position).normalized;

        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        UnitAI piercingTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (var unit in allUnits)
        {
            if (unit == null || !unit.isAlive || unit == primaryTarget || unit.team == ownerUnit.team || unit.currentState == UnitAI.UnitState.Bench)
                continue;

            Vector3 directionToUnit = (unit.transform.position - transform.position).normalized;
            float dotProduct = Vector3.Dot(directionToTarget, directionToUnit);

            if (dotProduct > 0.9f)
            {
                float distanceFromTurret = Vector3.Distance(transform.position, unit.transform.position);
                float distanceFromPrimary = Vector3.Distance(primaryTarget.transform.position, unit.transform.position);

                if (distanceFromTurret > Vector3.Distance(transform.position, primaryTarget.transform.position) &&
                    distanceFromPrimary < piercingAlignmentTolerance * 2f &&
                    distanceFromTurret < closestDistance)
                {
                    closestDistance = distanceFromTurret;
                    piercingTarget = unit;
                }
            }
        }

        return piercingTarget;
    }

    private UnitAI FindTarget()
    {
        if (ownerUnit == null)
            return null;

        UnitAI closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        foreach (var unit in allUnits)
        {
            if (unit == null || !unit.isAlive || unit.team == ownerUnit.team || unit.currentState == UnitAI.UnitState.Bench)
                continue;

            float distance = Vector3.Distance(transform.position, unit.transform.position);
            if (distance <= targetAcquisitionRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = unit;
            }
        }

        return closestEnemy;
    }

    private void RotateTowardsTarget(UnitAI target)
    {
        if (target == null)
            return;

        Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
        directionToTarget.y = 0f;

        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget) * Quaternion.Euler(90f, 0f, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void EnableBeam(bool enable)
    {
        if (beamVisual != null)
        {
            beamVisual.gameObject.SetActive(enable);
        }

        foreach (var ps in beamParticles)
        {
            if (ps != null)
            {
                if (enable && !ps.isPlaying)
                    ps.Play();
                else if (!enable && ps.isPlaying)
                    ps.Stop();
            }
        }
    }
}
