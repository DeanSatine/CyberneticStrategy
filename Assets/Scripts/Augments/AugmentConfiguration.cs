// Updated /Assets/Scripts/Augments/AugmentConfiguration.cs
using UnityEngine;

public enum ConfigurableAugmentType
{
    EradicateTheWeak,
    ClobberingTime,
    SupportRevolution
}

[System.Serializable]
public class AugmentConfiguration : MonoBehaviour
{
    [Header("Augment Selection")]
    [SerializeField] private ConfigurableAugmentType selectedAugment = ConfigurableAugmentType.EradicateTheWeak;

    [Header("Eradicate the Weak Settings")]
    [SerializeField] private GameObject manaDrivePrefab;
    [SerializeField] private float bonusAttackDamage = 10f;
    [SerializeField] private float bonusAttackSpeed = 0.1f; // 10%
    [SerializeField] private float healPercentage = 0.1f; // 10%
    [SerializeField] private GameObject linkVFXPrefab;
    [SerializeField] private GameObject healVFXPrefab;
    [SerializeField] private GameObject linkedUnitVFXPrefab;

    [Header("Clobbering Time Settings")]
    [SerializeField] private GameObject bopPrefab;
    [SerializeField] private float attackDamageBonus = 20f;
    [SerializeField] private float lowHealthThreshold = 0.1f;
    [SerializeField] private float jumpDamage = 25f;
    [SerializeField] private GameObject jumpVFXPrefab;
    [SerializeField] private GameObject landingVFXPrefab;

    [Header("Support Revolution Settings")]
    [SerializeField] private GameObject gearPrefab;
    [SerializeField] private int gearsPerUnit = 3;
    [SerializeField] private float gearOrbitRadius = 2f;
    [SerializeField] private float gearOrbitSpeed = 90f;
    [SerializeField] private float baseHealAmount = 15f;
    [SerializeField] private GameObject gearHealVFXPrefab;

    public string AugmentId
    {
        get
        {
            switch (selectedAugment)
            {
                case ConfigurableAugmentType.EradicateTheWeak: return "EradicateTheWeak";
                case ConfigurableAugmentType.ClobberingTime: return "ClobberingTime";
                case ConfigurableAugmentType.SupportRevolution: return "SupportRevolution";
                default: return "Unknown";
            }
        }
    }

    public BaseAugment CreateAugmentInstance()
    {
        BaseAugment augment = null;

        switch (selectedAugment)
        {
            case ConfigurableAugmentType.EradicateTheWeak:
                augment = new EradicateTheWeakAugment();

                break;
            case ConfigurableAugmentType.ClobberingTime:
                augment = new ItsClobberingTimeAugment();
                break;
            case ConfigurableAugmentType.SupportRevolution:
                augment = new SupportTheRevolutionAugment();
                break;
        }

        if (augment != null)
        {
            ConfigureBaseProperties(augment);
        }

        return augment;
    }
    public GameObject GetLinkVFXPrefab() => linkVFXPrefab;
    public GameObject GetHealVFXPrefab() => healVFXPrefab;
    public GameObject GetLinkedUnitVFXPrefab() => linkedUnitVFXPrefab;

    public GameObject GetJumpVFXPrefab() => jumpVFXPrefab;
    public GameObject GetLandingVFXPrefab() => landingVFXPrefab;

    public GameObject GetGearHealVFXPrefab() => gearHealVFXPrefab;

    private void ConfigureBaseProperties(BaseAugment augment)
    {
        augment.augmentName = GetDefaultName();
        augment.description = GetDefaultDescription();
        augment.type = GetAugmentType();
        augment.augmentColor = GetDefaultColor();
    }

    private string GetDefaultName()
    {
        switch (selectedAugment)
        {
            case ConfigurableAugmentType.EradicateTheWeak: return "Eradicate the Weak";
            case ConfigurableAugmentType.ClobberingTime: return "It's Clobbering Time!";
            case ConfigurableAugmentType.SupportRevolution: return "Support the Revolution";
            default: return "Unknown Augment";
        }
    }

    private string GetDefaultDescription()
    {
        switch (selectedAugment)
        {
            case ConfigurableAugmentType.EradicateTheWeak:
                return "Origin augment: Links strongest Eradicator to Hydraulic Press. Executions buff and heal the linked unit. Adds ManaDrive to bench.";
            case ConfigurableAugmentType.ClobberingTime:
                return "Class augment: Clobbertrons gain jump ability and increased attack damage. Adds B.O.P. to bench.";
            case ConfigurableAugmentType.SupportRevolution:
                return "Generic augment: All units gain orbiting gears that fly to heal allies when attacking.";
            default:
                return "No description available.";
        }
    }

    private AugmentType GetAugmentType()
    {
        switch (selectedAugment)
        {
            case ConfigurableAugmentType.EradicateTheWeak: return AugmentType.Origin;
            case ConfigurableAugmentType.ClobberingTime: return AugmentType.Class;
            case ConfigurableAugmentType.SupportRevolution: return AugmentType.Generic;
            default: return AugmentType.Generic;
        }
    }

    private Color GetDefaultColor()
    {
        switch (selectedAugment)
        {
            case ConfigurableAugmentType.EradicateTheWeak: return Color.red;
            case ConfigurableAugmentType.ClobberingTime: return Color.blue;
            case ConfigurableAugmentType.SupportRevolution: return Color.green;
            default: return Color.white;
        }
    }

    // Getters for specific augment settings
    public GameObject GetManaDrivePrefab() => manaDrivePrefab;
    public GameObject GetBOPPrefab() => bopPrefab;
    public GameObject GetGearPrefab() => gearPrefab;

    // Eradicate the Weak getters
    public float GetBonusAttackDamage() => bonusAttackDamage;
    public float GetBonusAttackSpeed() => bonusAttackSpeed;
    public float GetHealPercentage() => healPercentage;

    // Clobbering Time getters
    public float GetAttackDamageBonus() => attackDamageBonus;
    public float GetLowHealthThreshold() => lowHealthThreshold;
    public float GetJumpDamage() => jumpDamage;

    // Support Revolution getters
    public int GetGearsPerUnit() => gearsPerUnit;
    public float GetGearOrbitRadius() => gearOrbitRadius;
    public float GetGearOrbitSpeed() => gearOrbitSpeed;
    public float GetBaseHealAmount() => baseHealAmount;

    // Debug info
    [Header("Debug Info (Read Only)")]
    [SerializeField] private string debugAugmentId;
    [SerializeField] private string debugAugmentName;
    [SerializeField] private string debugAugmentType;

    private void OnValidate()
    {
        debugAugmentId = AugmentId;
        debugAugmentName = GetDefaultName();
        debugAugmentType = GetAugmentType().ToString();
    }
}
