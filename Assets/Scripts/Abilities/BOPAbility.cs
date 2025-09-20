using UnityEngine;
using static UnitAI;

public class BOPAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;

    [Header("Ability Stats")]
    public float[] damageAmpPerStar = { 30, 40, 50 };
    public float[] chestBuffPercent = { 0.1f, 0.12f, 0.15f };

    [Header("Optional VFX")]
    public GameObject chestVFX;
    public GameObject strikeVFX;

    [Header("🔊 BOP Ability Audio")]
    [Tooltip("Audio played when ability is activated")]
    public AudioClip abilityStartSound;

    [Tooltip("Audio played during chest pound moment")]
    public AudioClip chestPoundSound;

    [Tooltip("Audio played during bonk strike")]
    public AudioClip bonkStrikeSound;

    [Tooltip("Audio played when gaining health buff")]
    public AudioClip healthBuffSound;

    [Tooltip("Volume for ability audio")]
    [Range(0f, 1f)]
    public float abilityAudioVolume = 1f;

    // ✅ Audio system
    private AudioSource abilityAudioSource;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
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
        abilityAudioSource.spatialBlend = 0.3f; // Slight 3D positioning
        abilityAudioSource.volume = abilityAudioVolume;

        Debug.Log($"🔊 BOP ability audio system setup for {unitAI.unitName}");
    }

    // ✅ NEW: Play ability audio
    private void PlayAbilityAudio(AudioClip clip, string actionName = "")
    {
        if (clip != null && abilityAudioSource != null)
        {
            abilityAudioSource.PlayOneShot(clip, abilityAudioVolume);
            Debug.Log($"🔊 {unitAI.unitName} BOP played {actionName} audio");
        }
    }

    // ✅ Interface implementation
    public AudioClip GetAbilityAudio()
    {
        return abilityStartSound;
    }

    // Called by UnitAI when mana is full
    public void Cast(UnitAI target)
    {
        // ✅ Play ability start audio
        PlayAbilityAudio(abilityStartSound, "ability start");

        // Trigger chest pound animation
        if (unitAI.animator != null)
            unitAI.animator.SetTrigger("AbilityTrigger");

        Debug.Log($"💪 {unitAI.unitName} starts BOP ability!");
    }

    // 👊 Animation Event: Chest Pound frame
    public void ApplyChestBuff()
    {
        // ✅ Play chest pound audio
        PlayAbilityAudio(chestPoundSound, "chest pound");

        float buffAmount = unitAI.maxHealth * chestBuffPercent[
            Mathf.Clamp(unitAI.starLevel - 1, 0, chestBuffPercent.Length - 1)
        ];

        // store as a bonus so recalculations won't wipe it
        unitAI.bonusMaxHealth += buffAmount;

        // recompute effective max (updates unitAI.maxHealth and the unit UI)
        unitAI.RecalculateMaxHealth();

        // heal current health by the same amount
        unitAI.currentHealth += buffAmount;
        if (unitAI.currentHealth > unitAI.maxHealth) unitAI.currentHealth = unitAI.maxHealth;

        // ✅ Play health buff audio
        PlayAbilityAudio(healthBuffSound, "health buff");

        // ensure UI shows the new current health
        if (unitAI.ui != null)
            unitAI.ui.UpdateHealth(unitAI.currentHealth);

        // refresh Unit Info Panel if it's showing this unit
        if (UnitInfoPanelManager.Instance != null)
            UnitInfoPanelManager.Instance.RefreshActivePanelIfMatches(unitAI);

        if (chestVFX != null)
        {
            GameObject effect = Instantiate(chestVFX, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        Debug.Log($"💪 {unitAI.unitName} pounds chest! +{buffAmount:F1} max HP (now {unitAI.maxHealth:F1}).");
    }

    // 🔨 Animation Event: Bonk Strike frame
    public void DoBonkDamage()
    {
        UnitAI target = unitAI.GetCurrentTarget();
        if (target != null && target.isAlive && target.currentState != UnitState.Bench)
        {
            // ✅ Play bonk strike audio
            PlayAbilityAudio(bonkStrikeSound, "bonk strike");

            float damageAmp = damageAmpPerStar[Mathf.Clamp(unitAI.starLevel - 1, 0, damageAmpPerStar.Length - 1)];
            float damage = (unitAI.maxHealth * 0.2f) + damageAmp;

            target.TakeDamage(damage);

            if (strikeVFX != null)
            {
                GameObject effect = Instantiate(strikeVFX, target.transform.position, Quaternion.identity);
                Destroy(effect, 3f);
            }

            Debug.Log($"🔨 {unitAI.unitName} BONKS {target.unitName} for {damage} damage!");
        }
        else
        {
            Debug.Log($"⚠️ {unitAI.unitName} tried to bonk but no valid target!");
        }
    }
}
