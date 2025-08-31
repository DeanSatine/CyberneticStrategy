using UnityEngine;

public class StrikebyteTrait : MonoBehaviour
{
    [HideInInspector] public float rampDamageAmp = 1f;
    [HideInInspector] public float maxDamageAmp = 15f;
    [HideInInspector] public float rampAS = 0.05f;
    [HideInInspector] public float maxAS = 0.30f;

    private UnitAI unitAI;
    private UnitAI lastTarget;
    private float currentAmp = 0f;
    private float currentAS = 0f;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
    }

    private void Update()
    {
        if (!unitAI.isAlive || unitAI.currentTarget == null) return;

        UnitAI target = unitAI.GetCurrentTarget();

        if (target != lastTarget)
        {
            // reset ramp
            currentAmp = 0f;
            currentAS = 0f;
            unitAI.attackDamage = 10f; // reset base values (TODO: store base in UnitAI)
            unitAI.attackSpeed = 1f;
            lastTarget = target;
        }
    }

    // Hook into UnitAI attack event
    public void OnAttack(UnitAI enemy)
    {
        currentAmp = Mathf.Min(maxDamageAmp, currentAmp + rampDamageAmp);
        currentAS = Mathf.Min(maxAS, currentAS + rampAS);

        unitAI.attackDamage *= (1f + currentAmp / 100f);
        unitAI.attackSpeed += currentAS;

        Debug.Log($"{unitAI.unitName} ramped: +{currentAmp}% DamageAmp, +{currentAS * 100f}% AS");
    }
}
