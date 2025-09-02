using UnityEngine;
using static UnitAI;

public class BOPAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;

    [Header("Ability Stats")]
    public float[] damageAmpPerStar = { 30, 40, 50 };
    public float[] chestBuffPercent = { 0.05f, 0.05f, 0.05f }; // +5% per cast

    [Header("Optional VFX")]
    public GameObject chestVFX;
    public GameObject strikeVFX;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
    }

    // Called by UnitAI when mana is full
    public void Cast(UnitAI target)
    {
        // Trigger chest pound animation
        if (unitAI.animator != null)
            unitAI.animator.SetTrigger("AbilityTrigger");
    }


    // 👊 Animation Event: Chest Pound frame
    public void ApplyChestBuff()
    {
        float buffAmount = unitAI.maxHealth * chestBuffPercent[Mathf.Clamp(unitAI.starLevel - 1, 0, chestBuffPercent.Length - 1)];
        unitAI.maxHealth += buffAmount;
        unitAI.currentHealth += buffAmount;

        if (chestVFX != null)
            Instantiate(chestVFX, transform.position, Quaternion.identity);

        Debug.Log($"{unitAI.unitName} pounds chest! +{buffAmount} max HP (now {unitAI.maxHealth}).");
    }

    // 🔨 Animation Event: Bonk Strike frame
    public void DoBonkDamage()
    {
        UnitAI target = unitAI.GetCurrentTarget();
        if (target != null && target.isAlive)
        {
            float damageAmp = damageAmpPerStar[Mathf.Clamp(unitAI.starLevel - 1, 0, damageAmpPerStar.Length - 1)];
            float damage = (unitAI.maxHealth * 0.2f) + damageAmp;

            target.TakeDamage(damage);

            if (strikeVFX != null)
                Instantiate(strikeVFX, target.transform.position, Quaternion.identity);

            Debug.Log($"{unitAI.unitName} BONKS {target.unitName} for {damage} damage!");
        }
    }


}
