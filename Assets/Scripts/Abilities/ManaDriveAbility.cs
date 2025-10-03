using UnityEngine;
using static UnitAI;
using System.Collections.Generic;
using System.Collections;

public class ManaDriveAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;
    private CyberneticVFX vfxManager;

    [Header("Ability Stats")]
    public float[] damagePerStar = { 200, 250, 350 };
    public float range = 4f;
    public float splashRadius = 2f;
    public float attackSpeedGain = 0.3f;
    public float recursiveDamageReduction = 0.25f;

    [Header("Optional VFX")]
    public GameObject castVFX;
    public GameObject impactVFX;
    public GameObject splashVFX;

    [Header("🔊 ManaDrive Ability Audio")]
    [Tooltip("Audio played when ability is activated")]
    public AudioClip abilityStartSound;
    public AudioClip voiceLine;

    [Tooltip("Audio played when bomb is launched")]
    public AudioClip bombLaunchSound;

    [Tooltip("Audio played when bomb explodes")]
    public AudioClip bombExplosionSound;

    [Tooltip("Audio played when gaining attack speed on kill")]
    public AudioClip attackSpeedGainSound;

    [Tooltip("Audio played when recursive bomb is triggered")]
    public AudioClip recursiveBombSound;

    [Tooltip("Volume for ability audio")]
    [Range(0f, 1f)]
    public float abilityAudioVolume = 1f;

    // ✅ Audio system
    private AudioSource abilityAudioSource;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        vfxManager = GetComponent<CyberneticVFX>();
        SetupAbilityAudio();
    }

    // ✅ NEW: Setup ability audio
    private void SetupAbilityAudio()
    {
        abilityAudioSource = GetComponent<AudioSource>();
        if (abilityAudioSource == null)
        {
            abilityAudioSource = gameObject.AddComponent<AudioSource>();
        }

        abilityAudioSource.playOnAwake = false;
        abilityAudioSource.spatialBlend = 0.5f; // More 3D for bomb explosions
        abilityAudioSource.volume = abilityAudioVolume;

        Debug.Log($"🔊 ManaDrive ability audio system setup for {unitAI.unitName}");
    }

    // ✅ NEW: Play ability audio
    private void PlayAbilityAudio(AudioClip clip, string actionName = "")
    {
        if (clip != null && abilityAudioSource != null)
        {
            abilityAudioSource.PlayOneShot(clip, abilityAudioVolume);
            Debug.Log($"🔊 {unitAI.unitName} ManaDrive played {actionName} audio");
        }
    }

    // ✅ Interface implementation
    public AudioClip GetAbilityAudio()
    {
        return abilityStartSound;
    }

    public void Cast(UnitAI target)
    {

        // ✅ Play ability start audio
        PlayAbilityAudio(abilityStartSound, "ability start");
        PlayAbilityAudio(voiceLine, "voice line");

        if (unitAI.animator != null)
            unitAI.animator.SetTrigger("AbilityTrigger");

        Vector3 targetPosition = FindLargestEnemyGroup();

        if (castVFX != null)
        {
            GameObject castEffect = Instantiate(castVFX, transform.position + Vector3.up * 1.5f, Quaternion.identity);
            Destroy(castEffect, 2f);
        }

        Debug.Log($"💣 {unitAI.unitName} starts ManaDrive bombing sequence!");

        StartCoroutine(FireMassiveBomb(targetPosition, 1.0f));
    }

    private IEnumerator FireMassiveBomb(Vector3 targetPosition, float damageMultiplier)
    {
        // ✅ Play bomb launch audio
        PlayAbilityAudio(bombLaunchSound, "bomb launch");

        GameObject bombPrefab = vfxManager != null ? vfxManager.vfxConfig.abilityProjectile : null;

        if (bombPrefab == null)
        {
            Debug.LogError($"❌ No bomb prefab assigned for ManaDrive ({unitAI.unitName})!");
            ExplodeBomb(targetPosition, damageMultiplier);
            yield break;
        }

        Vector3 spawnPos = transform.position + Vector3.up * 8f;
        GameObject bomb = Instantiate(bombPrefab, spawnPos, Quaternion.identity);
        bomb.transform.localScale *= 2f;

        float fallSpeed = 15f;
        Debug.Log($"💣 {unitAI.unitName} bomb launched toward {targetPosition}");

        // Drop bomb downwards toward target
        while (bomb != null && bomb.transform.position.y > 0.6f)
        {
            Vector3 direction = (targetPosition - bomb.transform.position);
            direction.y = -1f; // force downward
            direction.Normalize();

            bomb.transform.position += direction * fallSpeed * Time.deltaTime;

            yield return null;
        }

        if (bomb != null)
        {
            bomb.transform.position = targetPosition + Vector3.up * 0.5f;
            ExplodeBomb(targetPosition, damageMultiplier);
            Destroy(bomb);
        }
    }

    // Handle bomb explosion and damage
    private void ExplodeBomb(Vector3 explosionPos, float damageMultiplier)
    {
        // ✅ Play bomb explosion audio
        PlayAbilityAudio(bombExplosionSound, "bomb explosion");

        float damage = damagePerStar[Mathf.Clamp(unitAI.starLevel - 1, 0, damagePerStar.Length - 1)] * damageMultiplier;
        bool killedTarget = false;

        Debug.Log($"💥 {unitAI.unitName} bomb explodes at {explosionPos} for {damage} damage!");

        // Explosion VFX
        if (vfxManager != null && vfxManager.vfxConfig.abilityImpactEffect != null)
        {
            GameObject explosion = Instantiate(vfxManager.vfxConfig.abilityImpactEffect, explosionPos, Quaternion.identity);
            explosion.transform.localScale *= 3f;
            Destroy(explosion, 3f);
        }

        // Optional splash indicator VFX
        if (splashVFX != null)
        {
            GameObject splash = Instantiate(splashVFX, explosionPos, Quaternion.identity);
            splash.transform.localScale = Vector3.one * splashRadius * 2f;
            Destroy(splash, 2f);
        }

        // Damage all enemies in splash radius
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        foreach (UnitAI enemy in allUnits)
        {
            if (enemy != null && enemy.isAlive && enemy.team != unitAI.team && enemy.currentState != UnitState.Bench)
            {
                float distance = Vector3.Distance(enemy.transform.position, explosionPos);
                if (distance <= splashRadius)
                {
                    float healthBeforeHit = enemy.currentHealth;
                    enemy.TakeDamage(damage);

                    if (healthBeforeHit > 0 && enemy.currentHealth <= 0)
                    {
                        killedTarget = true;
                    }

                    Debug.Log($"💥 {unitAI.unitName} bomb hit {enemy.unitName} for {damage} damage at distance {distance:F1}!");
                }
            }
        }

        // If bomb killed a target, gain attack speed and cast again
        if (killedTarget)
        {
            PlayAbilityAudio(voiceLine, "voice line");

            // ✅ Play attack speed gain audio
            PlayAbilityAudio(attackSpeedGainSound, "attack speed gain");

            unitAI.attackSpeed += attackSpeedGain;
            Debug.Log($"⚡ {unitAI.unitName} gained {attackSpeedGain} attack speed! Now: {unitAI.attackSpeed}");

            // Cast again at 75% effectiveness
            Vector3 newTargetPos = FindLargestEnemyGroup();
            if (newTargetPos != Vector3.zero)
            {
                // ✅ Play recursive bomb audio
                PlayAbilityAudio(recursiveBombSound, "recursive bomb");

                Debug.Log($"🔄 {unitAI.unitName} casting recursive bomb!");
                StartCoroutine(FireMassiveBomb(newTargetPos, 1f - recursiveDamageReduction));
            }
        }

        // Reset mana after cast
        unitAI.currentMana = 0;
    }

    // Find position with most enemies clustered together
    private Vector3 FindLargestEnemyGroup()
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        List<UnitAI> enemies = new List<UnitAI>();

        foreach (var unit in allUnits)
        {
            if (unit != unitAI && unit.isAlive && unit.team != unitAI.team && unit.currentState != UnitState.Bench)
                enemies.Add(unit);
        }

        if (enemies.Count == 0) return Vector3.zero;

        Vector3 bestPosition = enemies[0].transform.position;
        int maxEnemiesInRange = 0;

        // Check each enemy position as potential bomb center
        foreach (var enemy in enemies)
        {
            int enemiesInRange = 0;
            foreach (var otherEnemy in enemies)
            {
                if (Vector3.Distance(enemy.transform.position, otherEnemy.transform.position) <= splashRadius)
                {
                    enemiesInRange++;
                }
            }

            if (enemiesInRange > maxEnemiesInRange)
            {
                maxEnemiesInRange = enemiesInRange;
                bestPosition = enemy.transform.position;
            }
        }

        return bestPosition;
    }
}
