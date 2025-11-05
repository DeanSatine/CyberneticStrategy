using UnityEngine;

public enum DamageType
{
    Physical,
    Magic,
    True
}

public struct DamageInfo
{
    public float amount;
    public DamageType type;
    public UnitAI source;

    public DamageInfo(float amount, DamageType type, UnitAI source = null)
    {
        this.amount = amount;
        this.type = type;
        this.source = source;
    }

    public static DamageInfo Physical(float amount, UnitAI source = null)
    {
        return new DamageInfo(amount, DamageType.Physical, source);
    }

    public static DamageInfo Magic(float amount, UnitAI source = null)
    {
        return new DamageInfo(amount, DamageType.Magic, source);
    }

    public static DamageInfo True(float amount, UnitAI source = null)
    {
        return new DamageInfo(amount, DamageType.True, source);
    }
}

public static class DamageCalculator
{
    public static float CalculateDamageReduction(DamageInfo damageInfo, float armor, float magicResist, float damageReduction)
    {
        float damage = damageInfo.amount;

        if (damageInfo.type == DamageType.True)
        {
            return damage;
        }

        float resistanceReduction = 1f;

        if (damageInfo.type == DamageType.Physical)
        {
            resistanceReduction = 100f / (100f + armor);
        }
        else if (damageInfo.type == DamageType.Magic)
        {
            resistanceReduction = 100f / (100f + magicResist);
        }

        damage *= resistanceReduction;

        float damageReductionMultiplier = 1f - (damageReduction / 100f);
        damage *= damageReductionMultiplier;

        return damage;
    }

    public static float GetPhysicalDamageReduction(float armor, float damageReduction)
    {
        float armorReduction = (armor / (100f + armor)) * 100f;
        float totalReduction = 1f - ((1f - armorReduction / 100f) * (1f - damageReduction / 100f));
        return totalReduction * 100f;
    }

    public static float GetMagicDamageReduction(float magicResist, float damageReduction)
    {
        float mrReduction = (magicResist / (100f + magicResist)) * 100f;
        float totalReduction = 1f - ((1f - mrReduction / 100f) * (1f - damageReduction / 100f));
        return totalReduction * 100f;
    }

    public static string GetDamageTypeIcon(DamageType type)
    {
        switch (type)
        {
            case DamageType.Physical: return "⚔️";
            case DamageType.Magic: return "✨";
            case DamageType.True: return "💥";
            default: return "💢";
        }
    }

    public static Color GetDamageTypeColor(DamageType type)
    {
        switch (type)
        {
            case DamageType.Physical: return new Color(1f, 0.5f, 0f);
            case DamageType.Magic: return new Color(0.4f, 0.6f, 1f);
            case DamageType.True: return Color.white;
            default: return Color.gray;
        }
    }
}

public static class DamageExtensions
{
    public static void DealPhysicalDamage(this UnitAI source, UnitAI target, float amount)
    {
        if (target == null || !target.isAlive) return;
        target.TakeDamage(DamageInfo.Physical(amount, source));
    }

    public static void DealMagicDamage(this UnitAI source, UnitAI target, float amount)
    {
        if (target == null || !target.isAlive) return;
        target.TakeDamage(DamageInfo.Magic(amount, source));
    }

    public static void DealTrueDamage(this UnitAI source, UnitAI target, float amount)
    {
        if (target == null || !target.isAlive) return;
        target.TakeDamage(DamageInfo.True(amount, source));
    }

    public static void DealDamage(this UnitAI source, UnitAI target, float amount, DamageType type)
    {
        if (target == null || !target.isAlive) return;
        target.TakeDamage(new DamageInfo(amount, type, source));
    }
    public static void DealMagicDamageWithAP(this UnitAI source, UnitAI target, float baseDamage, float apRatio)
    {
        if (target == null || !target.isAlive) return;
        float totalDamage = baseDamage + (source.abilityPower * apRatio);
        target.TakeDamage(DamageInfo.Magic(totalDamage, source));
    }

    public static void DealPhysicalDamageWithAP(this UnitAI source, UnitAI target, float baseDamage, float apRatio)
    {
        if (target == null || !target.isAlive) return;
        float totalDamage = baseDamage + (source.abilityPower * apRatio);
        target.TakeDamage(DamageInfo.Physical(totalDamage, source));
    }

}
