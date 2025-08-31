using System.Collections;
using UnityEngine;
using static UnitAI;

public class KillSwitchAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;

    private UnitAI lastTarget;
    private int attackCounter = 0;

    [Header("Ability Stats")]
    public float[] slamDamagePerStar = { 100f, 150f, 250f }; // you can tune
    public float passiveSlamDamage = 100f;
    public float healOnTargetSwap = 200f;
    public float armorShred = 3f;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
    }

    // Called by UnitAI when mana is full
    public void Cast()
    {
        StartCoroutine(LeapAndSlam());
    }

    // Passive hook: call this from UnitAI.Attack after dealing damage
    public void OnAttack(UnitAI target)
    {
        attackCounter++;

        // Every other attack → shred armor
        if (attackCounter % 2 == 0 && target != null)
        {
            target.armor = Mathf.Max(0, target.armor - armorShred);
            Debug.Log($"{unitAI.unitName} shredded {target.unitName}'s armor! Now {target.armor}");
        }

        // Check for target swap
        if (lastTarget != null && lastTarget != target)
        {
            // Slam old target
            float dmg = passiveSlamDamage + unitAI.attackDamage;
            lastTarget.TakeDamage(dmg);
            unitAI.currentHealth = Mathf.Min(unitAI.maxHealth, unitAI.currentHealth + healOnTargetSwap);

            Debug.Log($"{unitAI.unitName} slammed {lastTarget.unitName} on target swap for {dmg} dmg + healed {healOnTargetSwap} HP!");
        }

        lastTarget = target;
    }

    private IEnumerator LeapAndSlam()
    {
        // Find farthest enemy within 3 hexes (replace with your board system if needed)
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        UnitAI farthest = null;
        float maxDist = 0f;

        foreach (var u in allUnits)
        {
            if (u == unitAI || !u.isAlive || u.team == unitAI.team) continue;
            float dist = Vector3.Distance(unitAI.transform.position, u.transform.position);
            if (dist <= 3f && dist > maxDist)
            {
                maxDist = dist;
                farthest = u;
            }
        }

        if (farthest == null) yield break;

        // Trigger leap animation
        if (unitAI.animator) unitAI.animator.SetTrigger("LeapTrigger");

        // Simulate leap delay (0.5s)
        yield return new WaitForSeconds(0.5f);

        // Slam damage
        float damage = slamDamagePerStar[Mathf.Clamp(unitAI.starLevel - 1, 0, slamDamagePerStar.Length - 1)]
                       + unitAI.attackDamage;
        farthest.TakeDamage(damage);

        Debug.Log($"{unitAI.unitName} leapt and slammed {farthest.unitName} for {damage}!");

        // Attack speed buff
        StartCoroutine(TemporaryAttackSpeedBuff(1.5f, 4f)); // 50% boost for 4s
    }

    private IEnumerator TemporaryAttackSpeedBuff(float multiplier, float duration)
    {
        float original = unitAI.attackSpeed;
        unitAI.attackSpeed *= multiplier;

        yield return new WaitForSeconds(duration);

        unitAI.attackSpeed = original;
    }
}
