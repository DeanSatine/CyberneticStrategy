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
    [SerializeField] private float bonusAttackSpeed = 0.1f; 
    [SerializeField] private float healPercentage = 0.1f;
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
    [Header("UI Settings")]
    [SerializeField] private Sprite augmentIcon;

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
        augment.icon = augmentIcon;
    }

    public string GetDefaultName()
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
                return "Your strongest Eradicator unit is linked to the Hydraulic Press. They gain 10 attack damage, 10% attack speed and heal 10% of their maximum health whenever the press executes an enemy. Gain a ManaDrive.";
            case ConfigurableAugmentType.ClobberingTime:
                return "Clobbertrons jump to their target on combat start and jump once more at 10% health. Their jumps create a shockwave that does 150% of their AD as damage. Gain a B.O.P. and Haymaker.";
            case ConfigurableAugmentType.SupportRevolution:
                return "Your units start combat with 3 gears which fly to and heal the lowest health ally on attack. Gears heal 50-300 health (based on stage) and refresh every 10 seconds.";
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
    public Sprite GetAugmentIcon() => augmentIcon;

    // Getters for specific augment settings
    public GameObject GetManaDrivePrefab() => manaDrivePrefab;
    public GameObject GetBOPPrefab() => bopPrefab;
    public GameObject GetGearPrefab() => gearPrefab;

    public float GetBonusAttackDamage() => bonusAttackDamage;
    public float GetBonusAttackSpeed() => bonusAttackSpeed;
    public float GetHealPercentage() => healPercentage;

    public float GetAttackDamageBonus() => attackDamageBonus;
    public float GetLowHealthThreshold() => lowHealthThreshold;
    public float GetJumpDamage() => jumpDamage;

    public int GetGearsPerUnit() => gearsPerUnit;
    public float GetGearOrbitRadius() => gearOrbitRadius;
    public float GetGearOrbitSpeed() => gearOrbitSpeed;
    public float GetBaseHealAmount() => baseHealAmount;

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
