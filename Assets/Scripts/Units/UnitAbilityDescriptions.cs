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

            case "Coreweaver":
                return GetCoreweaverDescription(unit, ad, star);

            case "BackSlash":
                return GetBackSlashDescription(unit, ad, star);

            case "SteelGuard":
                return GetSteelGuardDescription(unit, ad, star);

            case "Sightline":
                return GetSightlineDescription(unit, ad, star);
            case "TracerCore":
                return GetTracerCoreDescription(unit, ad, star);

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

    return $"Active: Rapidly shoot {ability.needlesPerCast}{bonusText} needles split between the nearest 2 enemies within 2 hexes, each dealing {dmg + (ad * 0.6f)} <color=#FF6600>physical damage</color>.\n\n" +
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
           $"Afterwards, Cobaltine's next auto attack deals {passiveConversion:F0}% of ALL healing received as bonus <color=#00BFFF>magic damage</color> (scales with {unit.abilityPower * 0.5f:F0} AP).";
}
    private static string GetSightlineDescription(UnitAI unit, float ad, int star)
    {
        var ability = unit.GetComponent<SightlineAbility>();
        if (ability == null) return "Sightline ability missing.";

        int starIndex = Mathf.Clamp(star - 1, 0, 2);
        float dps = ability.damagePerSecond[starIndex];
        float duration = ability.durationPerStar[starIndex];
        int stacks = ability.GetCurrentStacks();
        float asBonus = stacks * ability.attackSpeedPerStack * 100f;
        int turretCount = ability.GetActiveTurretCount();

        string turretStatus = turretCount > 0 ? $" (Currently {turretCount} active)" : "";

        return $"Passive: Attacks grant {ability.attackSpeedPerStack * 100:F0}% stacking attack speed.\n\n "+
               $"Active: Summon a portal that shoots piercing lasers {dps:F0} <color=#FF6600>physical damage</color> " +
               $"every second for {duration} seconds. Subsequent casts summon another portal.{turretStatus}";
    }
    private static string GetKuromushadoDescription(UnitAI unit, float ad, int star)
{
    var ability = unit.GetComponent<KuromushadoAbility>();
    if (ability == null) return "Kurōmushadō ability missing.";

    int coneSize = ability.coneSizePerStar[Mathf.Clamp(star - 1, 0, ability.coneSizePerStar.Length - 1)];
    float kickDamage = ability.jumpKickDamage[Mathf.Clamp(star - 1, 0, ability.jumpKickDamage.Length - 1)];

    return $"Passive: Auto Attacks sweep in a {coneSize} hex cone, hitting all enemies within {ability.coneAngle}° of the target.\n\n" +
           $"Active: Jump Kick the target, dealing {kickDamage} <color=#FF6600>physical damage</color> and knocking them back {ability.knockbackDistance} hexes.";
}
    private static string GetTracerCoreDescription(UnitAI unit, float ad, int star)
    {
        var ability = unit.GetComponent<TracerCoreAbility>();
        if (ability == null) return "TracerCore ability missing.";

        float damage = ability.damagePerStar[Mathf.Clamp(star - 1, 0, ability.damagePerStar.Length - 1)];

        return $"Aim at the 2 farthest enemies, shooting a devastating bolt to each target dealing {damage:F0} <color=#00BFFF>magic damage</color> to the first enemy it hits.";
    }

    private static string GetCoreweaverDescription(UnitAI unit, float ad, int star)
{
    var ability = unit.GetComponent<CoreweaverAbility>();
    if (ability == null) return "Coreweaver ability missing.";

    int starIndex = Mathf.Clamp(star - 1, 0, 2);

    float duration = ability.stormDuration[starIndex];
    float meteorDmg = ability.meteorDamage[starIndex];
    float tornadoDmg = ability.tornadoDamage[starIndex];
    
    float baseMana = ability.baseManaPerSecond.Length > starIndex ? ability.baseManaPerSecond[starIndex] : 5f;
    float manaPerSec = baseMana + (unit.attackSpeed * ability.attackSpeedToManaConversion);

    float meteorWithAP = meteorDmg + (unit.abilityPower * 1.5f);
    float tornadoWithAP = tornadoDmg + (unit.abilityPower * 1.5f);

    return
           $"</b>Unleash a storm of meteors and lightning in a large area around the current target for <color=#FFD700>{duration}</color> seconds.\n" +
           $"• <color=#FF4500>Meteors</color> deal <color=#FF4500>{meteorWithAP:F0}</color> <color=#00BFFF>magic damage</color> ({meteorDmg} + 150% AP)\n" +
           $"• <color=#00CED1>Lightning</color> deals <color=#00CED1>{tornadoWithAP:F0}</color> <color=#00BFFF>magic damage</color> ({tornadoDmg} + 150% AP) and stuns for 1 second.";
}

