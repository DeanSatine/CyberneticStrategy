using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class NeedleBotAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;
    private CyberneticVFX vfxManager; // ✅ Add VFX reference

    [Header("Ability Stats")]
    public int baseNeedleCount = 4;
    public int needlesPerCast;
    public float[] damagePerStar = { 100f, 150f, 175f };

    private int totalNeedlesThrown = 0;

    [Header("Timings")]
    public float startDelay = 0.25f;
    public float throwInterval = 0.1f;
    public float abilityDuration = 1.5f;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        vfxManager = GetComponent<CyberneticVFX>(); // ✅ Get VFX component
        needlesPerCast = baseNeedleCount;
    }

    public void Cast(UnitAI target)
    {
        if (!unitAI.isAlive) return;

        unitAI.canAttack = false;
        unitAI.canMove = false;

        if (unitAI.animator != null)
            unitAI.animator.SetTrigger("AbilityTrigger");

        // ✅ Call VFX system for rapid throw
        if (vfxManager != null)
            vfxManager.PlayNeedlebotRapidThrow();

        StartCoroutine(FireNeedlesRoutine());
    }

    private IEnumerator FireNeedlesRoutine()
    {
        yield return new WaitForSeconds(startDelay);

        List<UnitAI> targets = FindNearestEnemies(2);
        if (targets.Count == 0)
        {
            Debug.Log($"{unitAI.unitName} tried to cast but found no enemies!");
            EndCast();
            yield break;
        }

        int needlesPerTarget = Mathf.Max(1, needlesPerCast / targets.Count);
        float damage = damagePerStar[Mathf.Clamp(unitAI.starLevel - 1, 0, damagePerStar.Length - 1)];

        // ✅ Fire needles as actual projectiles
        for (int needleIndex = 0; needleIndex < needlesPerCast; needleIndex++)
        {
            UnitAI target = targets[needleIndex % targets.Count]; // Alternate between targets

            if (target != null && target.isAlive)
            {
                // ✅ Fire needle projectile using your existing projectile system
                if (unitAI.projectilePrefab != null)
                {
                    Vector3 spawnPos = unitAI.firePoint != null ?
                        unitAI.firePoint.position :
                        transform.position + Vector3.up * 1.5f;

                    StartCoroutine(FireNeedleProjectile(spawnPos, target, damage));
                }
                else
                {
                    // ✅ Fallback: instant damage
                    target.TakeDamage(damage + unitAI.attackDamage);
                }

                totalNeedlesThrown++;
                if (totalNeedlesThrown % 10 == 0)
                {
                    needlesPerCast++;
                    Debug.Log($"{unitAI.unitName} permanently increased needle count! Now: {needlesPerCast}");
                }
            }

            yield return new WaitForSeconds(throwInterval);
        }

        yield return new WaitForSeconds(0.2f);
        EndCast();
    }

    // ✅ Fire individual needle projectile
    private IEnumerator FireNeedleProjectile(Vector3 startPos, UnitAI target, float damage)
    {
        if (unitAI.projectilePrefab == null || target == null) yield break;

        GameObject needle = Instantiate(unitAI.projectilePrefab, startPos, Quaternion.identity);
        float speed = 20f;

        while (needle != null && target != null && target.isAlive)
        {
            Vector3 targetPos = target.transform.position + Vector3.up * 1.2f;
            Vector3 direction = (targetPos - needle.transform.position).normalized;

            needle.transform.position += direction * speed * Time.deltaTime;
            needle.transform.rotation = Quaternion.LookRotation(direction);

            if (Vector3.Distance(needle.transform.position, targetPos) < 0.3f)
            {
                // ✅ Hit target
                target.TakeDamage(damage + unitAI.attackDamage);

                // ✅ Play hit effect
                if (vfxManager != null && vfxManager.vfxConfig.autoAttackHitEffect != null)
                {
                    GameObject hitEffect = Instantiate(vfxManager.vfxConfig.autoAttackHitEffect, targetPos, Quaternion.identity);
                    Destroy(hitEffect, 1f);
                }

                Debug.Log($"{unitAI.unitName} needle hit {target.unitName} for {damage + unitAI.attackDamage} dmg!");
                Destroy(needle);
                yield break;
            }

            yield return null;
        }

        if (needle != null) Destroy(needle);
    }

    private void EndCast()
    {
        unitAI.currentMana = 0f;
        if (unitAI.unitUIPrefab != null)
            unitAI.GetComponentInChildren<UnitUI>()?.UpdateMana(unitAI.currentMana);

        unitAI.canAttack = true;
        unitAI.canMove = true;
    }

    private List<UnitAI> FindNearestEnemies(int count)
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        List<UnitAI> enemies = new List<UnitAI>();

        foreach (var u in allUnits)
        {
            if (u != unitAI && u.isAlive && u.team != unitAI.team)
                enemies.Add(u);
        }

        enemies.Sort((a, b) =>
            Vector3.Distance(unitAI.transform.position, a.transform.position)
            .CompareTo(Vector3.Distance(unitAI.transform.position, b.transform.position))
        );

        return enemies.GetRange(0, Mathf.Min(count, enemies.Count));
    }
}
