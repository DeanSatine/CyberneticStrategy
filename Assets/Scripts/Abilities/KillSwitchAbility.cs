using System.Collections;
using UnityEngine;
using static UnitAI;

public class KillSwitchAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;
    private UnitAI lastTarget;
    private int attackCounter = 0;

    [Header("Ability Stats")]
    public float[] slamDamagePerStar = { 100f, 150f, 250f };
    public float passiveSlamDamage = 100f;
    public float healOnTargetSwap = 200f;
    public float armorShred = 3f;

    [Header("Timings (no animation events)")]
    public float leapDuration = 0.5f;
    public float slamDuration = 0.7f;
    public float leapHeight = 2f;
    public float maxLeapRange = 3f; // 🔹 3 hexes

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
    }

    // Called by UnitAI
    public void Cast(UnitAI _ignored)
    {
        // 🔹 Find nearest enemy in range instead of relying on passed target
        UnitAI target = FindNearestEnemyInRange(maxLeapRange);
        if (target != null)
            StartCoroutine(LeapAndSlam(target));
        else
            Debug.Log($"{unitAI.unitName} tried to leap, but no enemies within {maxLeapRange}!");
    }

    public void OnAttack(UnitAI target)
    {
        attackCounter++;

        if (attackCounter % 2 == 0 && target != null)
        {
            target.armor = Mathf.Max(0, target.armor - armorShred);
            Debug.Log($"{unitAI.unitName} shredded {target.unitName}'s armor! Now {target.armor}");
        }

        if (lastTarget != null && lastTarget != target)
        {
            float dmg = passiveSlamDamage + unitAI.attackDamage;
            lastTarget.TakeDamage(dmg);
            unitAI.currentHealth = Mathf.Min(unitAI.maxHealth, unitAI.currentHealth + healOnTargetSwap);

            Debug.Log($"{unitAI.unitName} slammed {lastTarget.unitName} on target swap for {dmg} dmg + healed {healOnTargetSwap} HP!");
        }

        lastTarget = target;
    }

    private IEnumerator LeapAndSlam(UnitAI target)
    {
        Vector3 startPos = unitAI.transform.position;
        Vector3 endPos = target.transform.position;

        // 🔹 Play Leap animation
        if (unitAI.animator) unitAI.animator.SetTrigger("LeapTrigger");

        float elapsed = 0f;
        while (elapsed < leapDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / leapDuration;

            Vector3 midPoint = Vector3.Lerp(startPos, endPos, t);
            midPoint.y += Mathf.Sin(t * Mathf.PI) * leapHeight;

            unitAI.transform.position = midPoint;
            yield return null;
        }

        // 🔹 Land directly on enemy
        unitAI.transform.position = endPos;

        // 🔹 Play Ability animation immediately after landing
        if (unitAI.animator) unitAI.animator.SetTrigger("AbilityTrigger");

        yield return new WaitForSeconds(slamDuration);

        // Deal damage
        float damage = slamDamagePerStar[Mathf.Clamp(unitAI.starLevel - 1, 0, slamDamagePerStar.Length - 1)]
                       + unitAI.attackDamage;
        target.TakeDamage(damage);

        Debug.Log($"{unitAI.unitName} leapt and slammed {target.unitName} for {damage}!");

        StartCoroutine(TemporaryAttackSpeedBuff(1.5f, 4f));
    }

    private IEnumerator TemporaryAttackSpeedBuff(float multiplier, float duration)
    {
        float original = unitAI.attackSpeed;
        unitAI.attackSpeed *= multiplier;

        yield return new WaitForSeconds(duration);

        unitAI.attackSpeed = original;
    }

    // 🔹 Find nearest enemy within a given range
    private UnitAI FindNearestEnemyInRange(float range)
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        UnitAI nearest = null;
        float minDist = Mathf.Infinity;

        foreach (var unit in allUnits)
        {
            if (unit == unitAI || !unit.isAlive) continue;
            if (unit.team == unitAI.team) continue; // don’t target allies

            float dist = Vector3.Distance(unitAI.transform.position, unit.transform.position);
            if (dist < minDist && dist <= range)
            {
                minDist = dist;
                nearest = unit;
            }
        }
        return nearest;
    }
}
