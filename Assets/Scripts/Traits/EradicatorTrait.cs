using UnityEngine;

public class EradicatorTrait : MonoBehaviour
{
    [HideInInspector] public float executeThreshold; // set by TraitManager
    [HideInInspector] public GameObject pressPrefab; // prefab for the hydraulic press

    private UnitAI unitAI;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
    }

    private void Update()
    {
        if (!unitAI.isAlive || unitAI.currentState != UnitAI.UnitState.Combat) return;

        UnitAI[] all = FindObjectsOfType<UnitAI>();
        foreach (var enemy in all)
        {
            if (enemy.team != unitAI.team && enemy.isAlive)
            {
                float hpPercent = enemy.currentHealth / enemy.maxHealth;
                if (hpPercent <= executeThreshold)
                {
                    if (pressPrefab)
                    {
                        Instantiate(pressPrefab, enemy.transform.position, Quaternion.identity);
                    }

                    enemy.TakeDamage(99999); // guaranteed kill
                    Debug.Log($"Eradicator execution! {enemy.unitName} crushed by press.");
                }
            }
        }
    }
}
