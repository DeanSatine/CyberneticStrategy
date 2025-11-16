using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class ApheliosAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;

    [Header("Passive - Alternating Attacks")]
    [Tooltip("Damage dealt in cone attack per star level")]
    public float[] coneDamage = { 40f, 60f, 90f };

    [Tooltip("Healing amount per star level")]
    public float[] healAmount = { 50f, 80f, 120f };

    [Tooltip("Cone angle for damage attack (degrees)")]
    public float coneAngle = 60f;

    [Tooltip("Cone range in hexes")]
    public float coneRange = 2f;

    private bool isNextAttackCone = true;

    [Header("Active Ability - Twin Shot + Blast")]
    [Tooltip("Damage per shot on highest HP target")]
    public float[] shotDamage = { 80f, 120f, 180f };

    [Tooltip("Blast damage on clump")]
    public float[] blastDamage = { 150f, 225f, 340f };

    [Tooltip("Blast radius in hexes")]
    public float blastRadius = 2f;

    private UnitAI activeAbilityTarget;
    private Vector3 blastTargetPosition;
    private bool isPerformingAbility = false;

    [Header("VFX")]
    public GameObject coneAttackVFX;
    public GameObject healVFX;
    public GameObject shotVFX;
    public GameObject blastVFX;

    [Header("Audio")]
    public AudioClip coneAttackSound;
    public AudioClip healSound;
    public AudioClip shotSound;
    public AudioClip blastSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    private AudioSource audioSource;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        SetupAudio();
    }

    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.7f;
        audioSource.volume = volume;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    public void DealAutoAttackDamage()
    {
        if (isPerformingAbility)
        {
            unitAI.DealAutoAttackDamage();
            return;
        }

        if (isNextAttackCone)
        {
            OnConeAttackEvent();
        }
        else
        {
            OnHealAttackEvent();
        }

        isNextAttackCone = !isNextAttackCone;
    }

    public void OnConeAttackEvent()
    {
        PlaySound(coneAttackSound);

        int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, coneDamage.Length - 1);
        float damage = coneDamage[starIndex];

        Vector3 forward = transform.forward;
        List<UnitAI> enemiesInCone = FindEnemiesInCone(forward, coneRange, coneAngle);

        if (coneAttackVFX != null)
        {
            Vector3 vfxPos = transform.position + Vector3.up * 1f + forward * (coneRange * 0.5f);
            GameObject vfx = Instantiate(coneAttackVFX, vfxPos, Quaternion.LookRotation(forward));
            Destroy(vfx, 2f);
        }

        foreach (UnitAI enemy in enemiesInCone)
        {
            unitAI.DealPhysicalDamage(enemy, damage);
        }

        unitAI.GainMana(10);

        Debug.Log($"🌟 {unitAI.unitName} cone attack hit {enemiesInCone.Count} enemies for {damage} damage each!");
    }

    public void OnHealAttackEvent()
    {
        PlaySound(healSound);

        int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, healAmount.Length - 1);
        float healing = healAmount[starIndex];

        HealSelf(healing);

        unitAI.GainMana(10);

        Debug.Log($"💚 {unitAI.unitName} healed {healing} HP!");
    }

    private void HealSelf(float healAmount)
    {
        if (!unitAI.isAlive) return;

        float actualHealing = Mathf.Min(healAmount, unitAI.maxHealth - unitAI.currentHealth);

        if (actualHealing <= 0) return;

        unitAI.currentHealth = Mathf.Min(unitAI.maxHealth, unitAI.currentHealth + actualHealing);

        if (unitAI.ui != null)
        {
            unitAI.ui.UpdateHealth(unitAI.currentHealth);
        }

        if (healVFX != null)
        {
            GameObject healEffect = Instantiate(healVFX, transform.position + Vector3.up * 1.5f, Quaternion.identity);
            Destroy(healEffect, 2f);
        }

        unitAI.RaiseHealReceivedEvent(actualHealing);
    }

    private List<UnitAI> FindEnemiesInCone(Vector3 direction, float range, float angle)
    {
        List<UnitAI> enemiesInCone = new List<UnitAI>();
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();

        foreach (UnitAI unit in allUnits)
        {
            if (unit == unitAI || !unit.isAlive || unit.team == unitAI.team || unit.currentState == UnitState.Bench)
                continue;

            Vector3 toEnemy = unit.transform.position - transform.position;
            toEnemy.y = 0f;
            float distance = toEnemy.magnitude;

            if (distance <= range)
            {
                float angleToEnemy = Vector3.Angle(direction, toEnemy.normalized);

                if (angleToEnemy <= angle / 2f)
                {
                    enemiesInCone.Add(unit);
                }
            }
        }

        return enemiesInCone;
    }

    public void Cast(UnitAI target)
    {
        if (!unitAI.isAlive || isPerformingAbility) return;

        unitAI.currentMana = 0f;
        if (unitAI.ui != null)
        {
            unitAI.ui.UpdateMana(0f);
        }

        activeAbilityTarget = FindHighestHPEnemy();

        if (activeAbilityTarget == null)
        {
            Debug.LogWarning($"[Aphelios] No valid target for ability!");
            return;
        }

        blastTargetPosition = FindLargestClumpPosition();

        isPerformingAbility = true;

        Vector3 direction = (activeAbilityTarget.transform.position - transform.position).normalized;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        if (unitAI.animator)
        {
            unitAI.animator.SetTrigger("TwinShot");
        }
        else
        {
            OnFirstShotEvent();
        }

        Debug.Log($"🎯 {unitAI.unitName} starts Twin Shot ability at {activeAbilityTarget.unitName}!");
    }

    public void OnFirstShotEvent()
    {
        if (activeAbilityTarget == null || !activeAbilityTarget.isAlive)
        {
            activeAbilityTarget = FindHighestHPEnemy();
        }

        if (activeAbilityTarget != null && activeAbilityTarget.isAlive)
        {
            PlaySound(shotSound);

            int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, shotDamage.Length - 1);
            float damage = shotDamage[starIndex];

            unitAI.DealPhysicalDamage(activeAbilityTarget, damage);

            if (shotVFX != null)
            {
                Vector3 vfxPos = activeAbilityTarget.transform.position + Vector3.up * 1f;
                GameObject vfx = Instantiate(shotVFX, vfxPos, Quaternion.identity);
                Destroy(vfx, 1f);
            }

            Debug.Log($"🔫 {unitAI.unitName} first shot hit {activeAbilityTarget.unitName} for {damage} damage!");
        }
    }

    public void OnSecondShotEvent()
    {
        if (activeAbilityTarget == null || !activeAbilityTarget.isAlive)
        {
            activeAbilityTarget = FindHighestHPEnemy();
        }

        if (activeAbilityTarget != null && activeAbilityTarget.isAlive)
        {
            PlaySound(shotSound);

            int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, shotDamage.Length - 1);
            float damage = shotDamage[starIndex];

            unitAI.DealPhysicalDamage(activeAbilityTarget, damage);

            if (shotVFX != null)
            {
                Vector3 vfxPos = activeAbilityTarget.transform.position + Vector3.up * 1f;
                GameObject vfx = Instantiate(shotVFX, vfxPos, Quaternion.identity);
                Destroy(vfx, 1f);
            }

            Debug.Log($"🔫 {unitAI.unitName} second shot hit {activeAbilityTarget.unitName} for {damage} damage!");
        }

        if (unitAI.animator)
        {
            unitAI.animator.SetTrigger("Blast");
        }
        else
        {
            OnBlastEvent();
        }
    }

    public void OnBlastEvent()
    {
        PlaySound(blastSound);

        blastTargetPosition = FindLargestClumpPosition();

        int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, blastDamage.Length - 1);
        float damage = blastDamage[starIndex];

        List<UnitAI> enemiesInBlast = FindEnemiesInRadius(blastTargetPosition, blastRadius);

        if (blastVFX != null)
        {
            GameObject vfx = Instantiate(blastVFX, blastTargetPosition + Vector3.up * 0.5f, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        foreach (UnitAI enemy in enemiesInBlast)
        {
            unitAI.DealPhysicalDamage(enemy, damage);
        }

        Debug.Log($"💥 {unitAI.unitName} blast hit {enemiesInBlast.Count} enemies for {damage} damage each!");

        FinishAbility();
    }

    private void FinishAbility()
    {
        isPerformingAbility = false;
        activeAbilityTarget = null;
    }

    private UnitAI FindHighestHPEnemy()
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        UnitAI highestHPEnemy = null;
        float highestHP = 0f;

        foreach (UnitAI unit in allUnits)
        {
            if (unit == unitAI || !unit.isAlive || unit.team == unitAI.team || unit.currentState == UnitState.Bench)
                continue;

            if (unit.currentHealth > highestHP)
            {
                highestHP = unit.currentHealth;
                highestHPEnemy = unit;
            }
        }

        return highestHPEnemy;
    }

    private Vector3 FindLargestClumpPosition()
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        List<UnitAI> enemies = new List<UnitAI>();

        foreach (UnitAI unit in allUnits)
        {
            if (unit == unitAI || !unit.isAlive || unit.team == unitAI.team || unit.currentState == UnitState.Bench)
                continue;

            enemies.Add(unit);
        }

        if (enemies.Count == 0)
        {
            return transform.position + transform.forward * 3f;
        }

        Vector3 bestPosition = enemies[0].transform.position;
        int maxClumpSize = 0;

        foreach (UnitAI enemy in enemies)
        {
            int clumpSize = 0;
            Vector3 testPosition = enemy.transform.position;

            foreach (UnitAI otherEnemy in enemies)
            {
                float distance = Vector3.Distance(testPosition, otherEnemy.transform.position);
                if (distance <= blastRadius)
                {
                    clumpSize++;
                }
            }

            if (clumpSize > maxClumpSize)
            {
                maxClumpSize = clumpSize;
                bestPosition = testPosition;
            }
        }

        return bestPosition;
    }

    private List<UnitAI> FindEnemiesInRadius(Vector3 center, float radius)
    {
        List<UnitAI> enemiesInRadius = new List<UnitAI>();
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();

        foreach (UnitAI unit in allUnits)
        {
            if (unit == unitAI || !unit.isAlive || unit.team == unitAI.team || unit.currentState == UnitState.Bench)
                continue;

            float distance = Vector3.Distance(center, unit.transform.position);
            if (distance <= radius)
            {
                enemiesInRadius.Add(unit);
            }
        }

        return enemiesInRadius;
    }

    public void OnRoundEnd()
    {
        isPerformingAbility = false;
        activeAbilityTarget = null;
        isNextAttackCone = true;
    }

    private void OnDisable()
    {
        OnRoundEnd();
    }
}
