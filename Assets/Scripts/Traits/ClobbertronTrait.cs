using UnityEngine;

public class ClobbertronTrait : MonoBehaviour
{
    [HideInInspector] public float bonusArmor = 10f;
    [HideInInspector] public float bonusDamageAmp = 0.1f;
    [HideInInspector] public float crashRadius = 2f;
    [HideInInspector] public float crashDamage = 200f;

    private UnitAI unitAI;
    private bool buffApplied = false;
    private bool crashed = false;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
    }

    private void Update()
    {
        if (!unitAI.isAlive) return;

        if (!buffApplied)
        {
            unitAI.armor += bonusArmor;
            unitAI.attackDamage *= (1f + bonusDamageAmp);
            buffApplied = true;
        }

        if (!crashed && unitAI.currentHealth <= unitAI.maxHealth * 0.5f)
        {
            crashed = true;
            DoCrash();
        }
    }

    private void DoCrash()
    {
        Debug.Log($"{unitAI.unitName} CRASHED down!");

        Collider[] hits = Physics.OverlapSphere(unitAI.transform.position, crashRadius);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<UnitAI>();
            if (enemy != null && enemy.team != unitAI.team && enemy.isAlive)
            {
                enemy.TakeDamage(crashDamage);
            }
        }

        // Double their bonus permanently
        unitAI.armor += bonusArmor;
        unitAI.attackDamage *= (1f + bonusDamageAmp);
    }
}
