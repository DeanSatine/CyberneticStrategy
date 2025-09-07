using UnityEngine;
using static UnitAI;
using System.Collections.Generic;
using System.Collections;

public class ManaDriveAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;
    private CyberneticVFX vfxManager; // ✅ Add VFX reference

    [Header("Ability Stats")]
    public float[] damagePerStar = { 200, 250, 350 }; // ✅ Updated to match your design doc
    public float range = 4f;
    public float splashRadius = 2f;
    public float attackSpeedGain = 0.3f; // ✅ Attack speed gain on kill
    public float recursiveDamageReduction = 0.25f; // ✅ 75% effectiveness on recursive cast

    [Header("Optional VFX")]
    public GameObject castVFX;
    public GameObject impactVFX;
    public GameObject splashVFX;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        vfxManager = GetComponent<CyberneticVFX>(); // ✅ Get VFX component
    }

    public void Cast(UnitAI target)
    {
        if (unitAI.animator != null)
            unitAI.animator.SetTrigger("AbilityTrigger");

        // ✅ Find largest group of enemies for bomb targeting
        Vector3 targetPosition = FindLargestEnemyGroup();

        // ✅ Call VFX system for massive bomb
        if (vfxManager != null)
            vfxManager.PlayManaDriveMassiveBomb(targetPosition);

        // ✅ Fire bomb projectile
        StartCoroutine(FireMassiveBomb(targetPosition, 1.0f)); // Full damage
    }

    private IEnumerator FireMassiveBomb(Vector3 targetPosition, float damageMultiplier)
    {
        if (unitAI.projectilePrefab == null) yield break;

        Vector3 spawnPos = unitAI.firePoint != null ?
            unitAI.firePoint.position :
            transform.position + Vector3.up * 1.5f;

        GameObject bombPrefab = unitAI.projectilePrefab;
        GameObject bomb = Instantiate(bombPrefab, spawnPos, Quaternion.identity);

        // ✅ Scale bomb to look bigger
        bomb.transform.localScale *= 2f;

        // ✅ Arc trajectory parameters
        float flightTime = 2f; // Total time for bomb to reach target
        float maxHeight = 7f; // How high the arc goes
        float elapsed = 0f;

        Vector3 startPos = spawnPos;
        Vector3 endPos = targetPosition;

        while (bomb != null && elapsed < flightTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / flightTime; // 0 to 1

            // ✅ Calculate arc position
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);

            // ✅ Add parabolic height (goes up then down)
            float height = maxHeight * 4f * progress * (1f - progress); // Parabolic curve
            currentPos.y += height;

            bomb.transform.position = currentPos;

            // ✅ Rotate bomb to face movement direction
            if (progress < 0.99f) // Don't rotate on the very last frame
            {
                Vector3 nextPos = Vector3.Lerp(startPos, endPos, (elapsed + Time.deltaTime) / flightTime);
                nextPos.y += maxHeight * 4f * ((elapsed + Time.deltaTime) / flightTime) * (1f - ((elapsed + Time.deltaTime) / flightTime));

                Vector3 direction = (nextPos - currentPos).normalized;
                if (direction.magnitude > 0.1f)
                {
                    bomb.transform.rotation = Quaternion.LookRotation(direction);
                }
            }

            yield return null;
        }

        // ✅ Bomb reaches target and explodes
        if (bomb != null)
        {
            bomb.transform.position = targetPosition; // Ensure exact landing
            ExplodeBomb(targetPosition, damageMultiplier);
            Destroy(bomb);
        }
    }


    // ✅ Handle bomb explosion and damage
    private void ExplodeBomb(Vector3 explosionPos, float damageMultiplier)
    {
        float damage = damagePerStar[Mathf.Clamp(unitAI.starLevel - 1, 0, damagePerStar.Length - 1)] * damageMultiplier;
        bool killedTarget = false;

        // ✅ Explosion VFX
        if (vfxManager != null && vfxManager.vfxConfig.abilityImpactEffect != null)
        {
            GameObject explosion = Instantiate(vfxManager.vfxConfig.abilityImpactEffect, explosionPos, Quaternion.identity);
            explosion.transform.localScale *= 3f; // Large explosion
            Destroy(explosion, 3f);
        }

        // ✅ Damage all enemies in splash radius
        Collider[] hits = Physics.OverlapSphere(explosionPos, splashRadius);
        foreach (Collider hit in hits)
        {
            UnitAI enemy = hit.GetComponent<UnitAI>();
            if (enemy != null && enemy.teamID != unitAI.teamID && enemy.isAlive)
            {
                float healthBeforeHit = enemy.currentHealth;
                enemy.TakeDamage(damage);

                // ✅ Check if this killed the target
                if (healthBeforeHit > 0 && enemy.currentHealth <= 0)
                {
                    killedTarget = true;
                }

                Debug.Log($"{unitAI.unitName} bomb hit {enemy.unitName} for {damage} damage!");
            }
        }

        // ✅ If bomb killed a target, gain attack speed and cast again
        if (killedTarget)
        {
            unitAI.attackSpeed += attackSpeedGain;
            Debug.Log($"{unitAI.unitName} gained {attackSpeedGain} attack speed! Now: {unitAI.attackSpeed}");

            // ✅ Cast again at 75% effectiveness
            Vector3 newTargetPos = FindLargestEnemyGroup();
            if (newTargetPos != Vector3.zero)
            {
                Debug.Log($"{unitAI.unitName} casting recursive bomb!");
                StartCoroutine(FireMassiveBomb(newTargetPos, 1f - recursiveDamageReduction));
            }
        }

        // ✅ Reset mana
        unitAI.currentMana = 0;
    }

    // ✅ Find position with most enemies clustered together
    private Vector3 FindLargestEnemyGroup()
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        List<UnitAI> enemies = new List<UnitAI>();

        foreach (var unit in allUnits)
        {
            if (unit != unitAI && unit.isAlive && unit.team != unitAI.team)
                enemies.Add(unit);
        }

        if (enemies.Count == 0) return Vector3.zero;

        Vector3 bestPosition = enemies[0].transform.position;
        int maxEnemiesInRange = 0;

        // ✅ Check each enemy position as potential bomb center
        foreach (var enemy in enemies)
        {
            int enemiesInRange = 0;
            foreach (var otherEnemy in enemies)
            {
                if (Vector3.Distance(enemy.transform.position, otherEnemy.transform.position) <= splashRadius)
                {
                    enemiesInRange++;
                }
            }

            if (enemiesInRange > maxEnemiesInRange)
            {
                maxEnemiesInRange = enemiesInRange;
                bestPosition = enemy.transform.position;
            }
        }

        return bestPosition;
    }
}
