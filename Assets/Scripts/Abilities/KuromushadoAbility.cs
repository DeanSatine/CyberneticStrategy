using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class KuromushadoAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;

    [Header("Passive: Cone Sweep Auto Attacks")]
    [Tooltip("Cone size per star level (2/3/4 hexes)")]
    public int[] coneSizePerStar = { 2, 3, 4 };
    [Tooltip("Angle of the cone sweep in degrees")]
    public float coneAngle = 90f;
    [Tooltip("Duration to allow animation rotation (spin duration)")]
    public float spinDuration = 0.5f;

    [Header("Active: Jump Kick")]
    [Tooltip("Damage dealt per star level")]
    public float[] jumpKickDamage = { 300f, 450f, 850f };
    [Tooltip("Distance to knock back target in hexes")]
    public float knockbackDistance = 2f;
    [Tooltip("Knockback speed")]
    public float knockbackSpeed = 8f;

    [Header("VFX")]
    public GameObject sweepVFX;
    public GameObject jumpKickVFX;
    public GameObject knockbackVFX;

    [Header("Audio")]
    public AudioClip sweepSound;
    public AudioClip jumpKickSound;
    public AudioClip impactSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    private AudioSource audioSource;
    private bool isPerformingAbility = false;
    private UnitAI abilityTarget;

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

    private void Start()
    {
        if (unitAI != null)
        {
            unitAI.OnAttackEvent += OnAutoAttack;
        }

        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = true;
            Debug.Log($"[KuromushadoAbility] Apply Root Motion enabled for {unitAI.unitName}");
        }
    }

    private void OnDestroy()
    {
        if (unitAI != null)
        {
            unitAI.OnAttackEvent -= OnAutoAttack;
        }
    }

    private void OnAutoAttack(UnitAI target)
    {
        if (target == null || !target.isAlive) return;

        StartCoroutine(HandleSpinKickCone(target));
    }

    private IEnumerator HandleSpinKickCone(UnitAI target)
    {
        Vector3 initialForward = (target.transform.position - transform.position).normalized;
        initialForward.y = 0f;

        if (initialForward != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(initialForward);
        }

        yield return new WaitForSeconds(spinDuration);

        int coneSize = coneSizePerStar[Mathf.Clamp(unitAI.starLevel - 1, 0, coneSizePerStar.Length - 1)];

        Vector3 finalForward = transform.forward;
        List<UnitAI> enemiesInCone = FindEnemiesInCone(finalForward, coneSize);

        PlaySound(sweepSound);

        if (sweepVFX != null)
        {
            Vector3 vfxPosition = transform.position + Vector3.up * 1f + finalForward * 0.5f;
            Quaternion vfxRotation = Quaternion.LookRotation(finalForward);
            GameObject sweep = Instantiate(sweepVFX, vfxPosition, vfxRotation);
            sweep.transform.localScale = Vector3.one * coneSize;
            Destroy(sweep, 1.5f);
        }

        Debug.Log($"⚔️ {unitAI.unitName} cone sweep hit {enemiesInCone.Count} enemies in {coneSize} hex cone!");

        foreach (UnitAI enemy in enemiesInCone)
        {
            if (enemy != target)
            {
                enemy.TakeDamage(unitAI.attackDamage);
                Debug.Log($"💥 Cone damage: {unitAI.attackDamage} to {enemy.unitName}");
            }
        }
    }

    private List<UnitAI> FindEnemiesInCone(Vector3 direction, float range)
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
                float angle = Vector3.Angle(direction, toEnemy.normalized);

                if (angle <= coneAngle / 2f)
                {
                    enemiesInCone.Add(unit);
                }
            }
        }

        return enemiesInCone;
    }

    public void Cast(UnitAI target)
    {
        if (isPerformingAbility)
        {
            Debug.Log("[KuromushadoAbility] Already performing ability");
            return;
        }

        if (StageManager.Instance != null && StageManager.Instance.currentPhase != StageManager.GamePhase.Combat)
        {
            Debug.Log("[KuromushadoAbility] Cannot cast outside combat phase");
            return;
        }

        if (target == null || !target.isAlive)
        {
            target = FindClosestEnemy(6f);
            if (target == null)
            {
                Debug.Log("[KuromushadoAbility] No valid target for Jump Kick");
                return;
            }
        }

        abilityTarget = target;

        Vector3 direction = (target.transform.position - transform.position).normalized;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        isPerformingAbility = true;
        unitAI.isCastingAbility = true;

        if (unitAI.animator)
        {
            unitAI.animator.SetTrigger("AbilityTrigger");
        }

        PlaySound(jumpKickSound);
    }

    public void DealJumpKickDamage()
    {
        if (!unitAI.isAlive || abilityTarget == null)
        {
            FinishAbility();
            return;
        }

        if (abilityTarget.isAlive)
        {
            int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, jumpKickDamage.Length - 1);
            float damage = jumpKickDamage[starIndex];

            abilityTarget.TakeDamage(damage);
            PlaySound(impactSound);

            if (jumpKickVFX != null)
            {
                Vector3 vfxPosition = abilityTarget.transform.position + Vector3.up * 1.2f;
                GameObject kickVFX = Instantiate(jumpKickVFX, vfxPosition, Quaternion.identity);
                Destroy(kickVFX, 2f);
            }

            Debug.Log($"💥 {unitAI.unitName} Jump Kick: {damage} damage to {abilityTarget.unitName}");

            if (abilityTarget.isAlive)
            {
                StartCoroutine(ApplyKnockback(abilityTarget));
            }
            else
            {
                FinishAbility();
            }
        }
        else
        {
            FinishAbility();
        }
    }

    private IEnumerator ApplyKnockback(UnitAI target)
    {
        Vector3 knockbackDirection = (target.transform.position - transform.position).normalized;
        knockbackDirection.y = 0f;

        Vector3 startPosition = target.transform.position;
        Vector3 endPosition = startPosition + (knockbackDirection * knockbackDistance);

        if (knockbackVFX != null)
        {
            GameObject vfx = Instantiate(knockbackVFX, target.transform.position + Vector3.up * 1f, Quaternion.LookRotation(knockbackDirection));
            vfx.transform.SetParent(target.transform);
            Destroy(vfx, 1f);
        }

        float knockbackDuration = knockbackDistance / knockbackSpeed;
        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            if (target == null || !target.isAlive)
            {
                FinishAbility();
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / knockbackDuration;

            target.transform.position = Vector3.Lerp(startPosition, endPosition, t);

            yield return null;
        }

        if (target != null && target.isAlive)
        {
            target.transform.position = endPosition;
            Debug.Log($"🔨 {target.unitName} knocked back {knockbackDistance} hexes");
        }

        FinishAbility();
    }

    private void FinishAbility()
    {
        isPerformingAbility = false;
        unitAI.isCastingAbility = false;
        abilityTarget = null;

        unitAI.currentMana = 0f;
        if (unitAI.ui != null)
        {
            unitAI.ui.UpdateMana(unitAI.currentMana);
        }
    }

    private UnitAI FindClosestEnemy(float maxDistance)
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        UnitAI closest = null;
        float minDistance = maxDistance;

        foreach (UnitAI unit in allUnits)
        {
            if (unit == unitAI || !unit.isAlive || unit.team == unitAI.team || unit.currentState == UnitState.Bench)
                continue;

            float distance = Vector3.Distance(transform.position, unit.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = unit;
            }
        }

        return closest;
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
        if (isPerformingAbility)
        {
            StopAllCoroutines();
            isPerformingAbility = false;
            unitAI.isCastingAbility = false;
            abilityTarget = null;
        }

        Debug.Log($"[KuromushadoAbility] Round ended for {unitAI.unitName}");
    }
}
