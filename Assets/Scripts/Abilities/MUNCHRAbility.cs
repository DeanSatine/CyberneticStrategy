using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class MUNCHRAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;

    [Header("Active Ability - Eat Target")]
    [Tooltip("Damage reduction while eating per star level (35/45/99%)")]
    public float[] damageReductionPercent = { 35f, 45f, 99f };

    [Tooltip("Total damage dealt over eating duration per star level")]
    public float[] totalEatingDamage = { 800f, 1200f, 9999f };

    [Tooltip("Shield percent of max health after eating (25/35/100%)")]
    public float[] shieldPercent = { 0.25f, 0.35f, 1.0f };

    [Tooltip("Duration of eating in seconds")]
    public float eatingDuration = 5f;

    [Tooltip("How often to tick damage during eating")]
    public float damageTickInterval = 0.5f;

    [Header("Passive - O.B.C.D Trait (Feed Allies)")]
    [Tooltip("Stat bonuses per class when feeding allies")]
    public float attackClassBonus = 15f;      // AttackMarksman, AttackFighter, AttackCaster, AttackTank
    public float magicClassBonus = 15f;       // MagicMarksman, MagicFighter, MagicCaster, MagicTank
    public float hybridClassBonus = 10f;      // HybridMarksman, HybridCaster
    public float tankClassHealthBonus = 100f; // Tank classes grant bonus HP

    private bool hasBeenFedThisPrep = false;
    private Dictionary<UnitClass, StatBonus> permanentBonuses = new Dictionary<UnitClass, StatBonus>();

    [Header("Stat Bonuses from Eaten Enemies")]
    [Tooltip("Stats gained when enemy dies in stomach")]
    public float eatenAttackClassBonus = 10f;
    public float eatenMagicClassBonus = 10f;
    public float eatenHybridClassBonus = 7f;
    public float eatenTankClassHealthBonus = 75f;

    [Header("Mouth Animation")]
    [Tooltip("Reference to the top jaw/mouth that will open")]
    public Transform topJaw;

    [Tooltip("How much to rotate the top jaw when opening (in degrees)")]
    public float jawOpenAngle = 25f;

    [Tooltip("How fast the jaw opens/closes")]
    public float jawAnimationSpeed = 5f;

    [Tooltip("Shake intensity while eating")]
    public float shakeIntensity = 0.1f;

    [Tooltip("Shake frequency while eating")]
    public float shakeFrequency = 10f;

    [Header("VFX & Audio")]
    public GameObject eatingVFX;
    public GameObject burpVFX;
    public GameObject feedVFX;
    public AudioClip chompSound;
    public AudioClip eatingSound;
    public AudioClip burpSound;
    public AudioClip digestSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    private AudioSource audioSource;
    private bool isEating = false;
    private UnitAI currentVictim;
    private Coroutine eatingCoroutine;
    private Quaternion originalJawRotation;
    private Vector3 originalJawPosition;
    private float originalDamageReduction;

    [System.Serializable]
    private struct StatBonus
    {
        public float health;
        public float attackDamage;
        public float abilityPower;
        public float armor;
        public float magicResist;
        public int count;
    }

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        SetupAudio();

        if (topJaw != null)
        {
            originalJawRotation = topJaw.localRotation;
            originalJawPosition = topJaw.localPosition;
        }

        originalDamageReduction = unitAI.damageReduction;
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

    public void Cast(UnitAI target)
    {
        if (!unitAI.isAlive || isEating) return;

        if (target == null || !target.isAlive)
        {
            Debug.LogWarning($"[MUNCH-R] No valid target to eat!");
            return;
        }

        StartEating(target);
    }

    private void StartEating(UnitAI target)
    {
        isEating = true;
        currentVictim = target;

        unitAI.currentMana = 0f;
        if (unitAI.ui != null)
        {
            unitAI.ui.UpdateMana(0f);
        }

        int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, damageReductionPercent.Length - 1);
        float damageReduction = damageReductionPercent[starIndex];

        unitAI.damageReduction = damageReduction;

        if (unitAI.animator)
        {
            unitAI.animator.SetTrigger("AbilityTrigger");
        }

        PlaySound(chompSound);

        eatingCoroutine = StartCoroutine(EatingSequence(target, starIndex));

        Debug.Log($"🍴 {unitAI.unitName} starts eating {target.unitName}! ({damageReduction}% damage reduction)");
    }

    private IEnumerator EatingSequence(UnitAI victim, int starIndex)
    {
        victim.gameObject.SetActive(false);

        StartCoroutine(AnimateJawOpen());
        StartCoroutine(ShakeJawWhileEating());

        if (eatingVFX != null)
        {
            GameObject vfx = Instantiate(eatingVFX, transform.position + Vector3.up * 1.5f, Quaternion.identity, transform);
            Destroy(vfx, eatingDuration);
        }

        PlaySound(eatingSound);

        float totalDamage = totalEatingDamage[starIndex];
        float damagePerTick = totalDamage / (eatingDuration / damageTickInterval);
        float elapsed = 0f;
        bool victimDied = false;

        while (elapsed < eatingDuration && victim != null && victim.isAlive)
        {
            yield return new WaitForSeconds(damageTickInterval);
            elapsed += damageTickInterval;

            if (victim != null && victim.isAlive)
            {
                victim.TakeDamage(damagePerTick);

                if (!victim.isAlive)
                {
                    victimDied = true;
                    PlaySound(digestSound);
                    Debug.Log($"💀 {victim.unitName} was digested by {unitAI.unitName}!");
                    break;
                }
            }
        }

        StartCoroutine(AnimateJawClose());

        unitAI.damageReduction = originalDamageReduction;

        if (victimDied && victim != null)
        {
            OnVictimDigested(victim);
        }
        else if (victim != null && victim.isAlive)
        {
            SpitOutVictim(victim);
        }

        ApplyShield(starIndex);

        PlaySound(burpSound);
        if (burpVFX != null)
        {
            GameObject vfx = Instantiate(burpVFX, transform.position + transform.forward * 1f + Vector3.up * 1.5f, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        isEating = false;
        currentVictim = null;

        Debug.Log($"😋 {unitAI.unitName} finished eating!");
    }

    private void SpitOutVictim(UnitAI victim)
    {
        victim.gameObject.SetActive(true);

        Vector3 spitPosition = transform.position + transform.forward * 2f;
        victim.transform.position = spitPosition;

        Debug.Log($"💨 {unitAI.unitName} spits out {victim.unitName}!");
    }

    private void OnVictimDigested(UnitAI victim)
    {
        UnitClass victimClass = victim.classification;
        ApplyPermanentStats(victimClass, true);

        Debug.Log($"📈 {unitAI.unitName} gained permanent stats from eating {victim.unitName} ({victimClass})!");
    }

    private void ApplyShield(int starIndex)
    {
        float shieldAmount = unitAI.maxHealth * shieldPercent[starIndex];

        ShieldComponent shieldComp = GetComponent<ShieldComponent>();
        if (shieldComp == null)
        {
            shieldComp = gameObject.AddComponent<ShieldComponent>();
        }

        shieldComp.AddShield(shieldAmount, 999f);

        Debug.Log($"🛡️ {unitAI.unitName} gained {shieldAmount:F0} shield ({shieldPercent[starIndex] * 100:F0}% max HP)!");
    }

    private IEnumerator AnimateJawOpen()
    {
        if (topJaw == null) yield break;

        float elapsed = 0f;
        Quaternion targetRotation = originalJawRotation * Quaternion.Euler(-jawOpenAngle, 0f, 0f);

        while (elapsed < 1f / jawAnimationSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (1f / jawAnimationSpeed);
            topJaw.localRotation = Quaternion.Lerp(originalJawRotation, targetRotation, t);
            yield return null;
        }

        topJaw.localRotation = targetRotation;
    }

    private IEnumerator AnimateJawClose()
    {
        if (topJaw == null) yield break;

        StopCoroutine(ShakeJawWhileEating());

        float elapsed = 0f;
        Quaternion currentRotation = topJaw.localRotation;

        while (elapsed < 1f / jawAnimationSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (1f / jawAnimationSpeed);
            topJaw.localRotation = Quaternion.Lerp(currentRotation, originalJawRotation, t);
            topJaw.localPosition = Vector3.Lerp(topJaw.localPosition, originalJawPosition, t);
            yield return null;
        }

        topJaw.localRotation = originalJawRotation;
        topJaw.localPosition = originalJawPosition;
    }

    private IEnumerator ShakeJawWhileEating()
    {
        if (topJaw == null) yield break;

        while (isEating)
        {
            float shakeX = Mathf.Sin(Time.time * shakeFrequency) * shakeIntensity;
            float shakeY = Mathf.Cos(Time.time * shakeFrequency * 1.3f) * shakeIntensity;

            topJaw.localPosition = originalJawPosition + new Vector3(shakeX, shakeY, 0f);

            yield return null;
        }

        topJaw.localPosition = originalJawPosition;
    }

    public void FeedAlly(UnitAI allyToFeed)
    {
        if (hasBeenFedThisPrep)
        {
            Debug.LogWarning($"[MUNCH-R] Already been fed this prep phase!");
            return;
        }

        if (allyToFeed == null || allyToFeed == unitAI)
        {
            Debug.LogWarning($"[MUNCH-R] Invalid ally to feed!");
            return;
        }

        hasBeenFedThisPrep = true;

        UnitClass allyClass = allyToFeed.classification;
        ApplyPermanentStats(allyClass, false);

        Destroy(allyToFeed.gameObject);

        if (feedVFX != null)
        {
            GameObject vfx = Instantiate(feedVFX, transform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        PlaySound(chompSound);

        Debug.Log($"🍽️ {unitAI.unitName} was fed {allyToFeed.unitName} ({allyClass}) and gained permanent stats!");
    }

    private void ApplyPermanentStats(UnitClass unitClass, bool fromEnemy)
    {
        if (!permanentBonuses.ContainsKey(unitClass))
        {
            permanentBonuses[unitClass] = new StatBonus();
        }

        StatBonus bonus = permanentBonuses[unitClass];
        bonus.count++;

        float adBonus = 0f;
        float apBonus = 0f;
        float hpBonus = 0f;
        float armorBonus = 0f;
        float mrBonus = 0f;

        switch (unitClass)
        {
            case UnitClass.AttackMarksman:
            case UnitClass.AttackFighter:
            case UnitClass.AttackCaster:
                adBonus = fromEnemy ? eatenAttackClassBonus : attackClassBonus;
                break;

            case UnitClass.AttackTank:
                adBonus = fromEnemy ? eatenAttackClassBonus : attackClassBonus;
                hpBonus = fromEnemy ? eatenTankClassHealthBonus : tankClassHealthBonus;
                armorBonus = 5f;
                break;

            case UnitClass.MagicMarksman:
            case UnitClass.MagicFighter:
            case UnitClass.MagicCaster:
                apBonus = fromEnemy ? eatenMagicClassBonus : magicClassBonus;
                break;

            case UnitClass.MagicTank:
                apBonus = fromEnemy ? eatenMagicClassBonus : magicClassBonus;
                hpBonus = fromEnemy ? eatenTankClassHealthBonus : tankClassHealthBonus;
                mrBonus = 5f;
                break;

            case UnitClass.HybridMarksman:
            case UnitClass.HybridCaster:
                adBonus = fromEnemy ? eatenHybridClassBonus * 0.5f : hybridClassBonus * 0.5f;
                apBonus = fromEnemy ? eatenHybridClassBonus * 0.5f : hybridClassBonus * 0.5f;
                break;
        }

        bonus.attackDamage += adBonus;
        bonus.abilityPower += apBonus;
        bonus.health += hpBonus;
        bonus.armor += armorBonus;
        bonus.magicResist += mrBonus;

        permanentBonuses[unitClass] = bonus;

        unitAI.attackDamage += adBonus;
        unitAI.abilityPower += apBonus;
        unitAI.armor += armorBonus;
        unitAI.magicResist += mrBonus;

        if (hpBonus > 0f)
        {
            unitAI.bonusMaxHealth += hpBonus;
            unitAI.RecalculateMaxHealth();
            unitAI.currentHealth += hpBonus;

            if (unitAI.ui != null)
            {
                unitAI.ui.UpdateHealth(unitAI.currentHealth);
            }
        }

        Debug.Log($"📊 {unitAI.unitName} stat update: +{adBonus} AD, +{apBonus} AP, +{hpBonus} HP, +{armorBonus} Armor, +{mrBonus} MR");
    }

    public void OnRoundEnd()
    {
        if (isEating && eatingCoroutine != null)
        {
            StopCoroutine(eatingCoroutine);

            if (currentVictim != null && currentVictim.gameObject != null)
            {
                currentVictim.gameObject.SetActive(true);
            }

            isEating = false;
            currentVictim = null;
        }

        unitAI.damageReduction = originalDamageReduction;

        if (topJaw != null)
        {
            topJaw.localRotation = originalJawRotation;
            topJaw.localPosition = originalJawPosition;
        }
    }

    public void OnPrepPhaseStart()
    {
        hasBeenFedThisPrep = false;
        Debug.Log($"🔄 {unitAI.unitName} can be fed an ally this prep phase!");
    }

    public string GetStatSummary()
    {
        string summary = "Fed Units:\n";

        foreach (var kvp in permanentBonuses)
        {
            StatBonus bonus = kvp.Value;
            if (bonus.count > 0)
            {
                summary += $"{kvp.Key} x{bonus.count}: +{bonus.attackDamage} AD, +{bonus.abilityPower} AP, +{bonus.health} HP\n";
            }
        }

        return summary;
    }

    private void OnDisable()
    {
        OnRoundEnd();
    }
}
