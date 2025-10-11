// Updated /Assets/Scripts/System/AugmentManager.cs
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AugmentManager : MonoBehaviour
{
    public static AugmentManager Instance;

    [Header("Augment Settings")]
    [SerializeField] private List<BaseAugment> activeAugments = new List<BaseAugment>();
    [SerializeField] private int maxAugments = 3;

    [Header("Stage Timing")]
    [SerializeField] private int[] augmentStages = { 2, 4, 6 };

    [Header("Audio")]
    [SerializeField] private AudioClip augmentSelectSound;
    [SerializeField] private AudioSource audioSource;

    // Cache for augment configurations
    private AugmentConfiguration[] allConfigurations;

    // UI elements to hide during augment selection
    private GameObject shopUI;
    private GameObject traitUI;
    private bool uiWasVisible = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAugments();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAugments()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        // Find all AugmentConfiguration components in the scene
        allConfigurations = FindObjectsOfType<AugmentConfiguration>();

        Debug.Log($"🎯 AugmentManager initialized with {allConfigurations.Length} configurations found");
    }

    public bool ShouldOfferAugment(int stage)
    {
        return activeAugments.Count < maxAugments && augmentStages.Contains(stage);
    }

    public void ShowStaticAugmentSelection(int currentStage)
    {
        Debug.Log($"🎯 Showing static augment selection for stage {currentStage}");

        // Hide other UI elements first
        HideOtherUI();

        GameObject augmentCanvas = GameObject.Find("AugmentCanvas");
        if (augmentCanvas != null)
        {
            Transform panel = augmentCanvas.transform.Find("AugmentSelectionPanel");
            if (panel != null)
            {
                Transform title = panel.Find("Title");
                if (title != null)
                {
                    TMPro.TMP_Text titleText = title.GetComponent<TMPro.TMP_Text>();
                    if (titleText != null)
                    {
                        titleText.text = $"Choose an Augment - Stage {currentStage} ({activeAugments.Count + 1}/3)";
                    }
                }

                panel.gameObject.SetActive(true);
                // NO TIME.TIMESCALE = 0 - Game continues running!
            }
        }
    }

    public void HideAugmentSelection()
    {
        GameObject augmentCanvas = GameObject.Find("AugmentCanvas");
        if (augmentCanvas != null)
        {
            Transform panel = augmentCanvas.transform.Find("AugmentSelectionPanel");
            if (panel != null)
            {
                panel.gameObject.SetActive(false);
            }
        }

        // Restore other UI elements
        ShowOtherUI();
    }

    private void HideOtherUI()
    {
        // Find and hide shop UI
        if (shopUI == null)
        {
            // Try multiple possible names/paths for shop UI
            shopUI = GameObject.Find("ShopUI") ??
                     GameObject.Find("Shop") ??
                     GameObject.Find("ShopCanvas") ??
                     GameObject.Find("ShopPanel");
        }

        // Find and hide trait UI  
        if (traitUI == null)
        {
            // Try multiple possible names/paths for trait UI
            traitUI = GameObject.Find("TraitUI") ??
                      GameObject.Find("TraitsPanel") ??
                      GameObject.Find("TraitCanvas") ??
                      GameObject.Find("ActiveTraitsPanel");
        }

        // Hide shop UI if found
        if (shopUI != null && shopUI.activeInHierarchy)
        {
            shopUI.SetActive(false);
            Debug.Log("🔽 Hidden Shop UI for augment selection");
        }

        // Hide trait UI if found
        if (traitUI != null && traitUI.activeInHierarchy)
        {
            traitUI.SetActive(false);
            Debug.Log("🔽 Hidden Trait UI for augment selection");
        }

        // Alternative: Use managers if available
        HideUIViaManagers();
    }

    private void ShowOtherUI()
    {
        // Show shop UI if it was hidden
        if (shopUI != null)
        {
            shopUI.SetActive(true);
            Debug.Log("🔼 Restored Shop UI after augment selection");
        }

        // Show trait UI if it was hidden
        if (traitUI != null)
        {
            traitUI.SetActive(true);
            Debug.Log("🔼 Restored Trait UI after augment selection");
        }

        // Alternative: Use managers if available
        ShowUIViaManagers();
    }

    private void HideUIViaManagers()
    {
        // Try to hide via ShopUIManager if it exists
        ShopUIManager shopManager = FindObjectOfType<ShopUIManager>();
        if (shopManager != null)
        {
            // Assuming it has a method to hide UI
            // shopManager.HideShopUI(); // Uncomment if this method exists
        }

        // Try to hide via TraitUIManager if it exists
        TraitUIManager traitManager = FindObjectOfType<TraitUIManager>();
        if (traitManager != null)
        {
            // Assuming it has a method to hide UI
            // traitManager.HideTraitUI(); // Uncomment if this method exists
        }
    }

    private void ShowUIViaManagers()
    {
        // Try to show via ShopUIManager if it exists
        ShopUIManager shopManager = FindObjectOfType<ShopUIManager>();
        if (shopManager != null)
        {
            // Assuming it has a method to show UI
            // shopManager.ShowShopUI(); // Uncomment if this method exists
        }

        // Try to show via TraitUIManager if it exists
        TraitUIManager traitManager = FindObjectOfType<TraitUIManager>();
        if (traitManager != null)
        {
            // Assuming it has a method to show UI
            // traitManager.ShowTraitUI(); // Uncomment if this method exists
        }
    }

    public BaseAugment CreateAugmentFromId(string augmentId)
    {
        // Refresh configurations in case they changed
        if (allConfigurations == null || allConfigurations.Length == 0)
        {
            allConfigurations = FindObjectsOfType<AugmentConfiguration>();
        }

        // Find the configuration for this augment ID
        AugmentConfiguration config = System.Array.Find(allConfigurations,
            c => c != null && c.AugmentId == augmentId);

        if (config != null)
        {
            Debug.Log($"🎯 Creating augment from configuration: {augmentId}");
            return config.CreateAugmentInstance();
        }

        Debug.LogWarning($"⚠️ No configuration found for augment ID: {augmentId}");
        return null;
    }

    public void SelectAugment(BaseAugment selectedAugment)
    {
        if (selectedAugment == null) return;

        if (activeAugments.Count >= maxAugments)
        {
            Debug.LogWarning("⚠️ Maximum augments already selected!");
            return;
        }

        if (augmentSelectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(augmentSelectSound);
        }

        activeAugments.Add(selectedAugment);
        selectedAugment.ApplyAugment();

        Debug.Log($"🎯 Augment '{selectedAugment.augmentName}' selected and applied! ({activeAugments.Count}/{maxAugments})");

        // Apply augment to all existing units
        UnitAI[] allUnits = Object.FindObjectsOfType<UnitAI>();
        foreach (var unit in allUnits)
        {
            if (unit.team == Team.Player)
            {
                selectedAugment.OnUnitSpawned(unit);
            }
        }

        // Hide the augment selection immediately (this will also restore other UI)
        HideAugmentSelection();
    }

    public void OnCombatStart()
    {
        foreach (var augment in activeAugments)
        {
            augment.OnCombatStart();
        }
    }

    public void OnCombatEnd()
    {
        foreach (var augment in activeAugments)
        {
            augment.OnCombatEnd();
        }
    }

    public void OnUnitSpawned(UnitAI unit)
    {
        foreach (var augment in activeAugments)
        {
            augment.OnUnitSpawned(unit);
        }
    }

    public bool HasAugment<T>() where T : BaseAugment
    {
        return activeAugments.Any(aug => aug is T);
    }

    public T GetAugment<T>() where T : BaseAugment
    {
        return activeAugments.OfType<T>().FirstOrDefault();
    }

    public List<BaseAugment> GetActiveAugments()
    {
        return new List<BaseAugment>(activeAugments);
    }

    public void ResetAugments()
    {
        foreach (var augment in activeAugments)
        {
            augment.RemoveAugment();
        }
        activeAugments.Clear();
        Debug.Log("🔄 All augments reset");
    }
}
