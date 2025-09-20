using System.Collections.Generic;
using UnityEngine;

public class TraitManager : MonoBehaviour
{
    public static TraitManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    private void Start()
    {
        SetupTraitAudio();
    }

    private void SetupTraitAudio()
    {
        traitAudioSource = GetComponent<AudioSource>();
        if (traitAudioSource == null)
        {
            traitAudioSource = gameObject.AddComponent<AudioSource>();
        }

        traitAudioSource.playOnAwake = false;
        traitAudioSource.spatialBlend = 0.5f; // Balanced 2D/3D audio for trait effects
        traitAudioSource.volume = slamAudioVolume;

        Debug.Log("🔊 TraitManager audio system initialized");
    }

    // Public method for playing slam sound - can be called by EradicatorTrait
    public void PlayEradicatorSlamSound()
    {
        if (eradicatorSlamSound != null && traitAudioSource != null)
        {
            traitAudioSource.PlayOneShot(eradicatorSlamSound, slamAudioVolume);
            Debug.Log("🔊 TraitManager played Eradicator SLAM sound!");
        }
    }

    // ==============================
    // TRAIT SETTINGS (tweak in Inspector)
    // ==============================

    [Header("Eradicators")]
    public int eradicatorThreshold2 = 2;
    public int eradicatorThreshold3 = 3;
    public float eradicatorExecuteThreshold2 = 0.10f; // 10%
    public float eradicatorExecuteThreshold3 = 0.15f; // 15%
    public GameObject hydraulicPressPrefab;

    [Header("Bulkhead")]
    public int bulkheadThreshold = 2;
    public float bulkheadBonusHealthPercent = 0.25f;
    public float bulkheadDeathSharePercent = 0.5f;

    [Header("Clobbertron")]
    public int clobbertronThreshold = 2;
    public float clobbertronBonusArmor = 10f;
    public float clobbertronBonusDamageAmp = 0.10f;
    public float clobbertronCrashRadius = 2f;
    public float clobbertronCrashDamage = 200f;

    [Header("Strikebyte")]
    public int strikebyteThreshold2 = 2;
    public int strikebyteThreshold3 = 3;
    public int strikebyteThreshold4 = 4;

    // --- Damage Ramping ---
    public float strikebyteRampDamageAmp2 = 1f;
    public float strikebyteRampDamageAmp3 = 2f;
    public float strikebyteRampDamageAmp4 = 3f;

    public float strikebyteMaxDamageAmp2 = 15f;
    public float strikebyteMaxDamageAmp3 = 25f;
    public float strikebyteMaxDamageAmp4 = 30f;

    // --- Attack Speed Ramping ---
    public float strikebyteRampAS2 = 0.05f;
    public float strikebyteRampAS3 = 0.10f;
    public float strikebyteRampAS4 = 0.15f;

    public float strikebyteMaxAS2 = 0.30f;
    public float strikebyteMaxAS3 = 0.40f;
    public float strikebyteMaxAS4 = 0.50f;
    [Header("Eradicator Audio")]
    [Tooltip("Sound played when the hydraulic press slams down")]
    public AudioClip eradicatorSlamSound;

    [Tooltip("Volume for eradicator slam audio")]
    [Range(0f, 1f)]
    public float slamAudioVolume = 1f;

    // Audio system
    private AudioSource traitAudioSource;


    // ==============================
    // MAIN ENTRY
    // ==============================

    public void ApplyTraits(List<UnitAI> playerUnits)
    {
        Dictionary<Trait, int> traitCounts = CountTraits(playerUnits);
        ApplyBonuses(playerUnits, traitCounts);
    }

