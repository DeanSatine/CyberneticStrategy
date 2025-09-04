using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class NeedleBotAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;

    [Header("Ability Stats")]
    public int baseNeedleCount = 4;
    public int needlesPerCast;
    public float[] damagePerStar = { 100f, 150f, 175f };

    private int totalNeedlesThrown = 0;

    [Header("Timings")]
    public float startDelay = 0.25f;   // delay before throwing
    public float throwInterval = 0.1f; // delay between throws
    public float abilityDuration = 1.5f; // locks auto-attacks

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        needlesPerCast = baseNeedleCount;
    }

    public void Cast(UnitAI target)
    {
        if (!unitAI.isAlive) return;

        // 🔹 Stop attacking during cast
        unitAI.canAttack = false;
        unitAI.canMove = false;

        if (unitAI.animator != null)
            unitAI.animator.SetTrigger("AbilityTrigger");

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

        int half = Mathf.Max(1, needlesPerCast / targets.Count);
        float damage = damagePerStar[Mathf.Clamp(unitAI.starLevel - 1, 0, damagePerStar.Length - 1)];

        foreach (var target in targets)
        {
            for (int i = 0; i < half; i++)
            {
                if (target != null && target.isAlive)
                {
                    target.TakeDamage(damage + unitAI.attackDamage);
                    Debug.Log($"{unitAI.unitName} threw a needle at {target.unitName} for {damage} dmg!");

                    totalNeedlesThrown++;
                    if (totalNeedlesThrown % 10 == 0)
                    {
                        needlesPerCast++;
                        Debug.Log($"{unitAI.unitName} permanently increased needle count! Now: {needlesPerCast}");
                    }
                }

                yield return new WaitForSeconds(throwInterval);
            }
        }

        // 🔹 Small buffer so animation finishes cleanly
        yield return new WaitForSeconds(0.2f);

        EndCast();
    }

    private void EndCast()
    {
        unitAI.currentMana = 0f;
        if (unitAI.unitUIPrefab != null)
            unitAI.GetComponentInChildren<UnitUI>()?.UpdateMana(unitAI.currentMana);

        // 🔹 Allow attacking again
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
