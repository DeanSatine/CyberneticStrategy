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

        // ✅ Play just the cast animation/effect (no duplicate projectile)
        if (castVFX != null)
        {
            GameObject castEffect = Instantiate(castVFX, transform.position + Vector3.up * 1.5f, Quaternion.identity);
            Destroy(castEffect, 2f);
        }

        // ✅ Fire bomb projectile (this handles both arc + damage + explosion VFX)
        StartCoroutine(FireMassiveBomb(targetPosition, 1.0f));
    }


    private IEnumerator FireMassiveBomb(Vector3 targetPosition, float damageMultiplier)
    {
        // ✅ Use VFX ability projectile instead of UnitAI.projectilePrefab
        GameObject bombPrefab = vfxManager != null ? vfxManager.vfxConfig.abilityProjectile : null;

        if (bombPrefab == null)
        {
            Debug.LogError($"❌ No bomb prefab assigned for ManaDrive ({unitAI.unitName})!");
            ExplodeBomb(targetPosition, damageMultiplier);
            yield break;
        }

        // ✅ Spawn bomb above ManaDrive
        Vector3 spawnPos = transform.position + Vector3.up * 8f;
        GameObject bomb = Instantiate(bombPrefab, spawnPos, Quaternion.identity);
        bomb.transform.localScale *= 2f;

        float fallSpeed = 15f;

        // ✅ Drop bomb downwards toward target
        while (bomb != null && bomb.transform.position.y > 0.6f)
        {
            Vector3 direction = (targetPosition - bomb.transform.position);
            direction.y = -1f; // force downward
            direction.Normalize();

            bomb.transform.position += direction * fallSpeed * Time.deltaTime;

            yield return null;
        }

        if (bomb != null)
        {
            bomb.transform.position = targetPosition + Vector3.up * 0.5f;
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

        // ✅ Optional splash indicator VFX
        if (splashVFX != null)
        {
            GameObject splash = Instantiate(splashVFX, explosionPos, Quaternion.identity);
            splash.transform.localScale = Vector3.one * splashRadius * 2f; // scale to match radius
            Destroy(splash, 2f);
        }

        // ✅ Damage all enemies in splash radius (distance check instead of relying on colliders)
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        foreach (UnitAI enemy in allUnits)
        {
            if (enemy != null && enemy.isAlive && enemy.team != unitAI.team)
            {
                float distance = Vector3.Distance(enemy.transform.position, explosionPos);
                if (distance <= splashRadius)
                {
                    float healthBeforeHit = enemy.currentHealth;
                    enemy.TakeDamage(damage);

                    if (healthBeforeHit > 0 && enemy.currentHealth <= 0)
                    {
                        killedTarget = true;
                    }

                    Debug.Log($"{unitAI.unitName} bomb hit {enemy.unitName} for {damage} damage at distance {distance:F1}!");
                }
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

        // ✅ Reset mana after cast
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
