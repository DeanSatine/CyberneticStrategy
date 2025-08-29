using UnityEngine;
using static UnitAI;

public class BOPAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;
    public float[] damageAmpPerStar = { 30, 40, 50 };
    public float[] chestBuffPercent = { 0.05f, 0.05f, 0.05f }; // flat +5% per cast (can adjust per star)

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
    }

    public void Cast()
    {
        // ✅ Buff self health
        float buffAmount = unitAI.maxHealth * chestBuffPercent[Mathf.Clamp(unitAI.starLevel - 1, 0, chestBuffPercent.Length - 1)];
        unitAI.maxHealth += buffAmount;
        unitAI.currentHealth += buffAmount;

        Debug.Log($"{unitAI.unitName} pounds chest! Gained {buffAmount} max HP (now {unitAI.maxHealth}).");

        UnitAI target = unitAI.GetCurrentTarget();
        if (target != null && target.isAlive)
        {
            float damageAmp = damageAmpPerStar[Mathf.Clamp(unitAI.starLevel - 1, 0, damageAmpPerStar.Length - 1)];
            float damage = (unitAI.maxHealth * 0.2f) + damageAmp;

            target.TakeDamage(damage);
        }
    }
}
