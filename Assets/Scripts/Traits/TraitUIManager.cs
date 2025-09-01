using System.Collections.Generic;
using UnityEngine;

public class TraitUIManager : MonoBehaviour
{
    public static TraitUIManager Instance;

    [System.Serializable]
    public class TraitPrefabRef
    {
        public Trait trait;
        public GameObject prefab; // prefab of your designed UI element
    }

    [Header("Trait UI Settings")]
    public Transform traitUIParent; // The parent container (a Canvas or empty under it)
    public List<TraitPrefabRef> traitPrefabs;

    private Dictionary<Trait, TraitUIPanel> activePanels = new Dictionary<Trait, TraitUIPanel>();

    private void Awake()
    {
        Instance = this;
    }

    public void UpdateTraitUI(Dictionary<Trait, int> traitCounts)
    {
        foreach (var kvp in traitCounts)
        {
            Trait trait = kvp.Key;
            int count = kvp.Value;

            if (count == 0)
            {
                // remove if it exists
                if (activePanels.ContainsKey(trait))
                {
                    Destroy(activePanels[trait].gameObject);
                    activePanels.Remove(trait);
                }
                continue;
            }

            // spawn if missing
            if (!activePanels.ContainsKey(trait))
            {
                var prefabRef = traitPrefabs.Find(x => x.trait == trait);
                if (prefabRef != null && prefabRef.prefab != null)
                {
                    GameObject uiObj = Instantiate(prefabRef.prefab, traitUIParent);
                    TraitUIPanel panel = uiObj.GetComponent<TraitUIPanel>();
                    activePanels.Add(trait, panel);
                }
            }

            // update texts
            if (activePanels.TryGetValue(trait, out TraitUIPanel panelRef))
            {
                int activeTier = TraitManager.Instance.GetCurrentTier(trait, count);
                (int activeThreshold, int nextThreshold) = TraitManager.Instance.GetBreakpoints(trait, count);

                panelRef.UpdateTexts(count, activeThreshold, nextThreshold);
            }
        }
    }
}
