using UnityEngine;
using static UnitAI;

public class BOPAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;

    [Header("Ability Stats")]
    public float[] damageAmpPerStar = { 30, 40, 50 };
    public float[] chestBuffPercent = { 0.1f, 0.12f, 0.15f }; // +5% per cast

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
        float buffAmount = unitAI.maxHealth * chestBuffPercent[
            Mathf.Clamp(unitAI.starLevel - 1, 0, chestBuffPercent.Length - 1)
        ];

        // store as a bonus so recalculations won't wipe it
        unitAI.bonusMaxHealth += buffAmount;

        // recompute effective max (updates unitAI.maxHealth and the unit UI)
        unitAI.RecalculateMaxHealth();

        // heal current health by the same amount
        unitAI.currentHealth += buffAmount;
        if (unitAI.currentHealth > unitAI.maxHealth) unitAI.currentHealth = unitAI.maxHealth;

        // ensure UI shows the new current health
        if (unitAI.ui != null)
            unitAI.ui.UpdateHealth(unitAI.currentHealth);

        // refresh Unit Info Panel if it's showing this unit (see manager helper below)
        if (UnitInfoPanelManager.Instance != null)
            UnitInfoPanelManager.Instance.RefreshActivePanelIfMatches(unitAI);

        if (chestVFX != null)
            Instantiate(chestVFX, transform.position, Quaternion.identity);

        Debug.Log($"{unitAI.unitName} pounds chest! +{buffAmount:F1} max HP (now {unitAI.maxHealth:F1}).");
    }



    // 🔨 Animation Event: Bonk Strike frame
    public void DoBonkDamage()
    {
        UnitAI target = unitAI.GetCurrentTarget();
        if (target != null && target.isAlive && target.currentState != UnitState.Bench)
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
