using UnityEngine;

public class StrikebyteTrait : MonoBehaviour
{
    [HideInInspector] public float rampDamageAmp = 1f;  // +1 AD per attack
    [HideInInspector] public float maxDamageAmp = 15f;  // max +15 AD
    [HideInInspector] public float rampAS = 0.05f;      // +5% AS per attack
    [HideInInspector] public float maxAS = 0.30f;       // max +30% AS

    private UnitAI unitAI;
    private UnitAI lastTarget;
    private float bonusAD = 0f;
    private float bonusAS = 0f;
    private float baseAttackDamage;
    private float baseAttackSpeed;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        baseAttackDamage = unitAI.attackDamage;
        baseAttackSpeed = unitAI.attackSpeed;
    }

    private void Update()
    {
        if (!unitAI.isAlive || unitAI.currentTarget == null) return;

        UnitAI target = unitAI.GetCurrentTarget();

        if (target != lastTarget)
        {
            ResetRamp();
            lastTarget = target;
        }
    }

    private void ResetRamp()
    {
        bonusAD = 0f;
        bonusAS = 0f;
        unitAI.attackDamage = baseAttackDamage;
        unitAI.attackSpeed = baseAttackSpeed;
    }

    // ✅ Hook this into UnitAI's OnAttack event
    public void OnAttack(UnitAI enemy)
    {
        // Additive ramping
        bonusAD = Mathf.Min(maxDamageAmp, bonusAD + rampDamageAmp);
        bonusAS = Mathf.Min(maxAS, bonusAS + rampAS);

        unitAI.attackDamage = baseAttackDamage + bonusAD;
        unitAI.attackSpeed = baseAttackSpeed + bonusAS;

        Debug.Log($"{unitAI.unitName} ramped: +{bonusAD} AD, +{bonusAS * 100f}% AS");
    }
    private void OnEnable()
    {
        if (unitAI != null)
            unitAI.OnAttackEvent += OnAttack;
    }

    private void OnDisable()
    {
        if (unitAI != null)
            unitAI.OnAttackEvent -= OnAttack;
    }

}
