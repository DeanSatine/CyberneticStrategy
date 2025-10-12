
using UnityEngine;

public class HaymakerCloneStarLock : MonoBehaviour
{
    private UnitAI unitAI;
    private int lockedStarLevel;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        if (unitAI != null)
        {
            lockedStarLevel = unitAI.starLevel;
        }
    }

    private void Update()
    {
        // Prevent star level changes on clones
        if (unitAI != null && unitAI.starLevel != lockedStarLevel)
        {
            Debug.Log($"🚫 Prevented star upgrade on clone {unitAI.unitName}: {unitAI.starLevel} → {lockedStarLevel}");
            unitAI.starLevel = lockedStarLevel;
        }
    }

    // Prevent clone from being used in combinations
    private void OnTriggerEnter(Collider other)
    {
        // If this clone touches another unit of same type, prevent combination
        UnitAI otherUnit = other.GetComponent<UnitAI>();
        if (otherUnit != null && otherUnit.unitName.Contains("Haymaker"))
        {
            // Move away to prevent accidental combinations
            Vector3 avoidDirection = (transform.position - other.transform.position).normalized;
            transform.position += avoidDirection * 0.5f;
        }
    }
}
