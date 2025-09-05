using UnityEngine;
using System.Collections;

public class ClobbertronTrait : MonoBehaviour
{
    [HideInInspector] public float bonusArmor = 10f;
    [HideInInspector] public float bonusAttackDamageAmp = 0.1f; // +10% AD
    [HideInInspector] public float crashRadius = 2f;
    [HideInInspector] public float crashDamage = 200f;

    private UnitAI unitAI;
    private bool buffsApplied = false;
    private bool hasCrashed = false;
    private bool isCrashing = false;

    // Track attack bonuses properly
    private float flatAttackBonus = 0f;
    private float startingAttackDamage = 0f;

    [Header("VFX / Animation")]
    public GameObject slamEffectPrefab;
    public float jumpHeight = 2f;
    public float jumpDuration = 0.3f;
    public float slamDuration = 0.2f;
    public float cameraShakeIntensity = 0.4f;
    public float cameraShakeDuration = 0.25f;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
    }

    private void Update()
    {
        if (unitAI == null || !unitAI.isAlive) return;

        // ✅ Apply buffs once at combat start
        if (!buffsApplied && unitAI.currentState == UnitAI.UnitState.Combat)
        {
            startingAttackDamage = unitAI.attackDamage; // store original AD
            ApplyBuffs();
            buffsApplied = true;
        }

        // ✅ Trigger crash once when hitting 50% HP
        if (!hasCrashed && unitAI.currentHealth <= unitAI.maxHealth * 0.5f && !isCrashing)
        {
            StartCoroutine(CrashSequence());
        }
    }

    private void ApplyBuffs()
    {
        unitAI.armor += bonusArmor;

        // Add flat bonus relative to starting AD
        flatAttackBonus += startingAttackDamage * bonusAttackDamageAmp;
        unitAI.attackDamage = startingAttackDamage + flatAttackBonus;
    }

    private IEnumerator CrashSequence()
    {
        isCrashing = true;
        hasCrashed = true;

        Vector3 startPos = transform.position;
        Vector3 jumpPos = startPos + Vector3.up * jumpHeight;

        // Jump up
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / jumpDuration;
            transform.position = Vector3.Lerp(startPos, jumpPos, t);
            yield return null;
        }

        // Slam down
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / slamDuration;
            transform.position = Vector3.Lerp(jumpPos, startPos, t);
            yield return null;
        }

        // Slam FX
        if (slamEffectPrefab != null)
        {
            GameObject fx = Instantiate(slamEffectPrefab, startPos, Quaternion.identity);
            Destroy(fx, 2f);
        }
        StartCoroutine(CameraShake(cameraShakeIntensity, cameraShakeDuration));

        // Do AoE crash damage
        Collider[] hits = Physics.OverlapSphere(startPos, crashRadius);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<UnitAI>();
            if (enemy != null && enemy.team != unitAI.team && enemy.isAlive)
            {
                enemy.TakeDamage(crashDamage);
            }
        }

        // ✅ Apply permanent buffs again (flat additive)
        ApplyBuffs();

        isCrashing = false;
    }

    private IEnumerator CameraShake(float intensity, float duration)
    {
        if (Camera.main == null) yield break;

        Vector3 originalPos = Camera.main.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 offset = Random.insideUnitSphere * intensity;
            Camera.main.transform.position = originalPos + offset;
            yield return null;
        }

        Camera.main.transform.position = originalPos;
    }
}
