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
    public float strikebyteRampDamageAmp2 = 1f;
    public float strikebyteRampDamageAmp3 = 2f;
    public float strikebyteMaxDamageAmp2 = 15f;
    public float strikebyteMaxDamageAmp3 = 25f;
    public float strikebyteRampAS2 = 0.05f;
    public float strikebyteRampAS3 = 0.10f;
    public float strikebyteMaxAS2 = 0.30f;
    public float strikebyteMaxAS3 = 0.50f;

    // ==============================
    // MAIN ENTRY
    // ==============================

    public void ApplyTraits(List<UnitAI> playerUnits)
    {
        Dictionary<Trait, int> traitCounts = CountTraits(playerUnits);
        ApplyBonuses(playerUnits, traitCounts);
    }

    private Dictionary<Trait, int> CountTraits(List<UnitAI> units)
    {
        Dictionary<Trait, int> counts = new Dictionary<Trait, int>();

        foreach (var unit in units)
        {
            foreach (var trait in unit.traits)
            {
                if (!counts.ContainsKey(trait))
                    counts[trait] = 0;

                counts[trait]++;
            }
        }

        return counts;
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
                            if (unit.traits.Contains(Trait.Eradicator))
                            {
                                var ability = unit.GetComponent<EradicatorTrait>();
                                if (ability == null) ability = unit.gameObject.AddComponent<EradicatorTrait>();

                                if (count >= eradicatorThreshold3)
                                    ability.executeThreshold = eradicatorExecuteThreshold3;
                                else
                                    ability.executeThreshold = eradicatorExecuteThreshold2;

                                ability.pressPrefab = hydraulicPressPrefab;
                            }
                        }
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
                            if (unit.traits.Contains(Trait.Clobbertron))
                            {
                                var ability = unit.GetComponent<ClobbertronTrait>();
                                if (ability == null) ability = unit.gameObject.AddComponent<ClobbertronTrait>();

                                ability.bonusArmor = clobbertronBonusArmor;
                                ability.bonusDamageAmp = clobbertronBonusDamageAmp;
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
                            if (unit.traits.Contains(Trait.Strikebyte))
                            {
                                var ability = unit.GetComponent<StrikebyteTrait>();
                                if (ability == null) ability = unit.gameObject.AddComponent<StrikebyteTrait>();

                                if (count >= strikebyteThreshold3)
                                {
                                    ability.rampDamageAmp = strikebyteRampDamageAmp3;
                                    ability.maxDamageAmp = strikebyteMaxDamageAmp3;
                                    ability.rampAS = strikebyteRampAS3;
                                    ability.maxAS = strikebyteMaxAS3;
                                }
                                else
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
