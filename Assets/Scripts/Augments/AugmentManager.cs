// Complete updated /Assets/Scripts/Augments/AugmentManager.cs
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

public class AugmentManager : MonoBehaviour
{
    public static AugmentManager Instance;

    [Header("Augment Settings")]
    [SerializeField] private List<BaseAugment> activeAugments = new List<BaseAugment>();
    [SerializeField] private int maxAugments = 3;

    [Header("Round Timing")]
    [SerializeField] private int[] augmentRounds = { 1, 3, 5 }; // Changed from stages to specific rounds
    [SerializeField] private bool showOnFirstRound = true; // New option to control first round behavior

    [Header("Audio")]
    [SerializeField] private AudioClip augmentSelectSound;
    [SerializeField] private AudioSource audioSource;

    // Cache for augment configurations
    private AugmentConfiguration[] allConfigurations;

    // Track selected augments to prevent duplicates
    private HashSet<string> selectedAugmentIds = new HashSet<string>();

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

    // NEW: Check if we should offer augment based on total round number
    public bool ShouldOfferAugment(int stage, int roundInStage, int roundsPerStage)
    {
        // Calculate total round number across all stages
        int totalRound = ((stage - 1) * roundsPerStage) + roundInStage;

        // Always offer on first round, then check configured rounds
        bool shouldOffer = (showOnFirstRound && totalRound == 1) || augmentRounds.Contains(totalRound);

        // Only offer if we haven't reached max augments and there are unselected options
        return shouldOffer &&
               activeAugments.Count < maxAugments &&
               GetAvailableAugmentIds().Count > 0;
    }

    // NEW: Get list of available (unselected) augment IDs
    private List<string> GetAvailableAugmentIds()
    {
        if (allConfigurations == null || allConfigurations.Length == 0)
        {
            allConfigurations = FindObjectsOfType<AugmentConfiguration>();
        }

        List<string> availableIds = new List<string>();

        foreach (var config in allConfigurations)
        {
            if (config != null && !string.IsNullOrEmpty(config.AugmentId))
            {
                // Only include if not already selected
                if (!selectedAugmentIds.Contains(config.AugmentId))
                {
                    availableIds.Add(config.AugmentId);
                }
            }
        }

        return availableIds;
    }

    public void ShowStaticAugmentSelection(int currentStage, int currentRound)
    {
        int totalRound = ((currentStage - 1) * 3) + currentRound; // Assuming 3 rounds per stage
        Debug.Log($"🎯 Showing static augment selection for Stage {currentStage}, Round {currentRound} (Total Round {totalRound})");

        // Hide unavailable augments before showing
        HideSelectedAugmentButtons();

        GameObject augmentCanvas = GameObject.Find("AugmentCanvas");
        if (augmentCanvas != null)
        {
            Transform panel = augmentCanvas.transform.Find("AugmentSelectionPanel");
            if (panel != null)
            {
                // Hide other UI first
                HideOtherUI();

                // Update title
                Transform title = panel.Find("Title");
                if (title != null)
                {
                    TMPro.TMP_Text titleText = title.GetComponent<TMPro.TMP_Text>();
                    if (titleText != null)
                    {
                        titleText.text = $"Choose an Augment - Round {totalRound} ({activeAugments.Count + 1}/{maxAugments})";
                    }
                }

                // Get all available augment buttons and trigger their fade-in
                StaticAugmentButton[] allButtons = panel.GetComponentsInChildren<StaticAugmentButton>(true);
                List<StaticAugmentButton> availableButtons = new List<StaticAugmentButton>();

                foreach (var button in allButtons)
                {
                    if (button != null && !selectedAugmentIds.Contains(button.augmentId))
                    {
                        button.gameObject.SetActive(true);
                        availableButtons.Add(button);
                    }
                }

                panel.gameObject.SetActive(true);

                // Stagger the button fade-ins for available buttons only
                StartCoroutine(StaggeredButtonFadeIn(availableButtons.ToArray()));

                Debug.Log($"📋 Showing {availableButtons.Count} available augments (Total: {allButtons.Length}, Selected: {selectedAugmentIds.Count})");
            }
        }
    }

