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

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
    }

    // ✅ Now accepts a target passed in from UnitAI
    public void Cast(UnitAI target)
    {
        if (target != null)
            StartCoroutine(LeapAndSlam(target));
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

        unitAI.transform.position = endPos;

        if (unitAI.animator) unitAI.animator.SetTrigger("AbilityTrigger");

        yield return new WaitForSeconds(slamDuration);

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
}
