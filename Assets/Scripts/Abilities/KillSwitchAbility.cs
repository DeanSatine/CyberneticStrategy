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
    public float maxLeapRange = 3f; // in world units (approx 3 hexes)

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
    }

    // Called by UnitAI
    public void Cast(UnitAI _ignored)
    {
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
        if (target == null) yield break;

        // starting position
        Vector3 startPos = unitAI.transform.position;
        startPos.y = startPos.y; // keep consistent

        // Try to pick a landing hex 1 hex away from the target
        HexTile enemyTile = target.currentTile;
        HexTile landingTile = null;
        if (enemyTile != null && BoardManager.Instance != null)
        {
            // BoardManager should provide GetClosestFreeNeighbor(enemyTile, fromTile)
            landingTile = BoardManager.Instance.GetClosestFreeNeighbor(enemyTile, unitAI.currentTile);
        }

        // compute fallback end position if no landing tile found
        Vector3 dir = (target.transform.position - startPos);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = (unitAI.transform.forward); // fallback direction

        dir.Normalize();
        float stopDistance = 1.2f; // how far to stay away from enemy if no hex is available (tune as needed)

        Vector3 endPos;
        if (landingTile != null)
        {
            endPos = landingTile.transform.position;
        }
        else
        {
            // land slightly in front of the enemy (never inside)
            endPos = target.transform.position - dir * stopDistance;
        }

        // make sure endPos is at the same vertical level as start (no sinking / floating)
        endPos.y = startPos.y;

        // play leap animation
        if (unitAI.animator) unitAI.animator.SetTrigger("LeapTrigger");

        // Smooth arc from start -> end
        float elapsed = 0f;
        while (elapsed < leapDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / leapDuration);

            // Lerp horizontally and add vertical sinusoidal arc
            Vector3 horizontal = Vector3.Lerp(startPos, endPos, t);
            horizontal.y += Mathf.Sin(t * Mathf.PI) * leapHeight;

            unitAI.transform.position = horizontal;
            yield return null;
        }

        // Snap to final landing location (prevents small penetrations)
        unitAI.transform.position = endPos;

        // update tile occupancy if we landed on a tile
        if (landingTile != null)
        {
            // clear previous tile occupancy cleanly
            if (unitAI.currentTile != null && unitAI.currentTile.occupyingUnit == unitAI)
                unitAI.currentTile.occupyingUnit = null;

            unitAI.currentTile = landingTile;
            landingTile.occupyingUnit = unitAI;
        }
        else
        {
            // If we used fallback position, try to assign to nearest tile to keep board logic consistent
            if (BoardManager.Instance != null)
            {
                HexTile nearest = BoardManager.Instance.GetTileFromWorld(endPos);
                if (nearest != null)
                {
                    unitAI.ClearTile();
                    unitAI.AssignToTile(nearest);
                }
            }
        }

        // face the target horizontally
        Vector3 lookAt = target.transform.position;
        lookAt.y = unitAI.transform.position.y;
        unitAI.transform.LookAt(lookAt);

        // slam animation
        if (unitAI.animator) unitAI.animator.SetTrigger("AbilityTrigger");

        // wait for slam timing (so animation can play)
        yield return new WaitForSeconds(slamDuration);

        // Deal damage to the primary target
        float damage = slamDamagePerStar[Mathf.Clamp(unitAI.starLevel - 1, 0, slamDamagePerStar.Length - 1)]
                       + unitAI.attackDamage;

        // If we landed one hex away, target is still hit by slam (keeps original behavior)
        if (target != null && target.isAlive)
        {
            target.TakeDamage(damage);
            Debug.Log($"{unitAI.unitName} leapt in front of and slammed {target.unitName} for {damage}!");
        }

        // temporary buff
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
            if (unit.team == unitAI.team) continue;

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
