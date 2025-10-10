// /Assets/Scripts/System/AugmentManager.cs
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AugmentManager : MonoBehaviour
{
    public static AugmentManager Instance;

    [Header("Augment Settings")]
    [SerializeField] private List<BaseAugment> allAugments = new List<BaseAugment>();
    [SerializeField] private List<BaseAugment> activeAugments = new List<BaseAugment>();

    [Header("Stage Timing")]
    [SerializeField] private int[] augmentStages = { 2, 4, 6 }; // Stages when augments are offered
    [SerializeField] private int augmentsPerSelection = 3;

    [Header("Audio")]
    [SerializeField] private AudioClip augmentSelectSound;
    [SerializeField] private AudioSource audioSource;

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
        // Initialize all augment instances
        allAugments = new List<BaseAugment>
        {
            new EradicateTheWeakAugment(),
            new ItsClobberingTimeAugment(),
            new SupportTheRevolutionAugment()
        };

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D audio
        }

        Debug.Log($"🎯 AugmentManager initialized with {allAugments.Count} augments");
    }

    public bool ShouldOfferAugment(int stage)
    {
        return augmentStages.Contains(stage);
    }

    public List<BaseAugment> GetRandomAugmentChoices()
    {
        List<BaseAugment> availableAugments = allAugments.Where(aug =>
            !activeAugments.Any(active => active.GetType() == aug.GetType())).ToList();

        List<BaseAugment> choices = new List<BaseAugment>();

        // Ensure we get up to 3 unique augments
        while (choices.Count < augmentsPerSelection && availableAugments.Count > 0)
        {
            int randomIndex = Random.Range(0, availableAugments.Count);
            choices.Add(availableAugments[randomIndex]);
            availableAugments.RemoveAt(randomIndex);
        }

        Debug.Log($"🎯 Generated {choices.Count} augment choices");
        return choices;
    }

    public void SelectAugment(BaseAugment selectedAugment)
    {
        if (selectedAugment == null) return;

        // Play selection sound
        if (augmentSelectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(augmentSelectSound);
        }

        // Add to active augments
        activeAugments.Add(selectedAugment);

        // Apply the augment immediately
        selectedAugment.ApplyAugment();

        Debug.Log($"🎯 Augment '{selectedAugment.augmentName}' selected and applied!");

        // Notify all systems that an augment was selected
        OnAugmentSelected(selectedAugment);
    }

    private void OnAugmentSelected(BaseAugment augment)
    {
        // Update UI to show active augment
        if (UIManager.Instance != null)
        {
            UIManager.Instance.AddActiveAugment(augment);
        }

        // Apply augment to all existing units
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        foreach (var unit in allUnits)
        {
            if (unit.team == Team.Player)
            {
                augment.OnUnitSpawned(unit);
            }
        }
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
