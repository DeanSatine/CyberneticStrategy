using System.Collections;
using UnityEngine;
using static UnitAI;

public class KillSwitchAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;
    private UnitAI lastTarget;
    private int attackCounter = 0;

    [Header("Ability Stats")]
    public float[] slamDamagePerStar = { 100f, 150f, 250f };
    public float passiveSlamDamage = 100f;
    public float healOnTargetSwap = 200f;
    public float armorShred = 3f;

    [Header("Timings")]
    public float leapDuration = 0.5f;
    public float slamDuration = 0.7f;
    public float leapHeight = 2f;
    public float maxLeapRange = 3f;
    public float attackSpeedBuff = 1.5f;
    public float buffDuration = 4f;

    [Header("🎯 Slam Impact Timing")]
    [Tooltip("Delay after landing before slam impact (VFX + Damage together)")]
    public float slamImpactDelay = 0.35f;

    [Header("VFX")]
    public GameObject slamVFX;

    [Header("🔊 KillSwitch Ability Audio")]
    [Tooltip("Audio played when ability is activated")]
    public AudioClip abilityStartSound;

    [Tooltip("Audio played during leap/jump")]
    public AudioClip leapSound;

    [Tooltip("Audio played on slam impact")]
    public AudioClip slamImpactSound;

    [Tooltip("Audio played when landing after leap")]
    public AudioClip landingSound;

    [Tooltip("Audio played when gaining attack speed buff")]
    public AudioClip attackSpeedBuffSound;

    [Tooltip("Volume for ability audio")]
    [Range(0f, 1f)]
    public float abilityAudioVolume = 1f;

    // Audio system
    private AudioSource abilityAudioSource;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        SetupAbilityAudio();
    }

    // Setup ability audio
    private void SetupAbilityAudio()
    {
        abilityAudioSource = GetComponent<AudioSource>();
        if (abilityAudioSource == null)
        {
            abilityAudioSource = gameObject.AddComponent<AudioSource>();
        }

        abilityAudioSource.playOnAwake = false;
        abilityAudioSource.spatialBlend = 0.3f;
        abilityAudioSource.volume = abilityAudioVolume;

        Debug.Log($"🔊 KillSwitch ability audio system setup for {unitAI.unitName}");
    }

    // Play ability audio
    private void PlayAbilityAudio(AudioClip clip, string actionName = "")
    {
        if (clip != null && abilityAudioSource != null)
        {
            abilityAudioSource.PlayOneShot(clip, abilityAudioVolume);
            Debug.Log($"🔊 {unitAI.unitName} KillSwitch played {actionName} audio");
        }
    }

    // Interface implementation
    public AudioClip GetAbilityAudio()
    {
        return abilityStartSound;
    }

    public void Cast(UnitAI _ignored)
    {
        UnitAI target = FindFarthestEnemyInRange(maxLeapRange);

        if (target == null || target.currentState == UnitState.Bench)
        {
            Debug.Log($"{unitAI.unitName} tried to leap, but no valid enemies within {maxLeapRange}!");
            return;
        }

        // ✅ Play ability start audio
        PlayAbilityAudio(abilityStartSound, "ability start");

        Debug.Log($"🦘 {unitAI.unitName} leaping to FARTHEST enemy: {target.unitName}");
        StartCoroutine(LeapAndSlam(target));
    }

    private IEnumerator LeapAndSlam(UnitAI target)
    {
        if (target == null || !target.isAlive || target.currentState == UnitState.Bench) yield break;

        Vector3 startPos = unitAI.transform.position;

        // Find landing position
        HexTile enemyTile = target.currentTile;
        HexTile landingTile = null;
        if (enemyTile != null && BoardManager.Instance != null)
        {
            landingTile = BoardManager.Instance.GetClosestFreeNeighbor(enemyTile, unitAI.currentTile);
        }

        Vector3 dir = (target.transform.position - startPos);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = unitAI.transform.forward;

        dir.Normalize();
        float stopDistance = 1.2f;

        Vector3 endPos;
        if (landingTile != null)
        {
            endPos = landingTile.transform.position;
        }
        else
        {
            endPos = target.transform.position - dir * stopDistance;
        }

        endPos.y = startPos.y;

        if (unitAI.animator) unitAI.animator.SetTrigger("LeapTrigger");

        // ✅ Play leap audio when jumping starts
        PlayAbilityAudio(leapSound, "leap");

        Debug.Log($"🚀 {unitAI.unitName} leaping from {startPos} to {endPos} (targeting {target.unitName})");

        // ===== LEAP PHASE =====
        float elapsed = 0f;
        while (elapsed < leapDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / leapDuration);

            Vector3 horizontal = Vector3.Lerp(startPos, endPos, t);
            horizontal.y += Mathf.Sin(t * Mathf.PI) * leapHeight;

            unitAI.transform.position = horizontal;
            yield return null;
        }

        unitAI.transform.position = endPos;

        // ✅ Play landing audio
        PlayAbilityAudio(landingSound, "landing");

        Debug.Log($"🎯 {unitAI.unitName} LANDED! Starting slam sequence...");

        // Update positioning
        unitAI.currentTarget = target.transform;

        if (landingTile != null)
        {
            if (unitAI.currentTile != null && unitAI.currentTile.occupyingUnit == unitAI)
                unitAI.currentTile.occupyingUnit = null;

            unitAI.currentTile = landingTile;
            landingTile.occupyingUnit = unitAI;
        }
        else
        {
            if (BoardManager.Instance != null)
            {
                HexTile nearest = BoardManager.Instance.GetTileFromWorld(endPos);
                if (nearest != null)
                {
                    unitAI.ClearTile();
                    unitAI.AssignToTile(nearest);
                }
            }
        }

        Vector3 lookAt = target.transform.position;
        lookAt.y = unitAI.transform.position.y;
        unitAI.transform.LookAt(lookAt);

        if (unitAI.animator)
        {
            unitAI.animator.SetTrigger("AbilityTrigger");
            Debug.Log($"🎬 Slam animation started for {unitAI.unitName}");
        }

        Debug.Log($"⏳ Waiting {slamImpactDelay}s for slam impact moment...");
        yield return new WaitForSeconds(slamImpactDelay);

        // ===== SLAM IMPACT - VFX, AUDIO AND DAMAGE TOGETHER =====
        if (target != null && target.isAlive)
        {
            Debug.Log($"💥⚡ SLAM IMPACT! VFX + Audio + Damage happening NOW!");

            // ✅ Play slam impact audio
            PlayAbilityAudio(slamImpactSound, "slam impact");

            if (slamVFX != null)
            {
                Vector3 slamVFXPos = unitAI.transform.position;
                slamVFXPos.y = 0.1f;
                var slamEffect = Instantiate(slamVFX, slamVFXPos, Quaternion.identity);
                Destroy(slamEffect, 3f);

                Debug.Log($"💥✨ Slam VFX spawned at impact moment: {slamVFXPos}");
            }

            float damage = slamDamagePerStar[Mathf.Clamp(unitAI.starLevel - 1, 0, slamDamagePerStar.Length - 1)]
                           + unitAI.attackDamage;

            target.TakeDamage(damage);
            Debug.Log($"💥 {unitAI.unitName} dealt {damage} slam damage at impact moment!");

            if (!target.isAlive)
            {
                unitAI.currentTarget = null;
                Debug.Log($"💀 {target.unitName} was defeated by the slam impact!");
            }
            else
            {
                unitAI.currentTarget = target.transform;
            }
        }
        else
        {
            Debug.Log($"⚠️ Target died before slam impact, but playing VFX and audio anyway!");

            PlayAbilityAudio(slamImpactSound, "slam impact");

            if (slamVFX != null)
            {
                Vector3 slamVFXPos = unitAI.transform.position;
                slamVFXPos.y = 0.1f;
                var slamEffect = Instantiate(slamVFX, slamVFXPos, Quaternion.identity);
                Destroy(slamEffect, 3f);
            }
        }

        // Wait for remaining animation
        float remainingAnimTime = slamDuration - slamImpactDelay;
        if (remainingAnimTime > 0f)
        {
            Debug.Log($"⏳ Waiting {remainingAnimTime:F2}s for slam animation to finish...");
            yield return new WaitForSeconds(remainingAnimTime);
        }

        Debug.Log($"✅ Slam sequence complete!");

        // ✅ Play attack speed buff audio
        PlayAbilityAudio(attackSpeedBuffSound, "attack speed buff");

        StartCoroutine(TemporaryAttackSpeedBuff(attackSpeedBuff, buffDuration));
    }

    public void OnAttack(UnitAI target)
    {
        attackCounter++;

        if (attackCounter % 2 == 0 && target != null)
        {
            target.armor = Mathf.Max(0, target.armor - armorShred);
            Debug.Log($"{unitAI.unitName} shredded {target.unitName}'s armor! Now {target.armor}");
        }

        if (lastTarget != null && lastTarget != target)
        {
            float dmg = passiveSlamDamage + unitAI.attackDamage;
            lastTarget.TakeDamage(dmg);
            unitAI.currentHealth = Mathf.Min(unitAI.maxHealth, unitAI.currentHealth + healOnTargetSwap);

            Debug.Log($"{unitAI.unitName} slammed {lastTarget.unitName} on target swap for {dmg} dmg + healed {healOnTargetSwap} HP!");
        }

        lastTarget = target;
    }

    private IEnumerator TemporaryAttackSpeedBuff(float multiplier, float duration)
    {
        float original = unitAI.attackSpeed;
        unitAI.attackSpeed *= multiplier;

        Debug.Log($"⚡ {unitAI.unitName} gained {(multiplier - 1f) * 100:F0}% attack speed for {duration} seconds!");

        yield return new WaitForSeconds(duration);

        unitAI.attackSpeed = original;
        Debug.Log($"⏰ {unitAI.unitName} attack speed buff expired");
    }

    private UnitAI FindFarthestEnemyInRange(float range)
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        UnitAI farthest = null;
        float maxDist = 0f;

        foreach (var unit in allUnits)
        {
            if (unit == unitAI || !unit.isAlive || unit.team == unitAI.team || unit.currentState == UnitState.Bench)
                continue;

            float dist = Vector3.Distance(unitAI.transform.position, unit.transform.position);

            if (dist > maxDist && dist <= range)
            {
                maxDist = dist;
                farthest = unit;
            }
        }

        if (farthest != null)
        {
            Debug.Log($"🎯 {unitAI.unitName} found farthest enemy: {farthest.unitName} at distance {maxDist:F1}");
        }

        return farthest;
    }
}
