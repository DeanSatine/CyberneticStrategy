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

    [Header("Timings")]
    public float leapDuration = 0.5f;
    public float slamDuration = 0.7f;
    public float leapHeight = 2f;
    public float maxLeapRange = 3f; // in world units (approx 3 hexes)
    public float attackSpeedBuff = 1.5f; // 50% attack speed increase
    public float buffDuration = 4f; // 4 seconds

    [Header("VFX")]
    public GameObject slamVFX; // ✅ Only VFX needed - plays at feet when landing

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
    }

    // Called by UnitAI
    public void Cast(UnitAI _ignored)
    {
        // Find the FARTHEST enemy within range, not nearest
        UnitAI target = FindFarthestEnemyInRange(maxLeapRange);

        // Skip if no target or if target is benched
        if (target == null || target.currentState == UnitState.Bench)
        {
            Debug.Log($"{unitAI.unitName} tried to leap, but no valid enemies within {maxLeapRange}!");
            return;
        }

        Debug.Log($"🦘 {unitAI.unitName} leaping to FARTHEST enemy: {target.unitName}");
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
        if (target == null || !target.isAlive || target.currentState == UnitState.Bench) yield break;

        // Starting position
        Vector3 startPos = unitAI.transform.position;

        // Try to pick a landing hex 1 hex away from the target
        HexTile enemyTile = target.currentTile;
        HexTile landingTile = null;
        if (enemyTile != null && BoardManager.Instance != null)
        {
            landingTile = BoardManager.Instance.GetClosestFreeNeighbor(enemyTile, unitAI.currentTile);
        }

        // Compute fallback end position if no landing tile found
        Vector3 dir = (target.transform.position - startPos);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = unitAI.transform.forward; // fallback direction

        dir.Normalize();
        float stopDistance = 1.2f; // how far to stay away from enemy if no hex is available

        Vector3 endPos;
        if (landingTile != null)
        {
            endPos = landingTile.transform.position;
        }
        else
        {
            // Land slightly in front of the enemy (never inside)
            endPos = target.transform.position - dir * stopDistance;
        }

        // Make sure endPos is at the same vertical level as start
        endPos.y = startPos.y;

        // Play leap animation
        if (unitAI.animator) unitAI.animator.SetTrigger("LeapTrigger");

        Debug.Log($"🚀 {unitAI.unitName} leaping from {startPos} to {endPos} (targeting {target.unitName})");

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

        // Snap to final landing location
        unitAI.transform.position = endPos;

        // ✅ FIXED: Deal damage and spawn VFX immediately when landing (no delay!)
        if (target != null && target.isAlive)
        {
            // Deal damage immediately
            float damage = slamDamagePerStar[Mathf.Clamp(unitAI.starLevel - 1, 0, slamDamagePerStar.Length - 1)]
                           + unitAI.attackDamage;
            target.TakeDamage(damage);

            // ✅ Spawn VFX immediately at feet level when landing and dealing damage
            if (slamVFX != null)
            {
                Vector3 slamVFXPos = unitAI.transform.position; // KillSwitch's feet position
                slamVFXPos.y = 0.1f; // Slightly above ground level (feet level)
                var slamEffect = Instantiate(slamVFX, slamVFXPos, Quaternion.identity);
                Destroy(slamEffect, 3f);

                Debug.Log($"💥 Slam VFX spawned immediately at feet level: {slamVFXPos}");
            }

            Debug.Log($"💥 {unitAI.unitName} landed and slammed {target.unitName} for {damage} damage!");

            // If target is dead, clear it so UnitAI will find a new one
            if (!target.isAlive)
            {
                unitAI.currentTarget = null;
            }
            else
            {
                unitAI.currentTarget = target.transform; // keep locked if alive
            }
        }
        else
        {
            Debug.Log($"⚠️ {unitAI.unitName} slam target became invalid during leap");
        }

        // Make the jumped enemy the new official target
        unitAI.currentTarget = target.transform;

        // Update tile occupancy if we landed on a tile
        if (landingTile != null)
        {
            if (unitAI.currentTile != null && unitAI.currentTile.occupyingUnit == unitAI)
                unitAI.currentTile.occupyingUnit = null;

            unitAI.currentTile = landingTile;
            landingTile.occupyingUnit = unitAI;
        }
        else
        {
            // Fallback: nearest tile
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

        // Face the target horizontally
        Vector3 lookAt = target.transform.position;
        lookAt.y = unitAI.transform.position.y;
        unitAI.transform.LookAt(lookAt);

        // ✅ Play slam animation AFTER damage (for visual effect only)
        if (unitAI.animator) unitAI.animator.SetTrigger("AbilityTrigger");

        // ✅ Wait for animation to finish (but damage already dealt)
        yield return new WaitForSeconds(slamDuration);

        // Proper attack speed buff (50% = 1.5x multiplier)
        StartCoroutine(TemporaryAttackSpeedBuff(attackSpeedBuff, buffDuration));
    }


    private IEnumerator TemporaryAttackSpeedBuff(float multiplier, float duration)
    {
        float original = unitAI.attackSpeed;
        unitAI.attackSpeed *= multiplier;

        Debug.Log($"🚀 {unitAI.unitName} gained {(multiplier - 1f) * 100:F0}% attack speed for {duration} seconds!");

        yield return new WaitForSeconds(duration);

        unitAI.attackSpeed = original;
        Debug.Log($"⏰ {unitAI.unitName} attack speed buff expired");
    }

    // Find FARTHEST enemy within range instead of nearest
    private UnitAI FindFarthestEnemyInRange(float range)
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        UnitAI farthest = null;
        float maxDist = 0f;

        foreach (var unit in allUnits)
        {
            if (unit == unitAI || !unit.isAlive || unit.team == unitAI.team || unit.currentState == UnitState.Bench)
                continue;

            float dist = Vector3.Distance(unitAI.transform.position, unit.transform.position);

            // Find the farthest enemy within range
            if (dist > maxDist && dist <= range)
            {
                maxDist = dist;
                farthest = unit;
            }
        }

        if (farthest != null)
        {
            Debug.Log($"🎯 {unitAI.unitName} found farthest enemy: {farthest.unitName} at distance {maxDist:F1}");
        }

        return farthest;
    }
}