    public Dictionary<Trait, int> CountTraits(List<UnitAI> units)
    {
        Dictionary<Trait, int> counts = new Dictionary<Trait, int>();
        HashSet<string> contributedUnits = new HashSet<string>();

        foreach (var unit in units)
        {
            if (unit == null) continue;

            // ❌ Skip benched units
            if (unit.currentState == UnitAI.UnitState.Bench)
                continue;

            // ❌ Skip if this unit type already contributed
            if (contributedUnits.Contains(unit.unitName))
                continue;

            contributedUnits.Add(unit.unitName);

            foreach (var trait in unit.traits)
            {
                if (!counts.ContainsKey(trait))
                    counts[trait] = 0;

                counts[trait]++;
            }
        }

        return counts;
    }


    public bool IsTraitActive(Trait trait, int count)
    {
        switch (trait)
        {
            case Trait.Eradicator:
                return (count >= eradicatorThreshold2 || count >= eradicatorThreshold3);

            case Trait.Bulkhead:
                return (count >= bulkheadThreshold);

            case Trait.Clobbertron:
                return (count >= clobbertronThreshold);

            case Trait.Strikebyte:
                return (count >= strikebyteThreshold2 || count >= strikebyteThreshold3 || count >= strikebyteThreshold4);
        }
        return false;
    }
    public int GetCurrentTier(Trait trait, int count)
    {
        switch (trait)
        {
            case Trait.Eradicator:
                if (count >= eradicatorThreshold3) return 3;
                if (count >= eradicatorThreshold2) return 2;
                break;

            case Trait.Bulkhead:
                if (count >= bulkheadThreshold) return 2;
                break;

            case Trait.Clobbertron:
                if (count >= clobbertronThreshold) return 2;
                break;

            case Trait.Strikebyte:
                if (count >= strikebyteThreshold4) return 4;
                if (count >= strikebyteThreshold3) return 3;
                if (count >= strikebyteThreshold2) return 2;
                break;
        }
        return 0;
    }
    public (int activeThreshold, int nextThreshold) GetBreakpoints(Trait trait, int count)
    {
        switch (trait)
        {
            case Trait.Eradicator:
                if (count >= eradicatorThreshold3) return (eradicatorThreshold3, 0);
                if (count >= eradicatorThreshold2) return (eradicatorThreshold2, eradicatorThreshold3);
                return (0, eradicatorThreshold2);

            case Trait.Bulkhead:
                if (count >= bulkheadThreshold) return (bulkheadThreshold, 0);
                return (0, bulkheadThreshold);

            case Trait.Clobbertron:
                if (count >= clobbertronThreshold) return (clobbertronThreshold, 0);
                return (0, clobbertronThreshold);

            case Trait.Strikebyte:
                if (count >= strikebyteThreshold4) return (strikebyteThreshold4, 0);
                if (count >= strikebyteThreshold3) return (strikebyteThreshold3, strikebyteThreshold4);
                if (count >= strikebyteThreshold2) return (strikebyteThreshold2, strikebyteThreshold3);
                return (0, strikebyteThreshold2);
        }
        return (0, 0);
    }
    public void EvaluateTraits(List<UnitAI> playerUnits)
    {
        Dictionary<Trait, int> traitCounts = new Dictionary<Trait, int>();
        HashSet<string> contributedUnits = new HashSet<string>();

        foreach (var unit in playerUnits)
        {
            if (unit == null) continue;

            // ❌ Skip benched units
            if (unit.currentState == UnitAI.UnitState.Bench)
                continue;

            if (contributedUnits.Contains(unit.unitName))
                continue;

            contributedUnits.Add(unit.unitName);

            foreach (var trait in unit.traits)
            {
                if (!traitCounts.ContainsKey(trait))
                    traitCounts[trait] = 0;

                traitCounts[trait]++;
            }
        }

        // ✅ push to UI
        TraitUIManager.Instance.UpdateTraitUI(traitCounts);
    }