    // NEW: Hide buttons for already selected augments
    private void HideSelectedAugmentButtons()
    {
        GameObject augmentCanvas = GameObject.Find("AugmentCanvas");
        if (augmentCanvas != null)
        {
            Transform panel = augmentCanvas.transform.Find("AugmentSelectionPanel");
            if (panel != null)
            {
                StaticAugmentButton[] allButtons = panel.GetComponentsInChildren<StaticAugmentButton>(true);

                foreach (var button in allButtons)
                {
                    if (button != null && selectedAugmentIds.Contains(button.augmentId))
                    {
                        button.gameObject.SetActive(false);
                        Debug.Log($"🚫 Hiding already selected augment: {button.augmentId}");
                    }
                }
            }
        }
    }

    private IEnumerator StaggeredButtonFadeIn(StaticAugmentButton[] buttons)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].FadeIn();
                yield return new WaitForSecondsRealtime(0.1f);
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
                StaticAugmentButton[] buttons = panel.GetComponentsInChildren<StaticAugmentButton>();
                StartCoroutine(FadeOutAndHide(buttons, panel.gameObject));
            }
        }
    }

    private IEnumerator FadeOutAndHide(StaticAugmentButton[] buttons, GameObject panel)
    {
        foreach (var button in buttons)
        {
            if (button != null)
            {
                button.FadeOut();
            }
        }

        yield return new WaitForSecondsRealtime(0.3f);

        panel.SetActive(false);
        ShowOtherUI();
    }

    private void HideOtherUI()
    {
        if (shopUI == null)
        {
            shopUI = GameObject.Find("ShopUI") ??
                     GameObject.Find("Shop") ??
                     GameObject.Find("ShopCanvas") ??
                     GameObject.Find("ShopPanel");
        }

        if (traitUI == null)
        {
            traitUI = GameObject.Find("TraitUI") ??
                      GameObject.Find("TraitsPanel") ??
                      GameObject.Find("TraitCanvas") ??
                      GameObject.Find("ActiveTraitsPanel");
        }

        if (shopUI != null && shopUI.activeInHierarchy)
        {
            shopUI.SetActive(false);
            Debug.Log("🔽 Hidden Shop UI for augment selection");
        }

        if (traitUI != null && traitUI.activeInHierarchy)
        {
            traitUI.SetActive(false);
            Debug.Log("🔽 Hidden Trait UI for augment selection");
        }

        HideUIViaManagers();
    }

    private void ShowOtherUI()
    {
        if (shopUI != null)
        {
            shopUI.SetActive(true);
            Debug.Log("🔼 Restored Shop UI after augment selection");
        }

        if (traitUI != null)
        {
            traitUI.SetActive(true);
            Debug.Log("🔼 Restored Trait UI after augment selection");
        }

        ShowUIViaManagers();
    }

    private void HideUIViaManagers()
    {
        ShopUIManager shopManager = FindObjectOfType<ShopUIManager>();
        TraitUIManager traitManager = FindObjectOfType<TraitUIManager>();
    }

    private void ShowUIViaManagers()
    {
        ShopUIManager shopManager = FindObjectOfType<ShopUIManager>();
        TraitUIManager traitManager = FindObjectOfType<TraitUIManager>();
    }

    public BaseAugment CreateAugmentFromId(string augmentId)
    {
        if (allConfigurations == null || allConfigurations.Length == 0)
        {
            allConfigurations = FindObjectsOfType<AugmentConfiguration>();
        }

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

        // Find the augment ID to track it as selected
        string augmentId = GetAugmentIdFromInstance(selectedAugment);
        if (!string.IsNullOrEmpty(augmentId))
        {
            selectedAugmentIds.Add(augmentId);
            Debug.Log($"🚫 Marked augment as selected: {augmentId}");
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

        HideAugmentSelection();
    }

    // NEW: Get augment ID from instance (reverse lookup)
    private string GetAugmentIdFromInstance(BaseAugment augment)
    {
        if (allConfigurations == null) return null;

        foreach (var config in allConfigurations)
        {
            if (config != null)
            {
                var testAugment = config.CreateAugmentInstance();
                if (testAugment != null && testAugment.GetType() == augment.GetType())
                {
                    return config.AugmentId;
                }
            }
        }

        return null;
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
        selectedAugmentIds.Clear(); // Clear selected tracking
        Debug.Log("🔄 All augments reset");
    }

    // NEW: Debug method to see current state
    [ContextMenu("Debug Augment State")]
    public void DebugAugmentState()
    {
        Debug.Log($"🎯 Active Augments: {activeAugments.Count}/{maxAugments}");
        Debug.Log($"🚫 Selected IDs: {string.Join(", ", selectedAugmentIds)}");
        Debug.Log($"✅ Available IDs: {string.Join(", ", GetAvailableAugmentIds())}");
    }
}
