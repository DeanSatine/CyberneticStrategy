using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class SteelGuardAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;
    private CyberneticVFX vfxManager;

    [Header("Shield Settings")]
    [Tooltip("Shield value at 1/2/3 stars")]
    public float[] shieldValue = { 150f, 300f, 600f };

    [Tooltip("AP scaling ratios per star level (260/290/360%)")]
    public float[] shieldAPRatio = { 2.6f, 2.9f, 3.6f };

    [Tooltip("Shield duration in seconds")]
    public float shieldDuration = 4f;

    [Tooltip("How far to search for nearest ally (in hexes)")]
    public float allySearchRadius = 8f;

    [Header("VFX")]
    [Tooltip("Shield visual effect prefab")]
    public GameObject shieldVFX;

    [Header("Audio")]
    public AudioClip screamSound;
    [Range(0f, 1f)]
    public float audioVolume = 0.8f;

    private AudioSource audioSource;
    private Coroutine activeShieldCoroutine;
    private GameObject activeShieldVFX;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        vfxManager = GetComponent<CyberneticVFX>();
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
        audioSource.spatialBlend = 0.5f;
        audioSource.volume = audioVolume;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, audioVolume);
        }
    }

    public void Cast(UnitAI target)
    {
        if (!unitAI.isAlive) return;

        unitAI.currentMana = 0f;
        if (unitAI.ui != null)
        {
            unitAI.ui.UpdateMana(unitAI.currentMana);
        }

        PlaySound(screamSound);

        if (unitAI.animator)
        {
            unitAI.animator.SetTrigger("AbilityTrigger");
        }

        int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, shieldValue.Length - 1);
        float baseShield = shieldValue[starIndex];
        float apRatio = shieldAPRatio[starIndex];

        float myShieldValue = baseShield + (unitAI.abilityPower * apRatio);
        float allyShieldValue = myShieldValue * 0.5f;

        ApplyShield(unitAI, myShieldValue);

        UnitAI nearestAlly = FindNearestAlly();
        if (nearestAlly != null)
        {
            ApplyShield(nearestAlly, allyShieldValue);
            Debug.Log($"🛡️ {unitAI.unitName} screamed! Self shield: {myShieldValue:F0} ({baseShield} + {apRatio * 100:F0}% AP), Ally {nearestAlly.unitName} shield: {allyShieldValue:F0}");
        }
        else
        {
            Debug.Log($"🛡️ {unitAI.unitName} screamed! Self shield: {myShieldValue:F0} ({baseShield} + {apRatio * 100:F0}% AP) (no nearby allies)");
        }
    }

    public void OnRoundEnd()
    {
        if (activeShieldCoroutine != null)
        {
            StopCoroutine(activeShieldCoroutine);
            activeShieldCoroutine = null;
        }

        if (activeShieldVFX != null)
        {
            Destroy(activeShieldVFX);
            activeShieldVFX = null;
        }
    }

    private UnitAI FindNearestAlly()
    {
        UnitAI nearestAlly = null;
        float closestDistance = float.MaxValue;

        Collider[] colliders = Physics.OverlapSphere(transform.position, allySearchRadius);

        foreach (Collider col in colliders)
        {
            UnitAI otherUnit = col.GetComponent<UnitAI>();

            if (otherUnit != null &&
                otherUnit != unitAI &&
                otherUnit.teamID == unitAI.teamID &&
                otherUnit.isAlive &&
                otherUnit.currentState == UnitState.Combat)
            {
                float distance = Vector3.Distance(transform.position, otherUnit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestAlly = otherUnit;
                }
            }
        }

        return nearestAlly;
    }

    private void ApplyShield(UnitAI target, float shieldAmount)
    {
        if (target == null || !target.isAlive) return;

        ShieldComponent shieldComp = target.GetComponent<ShieldComponent>();
        if (shieldComp == null)
        {
            shieldComp = target.gameObject.AddComponent<ShieldComponent>();
        }

        shieldComp.AddShield(shieldAmount, shieldDuration);

        if (shieldVFX != null && target == unitAI)
        {
            if (activeShieldVFX != null)
            {
                Destroy(activeShieldVFX);
            }
            activeShieldVFX = Instantiate(shieldVFX, target.transform.position, Quaternion.identity, target.transform);
            Destroy(activeShieldVFX, shieldDuration);
        }
        else if (shieldVFX != null && target != unitAI)
        {
            GameObject allyVFX = Instantiate(shieldVFX, target.transform.position, Quaternion.identity, target.transform);
            Destroy(allyVFX, shieldDuration);
        }
    }

    private void OnDisable()
    {
        OnRoundEnd();
    }
}