    private void ApplyBonuses(List<UnitAI> playerUnits, Dictionary<Trait, int> activeTraits)
    {
        foreach (var kvp in activeTraits)
        {
            Trait trait = kvp.Key;
            int count = kvp.Value;

            switch (trait)
            {
                // =====================
                // ERADICATOR
                // =====================
                case Trait.Eradicator:
                    if (count >= eradicatorThreshold2)
                    {
                        foreach (var unit in playerUnits)
                        {
                            if (unit.currentState == UnitAI.UnitState.Bench) continue;

                            if (unit.traits.Contains(Trait.Eradicator))
                            {
                                var ability = unit.GetComponent<EradicatorTrait>();
                                if (ability == null) ability = unit.gameObject.AddComponent<EradicatorTrait>();

                                ability.executeThreshold = (count >= eradicatorThreshold3)
                                    ? eradicatorExecuteThreshold3
                                    : eradicatorExecuteThreshold2;

                                ability.pressPrefab = hydraulicPressPrefab;
                                ability.SpawnPressIfNeeded();
                            }
                        }
                    }
                    else
                    {
                        // ✅ Trait inactive → clean up press globally
                        EradicatorTrait.ResetAllEradicators();
                    }
                    break;


                // =====================
                // BULKHEAD
                // =====================
                case Trait.Bulkhead:

                    if (count >= bulkheadThreshold)
                    {
                        foreach (var unit in playerUnits)
                        {
                            if (unit.currentState == UnitAI.UnitState.Bench) continue;

                            if (unit.traits.Contains(Trait.Bulkhead))
                            {
                                var ability = unit.GetComponent<BulkheadTrait>();
                                if (ability == null) ability = unit.gameObject.AddComponent<BulkheadTrait>();

                                ability.bonusHealthPercent = bulkheadBonusHealthPercent;
                                ability.deathSharePercent = bulkheadDeathSharePercent;
                            }
                        }
                    }
                    break;

                // =====================
                // CLOBBERTRON
                // =====================
                case Trait.Clobbertron:
                    if (count >= clobbertronThreshold)
                    {
                        foreach (var unit in playerUnits)
                        {
                            if (unit.currentState == UnitAI.UnitState.Bench) continue;

                            if (unit.traits.Contains(Trait.Clobbertron))
                            {
                                var ability = unit.GetComponent<ClobbertronTrait>();
                                if (ability == null) ability = unit.gameObject.AddComponent<ClobbertronTrait>();

                                ability.bonusArmor = clobbertronBonusArmor;
                                ability.crashRadius = clobbertronCrashRadius;
                                ability.crashDamage = clobbertronCrashDamage;
                            }
                        }
                    }
                    break;

                // =====================
                // STRIKEBYTE
                // =====================
                case Trait.Strikebyte:
                    if (count >= strikebyteThreshold2)
                    {
                        foreach (var unit in playerUnits)
                        {
                            if (unit.currentState == UnitAI.UnitState.Bench) continue;

                            if (unit.traits.Contains(Trait.Strikebyte))
                            {
                                var ability = unit.GetComponent<StrikebyteTrait>();
                                if (ability == null) ability = unit.gameObject.AddComponent<StrikebyteTrait>();

                                if (count >= strikebyteThreshold4)
                                {
                                    ability.rampDamageAmp = strikebyteRampDamageAmp4;
                                    ability.maxDamageAmp = strikebyteMaxDamageAmp4;
                                    ability.rampAS = strikebyteRampAS4;
                                    ability.maxAS = strikebyteMaxAS4;
                                }
                                else if (count >= strikebyteThreshold3)
                                {
                                    ability.rampDamageAmp = strikebyteRampDamageAmp3;
                                    ability.maxDamageAmp = strikebyteMaxDamageAmp3;
                                    ability.rampAS = strikebyteRampAS3;
                                    ability.maxAS = strikebyteMaxAS3;
                                }
                                else // Tier 2
                                {
                                    ability.rampDamageAmp = strikebyteRampDamageAmp2;
                                    ability.maxDamageAmp = strikebyteMaxDamageAmp2;
                                    ability.rampAS = strikebyteRampAS2;
                                    ability.maxAS = strikebyteMaxAS2;
                                }
                            }
                        }
                    }
                    break;
            }
        }
    }
}
