using UnityEngine;

public static class UnitAbilityDescriptions
{
    public static string GetDescription(UnitAI unit)
    {
        float ad = unit.attackDamage;
        float maxHp = unit.maxHealth;
        int star = unit.starLevel;

        switch (unit.unitName)
        {
            case "Needlebot":
                return GetNeedlebotDescription(unit, ad, star);

            case "ManaDrive":
                return GetManaDriveDescription(unit, ad, star);

            case "HyperShot":
                return GetHyperShotDescription(unit, ad, star);

            case "KillSwitch":
                return GetKillSwitchDescription(unit, ad, star);

            case "Haymaker":
                return GetHaymakerDescription(unit, ad, star);

            case "Haymaker Clone":
                return GetHaymakerCloneDescription(unit);

            case "BOP":
                return GetBOPDescription(unit, maxHp, star);
            case "Cobaltine":

                return GetCobaltineDescription(unit, ad, star);
            case "Kurōmushadō":
                return GetKuromushadoDescription(unit, ad, star);

            default:
                return "This unit has no ability description yet.";
        }
    }

    private static string GetNeedlebotDescription(UnitAI unit, float ad, int star)
    {
        var ability = unit.GetComponent<NeedleBotAbility>();
        if (ability == null) return "Needlebot ability missing.";

        float dmg = ability.damagePerStar[Mathf.Clamp(star - 1, 0, ability.damagePerStar.Length - 1)];
        int bonusNeedles = ability.needlesPerCast - ability.baseNeedleCount;
        string bonusText = bonusNeedles > 0 ? $" (+{bonusNeedles})" : "";

        return $"Active: Rapidly shoot {ability.needlesPerCast}{bonusText} needles split between the nearest 2 enemies within 2 hexes, each dealing {dmg + (ad * 0.6f)} damage.\n\n" +
               $"Every 10 needles shot, increase the needle count by 1 permanently.\n\n" +
               $"Bonus Needle Count: {bonusNeedles}";
    }
    private static string GetCobaltineDescription(UnitAI unit, float ad, int star)
    {
        var ability = unit.GetComponent<CobaltineAbility>();
        if (ability == null) return "Cobaltine ability missing.";

        int starIndex = Mathf.Clamp(star - 1, 0, 2);

        float healing = 30f;
        float armorDrain = 3f;
        float duration = 4f;
        float passiveConversion = 50f;

        if (ability.healingPerSecond != null && ability.healingPerSecond.Length > starIndex)
            healing = ability.healingPerSecond[starIndex];

        if (ability.armorDrainPerSecond != null && ability.armorDrainPerSecond.Length > starIndex)
            armorDrain = ability.armorDrainPerSecond[starIndex];

        if (ability.cloudDuration != null && ability.cloudDuration.Length > starIndex)
            duration = ability.cloudDuration[starIndex];

        if (ability.healToDamageConversion != null && ability.healToDamageConversion.Length > starIndex)
            passiveConversion = ability.healToDamageConversion[starIndex] * 100f;

        return $"Cast a spell in a 2 hex radius above Cobaltine, lowering all enemy armour by {armorDrain} and healing Cobaltine for {healing} health every second for {duration} seconds.\n\n" +
               $"Afterwards, Cobaltine's next auto attack deals {passiveConversion:F0}% of ALL healing received as bonus damage.";
    }


    private static string GetKuromushadoDescription(UnitAI unit, float ad, int star)
    {
        var ability = unit.GetComponent<KuromushadoAbility>();
        if (ability == null) return "Kurōmushadō ability missing.";

        int coneSize = ability.coneSizePerStar[Mathf.Clamp(star - 1, 0, ability.coneSizePerStar.Length - 1)];
        float kickDamage = ability.jumpKickDamage[Mathf.Clamp(star - 1, 0, ability.jumpKickDamage.Length - 1)];

        return $"Passive: Auto Attacks sweep in a {coneSize} hex cone, hitting all enemies within {ability.coneAngle}° of the target.\n\n" +
               $"Active: Jump Kick the target, dealing {kickDamage} damage and knocking them back {ability.knockbackDistance} hexes.";
    }

    private static string GetManaDriveDescription(UnitAI unit, float ad, int star)
    {
        var ability = unit.GetComponent<ManaDriveAbility>();
        if (ability == null) return "ManaDrive ability missing.";

        float dmg = ability.damagePerStar[Mathf.Clamp(star - 1, 0, ability.damagePerStar.Length - 1)];

        return $"Hurl a massive bomb towards the largest group of enemies, dealing {dmg + (ad * 1.5f)} damage.\n\n" +
               $"If the bomb kills a target, ManaDrive gains {ability.attackSpeedGain * 100:F0}% attack speed for the rest of combat and casts again, at {(1f - ability.recursiveDamageReduction) * 100:F0}% effectiveness.";
    }

