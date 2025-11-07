using System.Collections;
using UnityEngine;

public class LaserTurret : MonoBehaviour
{
    private UnitAI ownerUnit;
    private float damagePerShot;
    private float fireRate;
    private float nextFireTime;

    [Header("VFX")]
    public GameObject laserProjectilePrefab;
    public GameObject muzzleFlashPrefab;
    public GameObject hitEffectPrefab;
    public float projectileSpeed = 30f;

    public void Initialize(UnitAI owner, float damage, float duration, float fireRate)
    {
        this.ownerUnit = owner;
        this.damagePerShot = damage;
        this.fireRate = fireRate;
        this.nextFireTime = Time.time;

        StartCoroutine(FireAtTargets());
    }

    private IEnumerator FireAtTargets()
    {
        while (ownerUnit != null && ownerUnit.isAlive)
        {
            if (Time.time >= nextFireTime)
            {
                UnitAI target = FindTarget();
                if (target != null && target.isAlive)
                {
                    FireLaser(target);
                    nextFireTime = Time.time + (1f / fireRate);
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
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
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = unit;
            }
        }

        return closestEnemy;
    }

    private void FireLaser(UnitAI target)
    {
        Vector3 startPos = transform.position + Vector3.up * 1f;
        Vector3 targetPos = target.transform.position + Vector3.up * 1.2f;
        Vector3 direction = (targetPos - startPos).normalized;

        if (muzzleFlashPrefab != null)
        {
            GameObject muzzle = Instantiate(muzzleFlashPrefab, startPos, Quaternion.identity);
            Destroy(muzzle, 0.5f);
        }

        StartCoroutine(FireLaserProjectile(startPos, target, direction));
    }

    private IEnumerator FireLaserProjectile(Vector3 startPos, UnitAI target, Vector3 direction)
    {
        GameObject projectile = null;

        if (laserProjectilePrefab != null)
        {
            projectile = Instantiate(laserProjectilePrefab, startPos, Quaternion.LookRotation(direction));
        }

        if (projectile == null)
        {
            target.TakeDamage(damagePerShot);
            Debug.Log($"⚡ [TURRET] Hit {target.unitName} for {damagePerShot} damage (instant)");
            yield break;
        }

        float travelTime = 0f;
        float maxTravelTime = 2f;
        bool hasHit = false;

        while (projectile != null && travelTime < maxTravelTime && !hasHit)
        {
            projectile.transform.position += direction * projectileSpeed * Time.deltaTime;

            if (target != null && target.isAlive)
            {
                float distance = Vector3.Distance(projectile.transform.position, target.transform.position + Vector3.up * 1.2f);
                if (distance < 0.8f)
                {
                    target.TakeDamage(damagePerShot);
                    hasHit = true;

                    if (hitEffectPrefab != null)
                    {
                        GameObject hit = Instantiate(hitEffectPrefab, target.transform.position + Vector3.up * 1.2f, Quaternion.identity);
                        Destroy(hit, 1f);
                    }

                    Debug.Log($"⚡ [TURRET] Hit {target.unitName} for {damagePerShot} damage!");
                    break;
                }
            }
            else
            {
                break;
            }

            travelTime += Time.deltaTime;
            yield return null;
        }

        if (projectile != null)
            Destroy(projectile);
    }
}
