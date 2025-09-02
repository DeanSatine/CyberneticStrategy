using UnityEngine;
using static UnitAI;
using System.Collections.Generic;

public class ManaDriveAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;

    [Header("Ability Stats")]
    public float[] damagePerStar = { 40, 60, 80 };   // scales with star level
    public float range = 4f;                         // max cast range
    public float splashRadius = 2f;                  // AoE radius around target

    [Header("Optional VFX")]
    public GameObject castVFX;       // plays at ManaDrive when casting
    public GameObject impactVFX;     // plays at main target
    public GameObject splashVFX;     // plays once for splash AoE (optional)

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
    }

    // Called by UnitAI when mana is full
    public void Cast(UnitAI target)
    {
        if (unitAI.animator != null)
            unitAI.animator.SetTrigger("AbilityTrigger");

        // Optional: casting VFX at self
        if (castVFX != null)
            Instantiate(castVFX, transform.position, Quaternion.identity);
    }


    // ⚡ Animation Event: Overload impact frame
    public void DoOverloadDamage()
    {
        UnitAI mainTarget = unitAI.GetCurrentTarget();
        if (mainTarget == null || !mainTarget.isAlive) return;

        float dist = Vector3.Distance(transform.position, mainTarget.transform.position);
        if (dist > range) return;

        float damage = damagePerStar[Mathf.Clamp(unitAI.starLevel - 1, 0, damagePerStar.Length - 1)];

        // Damage the main target
        mainTarget.TakeDamage(damage);
        if (impactVFX != null)
            Instantiate(impactVFX, mainTarget.transform.position, Quaternion.identity);

        Debug.Log($"{unitAI.unitName} overloads {mainTarget.unitName} for {damage} damage!");

        // Splash to nearby enemies
        Collider[] hits = Physics.OverlapSphere(mainTarget.transform.position, splashRadius);
        foreach (Collider hit in hits)
        {
            UnitAI enemy = hit.GetComponent<UnitAI>();
            if (enemy != null && enemy != mainTarget && enemy.teamID != unitAI.teamID && enemy.isAlive)
            {
                enemy.TakeDamage(damage * 0.5f); // splash deals 50% dmg
                Debug.Log($"  -> {enemy.unitName} takes {damage * 0.5f} splash damage!");
            }
        }

        // Optional: splash VFX
        if (splashVFX != null)
            Instantiate(splashVFX, mainTarget.transform.position, Quaternion.identity);

        // Reset mana
        unitAI.currentMana = 0;
    }
}
