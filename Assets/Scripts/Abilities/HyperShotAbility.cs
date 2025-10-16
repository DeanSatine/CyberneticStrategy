using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static UnitAI;

public class HyperShotAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;
    private CyberneticVFX vfxManager;

    [Header("Passive: Explosive Rounds")]
    [Tooltip("Number of attacks needed to trigger AOE")]
    public int attacksToTrigger = 3;
    [Tooltip("Damage dealt in AOE per star level")]
    public float[] aoeDamagePerStar = { 200f, 300f, 500f };
    [Tooltip("Radius of AOE damage in hexes")]
    public float aoeRadius = 3f;

    [Header("Active: Hyper Assault")]
    [Tooltip("Attack speed bonus (flat additive)")]
    public float attackSpeedBonus = 0.40f;
    [Tooltip("Duration of attack speed buff")]
    public float buffDuration = 8f;

    [Header("VFX")]
    public GameObject explosionVFX;
    public GameObject abilityBuffVFX;

    [Header("Audio")]
    public AudioClip autoAttackSound;
    public AudioClip abilitySound;
    public AudioClip explosionSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    private AudioSource audioSource;
    private List<Coroutine> activeBuffs = new List<Coroutine>();
    private List<GameObject> activeBuffVFXs = new List<GameObject>();
    private int activeStackCount = 0;
    private int attackCounter = 0;
    private bool handleProjectiles = false;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        vfxManager = GetComponent<CyberneticVFX>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.8f;
        }

        // Disable CyberneticVFX if present so we can handle projectiles manually
        if (vfxManager != null)
        {
            vfxManager.enabled = false;
            handleProjectiles = true;
            Debug.Log($"⚡ HyperShot will handle its own projectiles");
        }
    }

    private void OnEnable()
    {
        if (unitAI != null && handleProjectiles)
        {
            unitAI.OnAttackEvent += OnAutoAttack;
        }
    }

    private void OnDisable()
    {
        if (unitAI != null && handleProjectiles)
        {
            unitAI.OnAttackEvent -= OnAutoAttack;
        }
    }

    private void OnAutoAttack(UnitAI target)
    {
        if (target == null || !target.isAlive) return;

        Vector3 spawnPos = unitAI.firePoint != null ? unitAI.firePoint.position : transform.position + Vector3.up * 1.5f;

        // Spawn muzzle flash
        if (vfxManager != null && vfxManager.vfxConfig.autoAttackMuzzleFlash != null)
        {
            GameObject muzzleFlash = Instantiate(vfxManager.vfxConfig.autoAttackMuzzleFlash, spawnPos, Quaternion.identity);
            Destroy(muzzleFlash, 0.5f);
        }

        // Play sound
        if (autoAttackSound != null)
        {
            PlaySound(autoAttackSound);
        }
        else if (vfxManager != null && vfxManager.vfxConfig.autoAttackSound != null)
        {
            audioSource.PlayOneShot(vfxManager.vfxConfig.autoAttackSound, volume);
        }

        // Fire projectile
        StartCoroutine(FireProjectile(spawnPos, target, unitAI.attackDamage));
    }

    private IEnumerator FireProjectile(Vector3 startPos, UnitAI target, float damage)
    {
        GameObject projectilePrefab = null;

        if (vfxManager != null && vfxManager.vfxConfig.autoAttackProjectile != null)
        {
            projectilePrefab = vfxManager.vfxConfig.autoAttackProjectile;
        }
        else if (unitAI.projectilePrefab != null)
        {
            projectilePrefab = unitAI.projectilePrefab;
        }

        if (projectilePrefab == null)
        {
            Debug.LogWarning($"No projectile prefab found for {unitAI.unitName}");
            yield break;
        }

        GameObject projectile = Instantiate(projectilePrefab, startPos, Quaternion.identity);
        float speed = 20f;

        while (projectile != null && target != null && target.isAlive && target.currentState != UnitState.Bench)
        {
            Vector3 targetPos = target.transform.position + Vector3.up * 1.2f;
            Vector3 direction = (targetPos - projectile.transform.position).normalized;

            projectile.transform.position += direction * speed * Time.deltaTime;
            projectile.transform.rotation = Quaternion.LookRotation(direction);

            if (Vector3.Distance(projectile.transform.position, targetPos) < 0.3f)
            {
                target.TakeDamage(damage);

                // Spawn hit effect
                if (vfxManager != null && vfxManager.vfxConfig.autoAttackHitEffect != null)
                {
                    GameObject hitEffect = Instantiate(vfxManager.vfxConfig.autoAttackHitEffect, targetPos, Quaternion.identity);
                    Destroy(hitEffect, 1f);
                }

                // Count this hit toward passive
                IncrementAttackCounter(targetPos);

                Destroy(projectile);
                yield break;
            }

            yield return null;
        }

        if (projectile != null)
        {
            Destroy(projectile);
        }
    }

    private void IncrementAttackCounter(Vector3 hitPosition)
    {
        attackCounter++;
        Debug.Log($"⚡ {unitAI.unitName} attack counter: {attackCounter}/{attacksToTrigger}");

        if (attackCounter >= attacksToTrigger)
        {
            TriggerExplosion(hitPosition);
            attackCounter = 0;
        }
    }

    private void TriggerExplosion(Vector3 epicenter)
    {
        int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, aoeDamagePerStar.Length - 1);
        float damage = aoeDamagePerStar[starIndex];

        Debug.Log($"💥 {unitAI.unitName} triggers explosion at {epicenter} for {damage} damage!");

        PlaySound(explosionSound);

        // Spawn explosion VFX
        if (explosionVFX != null)
        {
            GameObject vfx = Instantiate(explosionVFX, epicenter + Vector3.up * 1f, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        // Find all enemies in radius
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        List<UnitAI> hitTargets = new List<UnitAI>();

        foreach (var unit in allUnits)
        {
            if (unit == unitAI || !unit.isAlive || unit.team == unitAI.team || unit.currentState == UnitState.Bench)
                continue;

            float distance = Vector3.Distance(epicenter, unit.transform.position);
            if (distance <= aoeRadius)
            {
                unit.TakeDamage(damage);
                hitTargets.Add(unit);
                Debug.Log($"💥 Explosion hit {unit.unitName} for {damage} damage");
            }
        }

        Debug.Log($"💥 {unitAI.unitName} explosion hit {hitTargets.Count} enemies!");
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
            unitAI.animator.speed = 2f;
            StartCoroutine(ResetAnimatorSpeed());
        }

        PlaySound(abilitySound);

        activeStackCount++;

        Coroutine newBuff = StartCoroutine(ApplyAttackSpeedBuff(attackSpeedBonus));
        activeBuffs.Add(newBuff);

        if (abilityBuffVFX != null)
        {
            GameObject vfx = Instantiate(abilityBuffVFX, transform.position, Quaternion.identity);
            vfx.transform.SetParent(transform);
            activeBuffVFXs.Add(vfx);

            StartCoroutine(ManageBuffVFX(vfx, buffDuration));
        }

        Debug.Log($"⚡ {unitAI.unitName} activates Hyper Assault! Stack {activeStackCount}: +{attackSpeedBonus} AS for {buffDuration}s!");
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

        if (vfx != null && activeBuffVFXs.Contains(vfx))
        {
            activeBuffVFXs.Remove(vfx);

            ParticleSystem[] particles = vfx.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particles)
            {
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }

            Destroy(vfx, 2f);
        }
    }

    private IEnumerator ApplyAttackSpeedBuff(float bonusAmount)
    {
        unitAI.attackSpeed += bonusAmount;

        Debug.Log($"⚡ {unitAI.unitName} gained +{bonusAmount:F2} attack speed. Total speed: {unitAI.attackSpeed:F2} (stack {activeStackCount})");

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
        attackCounter = 0;

        Debug.Log($"[HyperShotAbility] Round ended for {unitAI.unitName}");
    }
}
