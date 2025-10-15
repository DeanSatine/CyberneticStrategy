using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static UnitAI;

public class HyperShotAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;

    [Header("Passive: Cone Attack")]
    [Tooltip("Radius of the cone attack in hexes")]
    public float coneRadius = 2f;
    [Tooltip("Angle of the cone attack in degrees")]
    public float coneAngle = 60f;
    [Tooltip("Bonus damage per auto attack")]
    public float passiveBonusDamage = 10f;

    [Header("Active: Hyper Assault")]
    [Tooltip("Attack speed bonus by star level")]
    public float[] attackSpeedBonus = { 0.40f, 0.50f, 0.60f };
    [Tooltip("Duration of attack speed buff")]
    public float buffDuration = 4f;

    [Header("VFX")]
    public GameObject coneAttackVFX;
    public GameObject abilityBuffVFX;
    public GameObject autoAttackVFX;
    [Tooltip("VFX that spawns at the fire point when shooting")]
    public GameObject muzzleFlashVFX;

    [Header("Audio")]
    public AudioClip autoAttackSound;
    public AudioClip abilitySound;
    [Range(0f, 1f)] public float volume = 0.8f;

    private AudioSource audioSource;
    private List<Coroutine> activeBuffs = new List<Coroutine>();
    private List<GameObject> activeBuffVFXs = new List<GameObject>();
    private int activeStackCount = 0;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.8f;
        }
    }

    private void OnEnable()
    {
        if (unitAI != null)
        {
            unitAI.OnAttackEvent += OnAutoAttack;
        }
    }

    private void OnDisable()
    {
        if (unitAI != null)
        {
            unitAI.OnAttackEvent -= OnAutoAttack;
        }
    }

    private void OnAutoAttack(UnitAI target)
    {
        if (target == null || !target.isAlive) return;

        PlaySound(autoAttackSound);

        SpawnMuzzleFlash();

        ApplyConeAttack(target);
    }

    private void SpawnMuzzleFlash()
    {
        if (muzzleFlashVFX != null && unitAI.firePoint != null)
        {
            GameObject muzzle = Instantiate(muzzleFlashVFX, unitAI.firePoint.position, unitAI.firePoint.rotation);
            muzzle.transform.SetParent(unitAI.firePoint);
            Destroy(muzzle, 1f);
        }
    }

    private void ApplyConeAttack(UnitAI primaryTarget)
    {
        Vector3 attackDirection = (primaryTarget.transform.position - transform.position).normalized;
        List<UnitAI> coneTargets = FindUnitsInCone(attackDirection, coneRadius, coneAngle);

        foreach (UnitAI coneTarget in coneTargets)
        {
            if (coneTarget == primaryTarget) continue;

            coneTarget.TakeDamage(passiveBonusDamage);

            if (coneAttackVFX != null)
            {
                Vector3 vfxPos = coneTarget.transform.position + Vector3.up * 1f;
                GameObject vfx = Instantiate(coneAttackVFX, vfxPos, Quaternion.identity);
                Destroy(vfx, 1f);
            }

            Debug.Log($"⚡ {unitAI.unitName} cone damage: {passiveBonusDamage} to {coneTarget.unitName}");
        }

        if (coneTargets.Count > 0 && autoAttackVFX != null)
        {
            Vector3 vfxPos = transform.position + attackDirection * (coneRadius / 2f) + Vector3.up * 1f;
            GameObject vfx = Instantiate(autoAttackVFX, vfxPos, Quaternion.LookRotation(attackDirection));
            Destroy(vfx, 1.5f);
        }

        if (coneTargets.Count > 0)
        {
            Debug.Log($"⚡ {unitAI.unitName} hit {coneTargets.Count} additional targets with cone attack");
        }
    }

    private List<UnitAI> FindUnitsInCone(Vector3 direction, float radius, float angle)
    {
        List<UnitAI> unitsInCone = new List<UnitAI>();
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();

        foreach (var unit in allUnits)
        {
            if (unit == unitAI || !unit.isAlive || unit.team == unitAI.team || unit.currentState == UnitState.Bench)
                continue;

            Vector3 toTarget = unit.transform.position - transform.position;
            float distance = toTarget.magnitude;

            if (distance > radius) continue;

            float angleToTarget = Vector3.Angle(direction, toTarget.normalized);

            if (angleToTarget <= angle / 2f)
            {
                unitsInCone.Add(unit);
            }
        }

        return unitsInCone;
    }

    public void Cast(UnitAI target)
    {
        if (unitAI.currentState != UnitState.Combat && unitAI.currentState != UnitState.BoardIdle)
        {
            Debug.Log($"[HyperShotAbility] Cannot cast in state: {unitAI.currentState}");
            return;
        }

        if (unitAI.animator)
        {
            unitAI.animator.SetTrigger("AbilityTrigger");

            // Optional: Speed up the ability animation
            unitAI.animator.speed = 2f;
            StartCoroutine(ResetAnimatorSpeed());
        }

        PlaySound(abilitySound);

        activeStackCount++;

        int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, attackSpeedBonus.Length - 1);
        float asBonus = attackSpeedBonus[starIndex];

        Coroutine newBuff = StartCoroutine(ApplyAttackSpeedBuff(asBonus));
        activeBuffs.Add(newBuff);

        if (abilityBuffVFX != null)
        {
            GameObject vfx = Instantiate(abilityBuffVFX, transform.position, Quaternion.identity);
            vfx.transform.SetParent(transform);
            activeBuffVFXs.Add(vfx);

            StartCoroutine(ManageBuffVFX(vfx, buffDuration));
        }

        Debug.Log($"⚡ {unitAI.unitName} activates Hyper Assault! Stack {activeStackCount}: +{asBonus * 100f}% AS for {buffDuration}s");
    }

    private IEnumerator ResetAnimatorSpeed()
    {
        yield return new WaitForSeconds(0.3f);
        if (unitAI.animator)
        {
            unitAI.animator.speed = 1f;
        }
    }


    private IEnumerator ManageBuffVFX(GameObject vfx, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (vfx != null)
        {
            ParticleSystem[] particles = vfx.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            activeBuffVFXs.Remove(vfx);

            yield return new WaitForSeconds(2f);

            if (vfx != null)
            {
                Destroy(vfx);
            }
        }
    }


    private IEnumerator DestroyBuffVFXAfterDuration(GameObject vfx, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (vfx != null)
        {
            activeBuffVFXs.Remove(vfx);
            Destroy(vfx);
        }
    }

    private IEnumerator ApplyAttackSpeedBuff(float bonusPercent)
    {
        float bonusAmount = unitAI.attackSpeed * bonusPercent;

        unitAI.attackSpeed += bonusAmount;

        Debug.Log($"⚡ {unitAI.unitName} gained +{bonusAmount:F2} attack speed ({bonusPercent * 100f}%). Total speed: {unitAI.attackSpeed:F2} (stack {activeStackCount})");

        yield return new WaitForSeconds(buffDuration);

        unitAI.attackSpeed -= bonusAmount;
        activeStackCount = Mathf.Max(0, activeStackCount - 1);

        Debug.Log($"⚡ {unitAI.unitName} lost {bonusAmount:F2} attack speed. Current speed: {unitAI.attackSpeed:F2}, remaining stacks: {activeStackCount}");
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    public void OnRoundEnd()
    {
        foreach (var buff in activeBuffs)
        {
            if (buff != null)
            {
                StopCoroutine(buff);
            }
        }

        activeBuffs.Clear();

        foreach (var vfx in activeBuffVFXs)
        {
            if (vfx != null)
            {
                ParticleSystem[] particles = vfx.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem ps in particles)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }

                Destroy(vfx, 2f);
            }
        }

        activeBuffVFXs.Clear();
        activeStackCount = 0;

        Debug.Log($"[HyperShotAbility] Round ended for {unitAI.unitName}");
    }
}
