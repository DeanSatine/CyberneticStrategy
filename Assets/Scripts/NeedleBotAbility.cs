using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeedleBotAbility : MonoBehaviour
{
    private UnitAI unitAI;

    [Header("Ability Stats")]
    public int baseNeedleCount = 4;      // starts at 4
    public int needlesPerCast;           // dynamic
    public float[] damagePerStar = { 100f, 150f, 175f };

    private int totalNeedlesThrown = 0;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        needlesPerCast = baseNeedleCount;
    }

    public void Cast()
    {
        StartCoroutine(FireNeedles());
    }

    private IEnumerator FireNeedles()
    {
        // Find the 2 nearest enemies
        List<UnitAI> targets = FindNearestEnemies(2);

        if (targets.Count == 0) yield break;

        int half = needlesPerCast / targets.Count;
        float damage = damagePerStar[Mathf.Clamp(unitAI.starLevel - 1, 0, damagePerStar.Length - 1)];

        // Throw projectiles one by one for visual pacing
        foreach (var target in targets)
        {
            for (int i = 0; i < half; i++)
            {
                if (target != null && target.isAlive)
                {
                    target.TakeDamage(damage + unitAI.attackDamage); // uses Damage Amp
                    Debug.Log($"{unitAI.unitName} threw a needle at {target.unitName} for {damage} dmg!");

                    totalNeedlesThrown++;
                    if (totalNeedlesThrown % 10 == 0)
                    {
                        needlesPerCast++;
                        Debug.Log($"{unitAI.unitName} permanently increased needle count! Now: {needlesPerCast}");
                    }
                }

                yield return new WaitForSeconds(0.1f); // small delay between throws
            }
        }
    }

    private List<UnitAI> FindNearestEnemies(int count)
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        List<UnitAI> enemies = new List<UnitAI>();

        foreach (var u in allUnits)
        {
            if (u != unitAI && u.isAlive) // TODO: team check
            {
                enemies.Add(u);
            }
        }

        enemies.Sort((a, b) =>
            Vector3.Distance(unitAI.transform.position, a.transform.position)
            .CompareTo(Vector3.Distance(unitAI.transform.position, b.transform.position))
        );

        return enemies.GetRange(0, Mathf.Min(count, enemies.Count));
    }
}