    private static string GetHyperShotDescription(UnitAI unit, float ad, int star)
    {
        var ability = unit.GetComponent<HyperShotAbility>();
        if (ability == null) return "HyperShot ability missing.";

        int starIndex = Mathf.Clamp(star - 1, 0, ability.aoeDamagePerStar.Length - 1);
        float aoeDamage = ability.aoeDamagePerStar[starIndex];

        return $"Passive: Every {ability.attacksToTrigger} attacks, trigger an explosion dealing {aoeDamage + (ad * 0.75f)} damage to all enemies in a {ability.aoeRadius} hex radius.\n\n" +
               $"Active: Gain +{ability.attackSpeedBonus * 100:F0}% attack speed for {ability.buffDuration} seconds. \n\nThis effect stacks!";
    }


    private static string GetKillSwitchDescription(UnitAI unit, float ad, int star)
    {
        var ability = unit.GetComponent<KillSwitchAbility>();
        if (ability == null) return "KillSwitch ability missing.";

        float slamDmg = ability.slamDamagePerStar[Mathf.Clamp(star - 1, 0, ability.slamDamagePerStar.Length - 1)];

        return $"Passive: Every other attack lowers the target's armour by {ability.armorShred}.\n\n" +
               $"Active: Leap to the farthest enemy within 4 hexes, heal {ability.healOnTargetSwap} hp and slam them for {slamDmg + (ad * 0.4f)} damage. Grant KillSwitch 50% attack speed for 4 seconds.";
    }

    private static string GetHaymakerDescription(UnitAI unit, float ad, int star)
    {
        var ability = unit.GetComponent<HaymakerAbility>();
        if (ability == null) return "Haymaker ability missing.";

        float slashDmg = ability.slashDamage[Mathf.Clamp(star - 1, 0, ability.slashDamage.Length - 1)];
        float slamDmg = ability.slamDamage[Mathf.Clamp(star - 1, 0, ability.slamDamage.Length - 1)];

        int starIndex = Mathf.Clamp(star - 1, 0, ability.temporaryArmor.Length - 1);
        int damageReductionPercent = 80 + (starIndex * 10);

        return $"Passive: Summon a clone of Haymaker with 25% health and damage. The clone does not benefit from traits.\n\n" +
               $"When units on the board die, Haymaker absorbs their soul. The clone gains 1% health and damage for every 5 souls absorbed.\n\n" +
               $"Active: Dash to the center clump of enemies and unleash a fury of slashes within 3 hexes that each do {slashDmg + (ad * 1.2f)} damage for 3 seconds. While slashing take {damageReductionPercent}% reduced damage. Then dash back to original position.\n\n" +
               $"Then, the clone will slam onto the final target, dealing {slamDmg + (ad * 1.2f)} damage. ";
    }

    private static string GetHaymakerCloneDescription(UnitAI unit)
    {
        var cloneComponent = unit.GetComponent<HaymakerClone>();
        if (cloneComponent != null)
        {
            float slamDmg = cloneComponent.GetSlamDamage();
            string soulStatus = cloneComponent.GetDetailedSoulStatus();

            return $"Passive: Gains +1% health & damage for every 5 enemy souls absorbed by Haymaker.\n\n" +
                   $"Soul Status: {soulStatus}\n\n" +
                   $"Active: Slams down at the last hit target when Haymaker finishes casting, dealing {slamDmg} damage.";
        }

        return "Slams down at the last hit target when Haymaker finishes casting.";
    }

    private static string GetBOPDescription(UnitAI unit, float maxHp, int star)
    {
        var ability = unit.GetComponent<BOPAbility>();
        if (ability == null) return "B.O.P ability missing.";

        float buffPercent = ability.chestBuffPercent[Mathf.Clamp(star - 1, 0, ability.chestBuffPercent.Length - 1)];
        float dmgAmp = ability.damageAmpPerStar[Mathf.Clamp(star - 1, 0, ability.damageAmpPerStar.Length - 1)];
        float bonkDmg = (maxHp * 0.2f) + dmgAmp;

        return $"B.O.P. pounds its chest, granting itself +{buffPercent * 100:F0}% max health.\n\n" +
               $"Then bonk the target, dealing 20% of B.O.P.'s max health + Attack Damage as damage ({bonkDmg:F0} damage).";
    }
}
