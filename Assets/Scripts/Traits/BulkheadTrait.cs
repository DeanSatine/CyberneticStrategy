using UnityEngine;

public class BulkheadTrait : MonoBehaviour
{
    [HideInInspector] public float bonusHealthPercent;
    [HideInInspector] public float deathSharePercent;

    private UnitAI unitAI;
    private bool applied = false;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
    }

    private void Start()
    {
        // Apply bonus health only once
        if (!applied)
        {
            float bonus = unitAI.maxHealth * bonusHealthPercent;
            unitAI.maxHealth += bonus;
            unitAI.currentHealth += bonus;
            applied = true;
        }

        // Hook into death
        UnitAI.OnAnyUnitDeath += OnUnitDeath;
    }

    private void OnDestroy()
    {
        UnitAI.OnAnyUnitDeath -= OnUnitDeath;
    }

    private void OnUnitDeath(UnitAI deadUnit)
    {
        if (deadUnit == unitAI) // this Bulkhead died
        {
            UnitAI nearest = FindNearestAlly();
            if (nearest != null)
            {
                float healthShare = unitAI.maxHealth * deathSharePercent;
                nearest.currentHealth = Mathf.Min(nearest.maxHealth, nearest.currentHealth + healthShare);
                Debug.Log($"{unitAI.unitName} shared {healthShare} HP with {nearest.unitName}");
            }
        }
    }

    private UnitAI FindNearestAlly()
    {
        UnitAI[] all = FindObjectsOfType<UnitAI>();
        UnitAI nearest = null;
        float minDist = Mathf.Infinity;

        foreach (var ally in all)
        {
            if (ally == unitAI || !ally.isAlive) continue;
            if (ally.team != unitAI.team) continue;

            float dist = Vector3.Distance(unitAI.transform.position, ally.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = ally;
            }
        }

        return nearest;
    }
}