private static string GetManaDriveDescription(UnitAI unit, float ad, int star)
{
    var ability = unit.GetComponent<ManaDriveAbility>();
    if (ability == null) return "ManaDrive ability missing.";

    float dmg = ability.damagePerStar[Mathf.Clamp(star - 1, 0, ability.damagePerStar.Length - 1)];
    float totalDamage = dmg + (unit.abilityPower * 1.0f);

    return $"Hurl a massive bomb towards the largest group of enemies, dealing {totalDamage:F0} <color=#00BFFF>magic damage</color> ({dmg} + 100% AP).\n\n" +
           $"If the bomb kills a target, ManaDrive gains {ability.attackSpeedGain * 100:F0}% attack speed for the rest of combat and casts again, at {(1f - ability.recursiveDamageReduction) * 100:F0}% effectiveness.";
}

private static string GetHyperShotDescription(UnitAI unit, float ad, int star)
{
    var ability = unit.GetComponent<HyperShotAbility>();
    if (ability == null) return "HyperShot ability missing.";

    int starIndex = Mathf.Clamp(star - 1, 0, ability.aoeDamagePerStar.Length - 1);
    float aoeDamage = ability.aoeDamagePerStar[starIndex];
    float totalDamage = aoeDamage + (unit.abilityPower * 1.0f);

    return $"Passive: Every {ability.attacksToTrigger} attacks, trigger an explosion dealing {totalDamage:F0} <color=#00BFFF>magic damage</color> ({aoeDamage} + 100% AP) to all enemies in a {ability.aoeRadius} hex radius.\n\n" +
           $"Active: Gain +{ability.attackSpeedBonus * 100:F0}% attack speed for {ability.buffDuration} seconds. \n\nThis effect stacks!";
}
    private static string GetSteelGuardDescription(UnitAI unit, float ad, int star)
    {
        var ability = unit.GetComponent<SteelGuardAbility>();
        if (ability == null) return "SteelGuard ability missing.";

        int starIndex = Mathf.Clamp(star - 1, 0, ability.shieldValue.Length - 1);
        float shieldAmount = ability.shieldValue[starIndex];
        float allyShieldAmount = shieldAmount * 0.5f;

        return $"Scream, gaining a <color=#FFD700>{shieldAmount}</color> shield for {ability.shieldDuration} seconds and granting a <color=#FFD700>{allyShieldAmount}</color> shield to the nearest ally.\n\n";
    }
    private static string GetBackSlashDescription(UnitAI unit, float ad, int star)
    {
        var ability = unit.GetComponent<BackSlashAbility>();
        if (ability == null) return "BackSlash ability missing.";

        int starIndex = Mathf.Clamp(star - 1, 0, ability.slashDamage.Length - 1);
        float slashDmg = ability.slashDamage[starIndex];
        float slamDmg = ability.slamDamage[starIndex];

        return $"Slash 1 hex in front of BackSlash, dealing {slashDmg:F0} <color=#00BFFF>magic damage</color>.\n\n" +
               $"Every 3rd cast, slam down dealing {slamDmg:F0} <color=#00BFFF>magic damage</color> in a {ability.slamRadius / 3f:F0} hex radius.";
    }

    private static string GetKillSwitchDescription(UnitAI unit, float ad, int star)
{
    var ability = unit.GetComponent<KillSwitchAbility>();
    if (ability == null) return "KillSwitch ability missing.";

    float slamDmg = ability.slamDamagePerStar[Mathf.Clamp(star - 1, 0, ability.slamDamagePerStar.Length - 1)];

    return $"Passive: Every other attack lowers the target's armour by {ability.armorShred}.\n\n" +
           $"Active: Leap to the farthest enemy within 4 hexes, heal {ability.healOnTargetSwap} hp and slam them for {slamDmg + (ad * 0.4f)} <color=#FF6600>physical damage</color>. Grant KillSwitch 50% attack speed for 4 seconds.";
}

private static string GetHaymakerDescription(UnitAI unit, float ad, int star)
{
    var ability = unit.GetComponent<HaymakerAbility>();
    if (ability == null) return "Haymaker ability missing.";

    float slashDmg = ability.slashDamage[Mathf.Clamp(star - 1, 0, ability.slashDamage.Length - 1)];
    float slamDmg = ability.slamDamage[Mathf.Clamp(star - 1, 0, ability.slamDamage.Length - 1)];

    int starIndex = Mathf.Clamp(star - 1, 0, ability.temporaryArmor.Length - 1);
    int damageReductionPercent = 80 + (starIndex * 10);

    return 
           $"Dash to the center clump of enemies and unleash a fury of slashes within 3 hexes that each do {slashDmg + (ad * 1.2f)} <color=#FF6600>physical damage</color> for 3 seconds. While slashing take {damageReductionPercent}% reduced damage. Then dash back to original position.\n\n" +
           $"Then, the clone will slam onto the final target, dealing {slamDmg + (ad * 1.2f)} <color=#FF6600>physical damage</color>. ";
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
           $"Then bonk the target, dealing 20% of B.O.P.'s max health + Attack Damage as <color=#FF6600>physical damage</color> ({bonkDmg:F0} damage).";
}

}
