using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TraitUIManager : MonoBehaviour
{
    public static TraitUIManager Instance;

    [System.Serializable]
    public class TraitPanelPrefab
    {
        public Trait trait;
        public GameObject panelPrefab;
    }

    [Header("Slot System")]
    public Transform slotsContainer;
    public List<TraitSlot> traitSlots;

    [Header("Panel Prefabs")]
    public List<TraitPanelPrefab> traitPanelPrefabs;

    [Header("Ordering")]
    public List<Trait> activeTraitOrder = new List<Trait>();

    private Dictionary<Trait, GameObject> prefabMap = new Dictionary<Trait, GameObject>();
    private Dictionary<Trait, int> lastUpdateCounts = new Dictionary<Trait, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple TraitUIManager instances detected!");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Debug.Log($"[TraitUIManager] Initializing with {traitPanelPrefabs.Count} prefabs and {traitSlots.Count} slots");

        foreach (var prefabRef in traitPanelPrefabs)
        {
            if (prefabRef.panelPrefab == null)
            {
                Debug.LogError($"[TraitUIManager] Prefab for trait {prefabRef.trait} is null!");
                continue;
            }

            if (!prefabMap.ContainsKey(prefabRef.trait))
            {
                prefabMap.Add(prefabRef.trait, prefabRef.panelPrefab);
                Debug.Log($"[TraitUIManager] Registered prefab for {prefabRef.trait}");
            }
        }

        if (traitSlots.Count == 0 && slotsContainer != null)
        {
            traitSlots = new List<TraitSlot>(slotsContainer.GetComponentsInChildren<TraitSlot>());
            Debug.Log($"[TraitUIManager] Auto-detected {traitSlots.Count} slots from container");
        }

        for (int i = 0; i < traitSlots.Count; i++)
        {
            if (traitSlots[i] != null)
            {
                traitSlots[i].slotIndex = i;
            }
        }
    }

    public void UpdateTraitUI(Dictionary<Trait, int> traitCounts)
    {
        Debug.Log($"[TraitUIManager] UpdateTraitUI called with {traitCounts.Count} traits");

        foreach (var kvp in traitCounts)
        {
            Debug.Log($"  - {kvp.Key}: {kvp.Value} units");
        }

        UpdateTraitOrder(traitCounts);
        ApplyTraitsToSlots(traitCounts);
    }

    private void UpdateTraitOrder(Dictionary<Trait, int> traitCounts)
    {
        List<Trait> newActiveTraits = traitCounts.Keys.Where(t => traitCounts[t] > 0 && t != Trait.None).ToList();

        foreach (var trait in newActiveTraits)
        {
            if (activeTraitOrder.Contains(trait))
            {
                activeTraitOrder.Remove(trait);
            }
            activeTraitOrder.Insert(0, trait);
        }

        activeTraitOrder.RemoveAll(t => !newActiveTraits.Contains(t));

        Debug.Log($"[TraitUIManager] Active trait order: {string.Join(", ", activeTraitOrder)}");
    }

    private void ApplyTraitsToSlots(Dictionary<Trait, int> traitCounts)
    {
        ClearEmptySlots(traitCounts);

        int slotIndex = 0;

        foreach (var trait in activeTraitOrder)
        {
            if (slotIndex >= traitSlots.Count)
            {
                Debug.LogWarning($"[TraitUIManager] Not enough trait slots! Need {activeTraitOrder.Count}, have {traitSlots.Count}");
                break;
            }

            int count = traitCounts.ContainsKey(trait) ? traitCounts[trait] : 0;
            if (count == 0) continue;

            TraitSlot slot = traitSlots[slotIndex];

            if (slot.currentTrait != trait)
            {
                Debug.Log($"[TraitUIManager] Creating panel for {trait} in slot {slotIndex}");
                CreatePanelInSlot(slot, trait);
            }

            if (slot.currentPanel != null)
            {
                int activeTier = TraitManager.Instance.GetCurrentTier(trait, count);
                (int activeThreshold, int nextThreshold) = TraitManager.Instance.GetBreakpoints(trait, count);
                bool isActive = activeTier > 0;

                slot.currentPanel.UpdateTexts(count, activeThreshold, nextThreshold, isActive);
                Debug.Log($"[TraitUIManager] Updated {trait} panel: {count}/{activeThreshold}/{nextThreshold}, active={isActive}");
            }

            slotIndex++;
        }

        for (int i = slotIndex; i < traitSlots.Count; i++)
        {
            traitSlots[i].Clear();
        }

        lastUpdateCounts = new Dictionary<Trait, int>(traitCounts);
    }

    private void ClearEmptySlots(Dictionary<Trait, int> traitCounts)
    {
        foreach (var slot in traitSlots)
        {
            if (slot.currentTrait != Trait.None)
            {
                int count = traitCounts.ContainsKey(slot.currentTrait) ? traitCounts[slot.currentTrait] : 0;
                if (count == 0)
                {
                    slot.Clear();
                }
            }
        }
    }

    private void CreatePanelInSlot(TraitSlot slot, Trait trait)
    {
        if (!prefabMap.ContainsKey(trait))
        {
            Debug.LogError($"[TraitUIManager] No prefab found for trait: {trait}");
            return;
        }

        GameObject prefab = prefabMap[trait];
        if (prefab == null)
        {
            Debug.LogError($"[TraitUIManager] Prefab for {trait} is null in map!");
            return;
        }

        GameObject panelObj = Instantiate(prefab);
        TraitUIPanel panel = panelObj.GetComponent<TraitUIPanel>();

        if (panel == null)
        {
            Debug.LogError($"[TraitUIManager] Prefab for {trait} does not have TraitUIPanel component!");
            Destroy(panelObj);
            return;
        }

        slot.AssignPanel(panel, trait);
        Debug.Log($"[TraitUIManager] Successfully created and assigned {trait} panel to slot {slot.slotIndex}");
    }
}
