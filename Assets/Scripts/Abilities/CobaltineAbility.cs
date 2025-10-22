using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class CobaltineAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;
    private CyberneticVFX vfxManager;
    private Coroutine cloudRoutine;

    [Header("Passive: Healing → Damage Conversion")]
    [Tooltip("Percentage of healing converted to next auto attack bonus damage")]
    [Range(0f, 1f)]
    public float healToDamageConversion = 0.5f;

    private float storedPassiveDamage = 0f;

    [Header("Active: Armor Drain Cloud")]
    [Tooltip("Healing per second at 1/2/3 stars")]
    public float[] healingPerSecond = { 30f, 90f, 500f };

    [Tooltip("Armor drained total per second (split between enemies)")]
    public float armorDrainPerSecond = 5f;

    [Tooltip("Cloud radius in hex tiles")]
    public float cloudRadius = 2f;

    [Tooltip("How long the cloud lasts")]
    public float cloudDuration = 5f;

    [Header("VFX")]
    [Tooltip("VFX to play on body when Cobaltine receives healing")]
    public GameObject healVFX;

    [Tooltip("Cloud VFX spawned above the target area")]
    public GameObject cloudVFX;

    [Tooltip("VFX to play on hand when passive damage is ready")]
    public GameObject passiveReadyVFX;

    [Header("Audio")]
    public AudioClip abilityStartSound;
    public AudioClip cloudTickSound;
    public AudioClip passiveProcSound;
    [Range(0f, 1f)]
    public float audioVolume = 0.7f;

    private AudioSource audioSource;
    private GameObject activeCloudVFX;
    private GameObject passiveVFXInstance;

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
    }

    private void Start()
    {
        unitAI.OnAttackEvent += OnAutoAttack;
    }

    private void OnDestroy()
    {
        if (unitAI != null)
        {
            unitAI.OnAttackEvent -= OnAutoAttack;
        }

        if (cloudRoutine != null)
        {
            StopCoroutine(cloudRoutine);
        }

        if (activeCloudVFX != null)
        {
            Destroy(activeCloudVFX);
        }

        if (passiveVFXInstance != null)
        {
            Destroy(passiveVFXInstance);
        }
    }

    public void Cast(UnitAI target)
    {
        if (!unitAI.isAlive) return;

        if (unitAI.animator != null)
        {
            unitAI.animator.SetTrigger("AbilityTrigger");
        }

        PlayAbilityAudio(abilityStartSound);

        Vector3 cloudPosition = transform.position + Vector3.up * 3f;

        if (cloudVFX != null)
        {
            activeCloudVFX = Instantiate(cloudVFX, cloudPosition, Quaternion.identity);
            activeCloudVFX.transform.localScale = Vector3.one * cloudRadius * 2f;
        }

        cloudRoutine = StartCoroutine(CloudEffectRoutine());
    }

    private IEnumerator CloudEffectRoutine()
    {
        float elapsed = 0f;
        float tickRate = 1f;
        float nextTick = 0f;

        Debug.Log($"☁️ {unitAI.unitName} cloud active for {cloudDuration}s!");

        while (elapsed < cloudDuration && unitAI != null && unitAI.isAlive)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextTick)
            {
                nextTick += tickRate;
                ProcessCloudTick();
            }

            yield return null;
        }

        if (activeCloudVFX != null)
        {
            Destroy(activeCloudVFX);
        }

        unitAI.currentMana = 0f;
        if (unitAI.ui != null)
        {
            unitAI.ui.UpdateMana(unitAI.currentMana);
        }

        Debug.Log($"☁️ {unitAI.unitName} cloud expired!");
    }

    private void ProcessCloudTick()
    {
        PlayAbilityAudio(cloudTickSound);

        List<UnitAI> enemiesInCloud = FindEnemiesInCloud();

        if (enemiesInCloud.Count > 0)
        {
            float armorDrainPerEnemy = armorDrainPerSecond / enemiesInCloud.Count;

            foreach (UnitAI enemy in enemiesInCloud)
            {
                enemy.armor = Mathf.Max(0, enemy.armor - armorDrainPerEnemy);
                Debug.Log($"☁️ {unitAI.unitName} drained {armorDrainPerEnemy} armor from {enemy.unitName} (now: {enemy.armor})");
            }
        }

        float healing = healingPerSecond[Mathf.Clamp(unitAI.starLevel - 1, 0, healingPerSecond.Length - 1)];
        HealSelf(healing);
    }

    private List<UnitAI> FindEnemiesInCloud()
    {
        List<UnitAI> enemies = new List<UnitAI>();
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();

        foreach (UnitAI unit in allUnits)
        {
            if (unit != unitAI && unit.isAlive && unit.team != unitAI.team && unit.currentState != UnitState.Bench)
            {
                float distance = Vector3.Distance(transform.position, unit.transform.position);
                if (distance <= cloudRadius)
                {
                    enemies.Add(unit);
                }
            }
        }

        return enemies;
    }

    public void HealSelf(float healAmount)
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
            GameObject healEffect = Instantiate(healVFX, transform.position + Vector3.up * 1f, Quaternion.identity);
            Destroy(healEffect, 1.5f);
        }

        float passiveDamageGain = actualHealing * healToDamageConversion;
        storedPassiveDamage += passiveDamageGain;

        UpdatePassiveVFX();

        Debug.Log($"💚 {unitAI.unitName} healed {actualHealing} HP! Stored passive damage: {storedPassiveDamage}");
    }

    private void OnAutoAttack(UnitAI target)
    {
        if (storedPassiveDamage > 0 && target != null && target.isAlive)
        {
            PlayAbilityAudio(passiveProcSound);

            target.TakeDamage(storedPassiveDamage);

            Debug.Log($"⚡ {unitAI.unitName} dealt {storedPassiveDamage} bonus passive damage to {target.unitName}!");

            storedPassiveDamage = 0f;
            UpdatePassiveVFX();
        }
    }

    private void UpdatePassiveVFX()
    {
        if (passiveReadyVFX == null) return;

        if (storedPassiveDamage > 0 && passiveVFXInstance == null)
        {
            Transform handPoint = unitAI.firePoint != null ? unitAI.firePoint : transform;
            passiveVFXInstance = Instantiate(passiveReadyVFX, handPoint.position, Quaternion.identity, handPoint);
        }
        else if (storedPassiveDamage <= 0 && passiveVFXInstance != null)
        {
            Destroy(passiveVFXInstance);
            passiveVFXInstance = null;
        }
    }

    private void PlayAbilityAudio(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, audioVolume);
        }
    }

    public void OnRoundEnd()
    {
        if (cloudRoutine != null)
        {
            StopCoroutine(cloudRoutine);
            cloudRoutine = null;
        }

        if (activeCloudVFX != null)
        {
            Destroy(activeCloudVFX);
        }

        storedPassiveDamage = 0f;

        if (passiveVFXInstance != null)
        {
            Destroy(passiveVFXInstance);
            passiveVFXInstance = null;
        }
    }
}
