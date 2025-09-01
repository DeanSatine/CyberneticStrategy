using System.Collections.Generic;
using UnityEngine;

public class TraitUIManager : MonoBehaviour
{
    public static TraitUIManager Instance;

    [System.Serializable]
    public class TraitPanelRef
    {
        public Trait trait;
        public TraitUIPanel panel;
    }

    [Header("Preplaced Panels")]
    public List<TraitPanelRef> traitPanels;

    private Dictionary<Trait, TraitUIPanel> panelMap = new Dictionary<Trait, TraitUIPanel>();

    private void Awake()
    {
        Instance = this;

        foreach (var refPair in traitPanels)
        {
            if (!panelMap.ContainsKey(refPair.trait))
                panelMap.Add(refPair.trait, refPair.panel);

            if (refPair.panel != null)
                refPair.panel.gameObject.SetActive(false);
        }
    }

    public void UpdateTraitUI(Dictionary<Trait, int> traitCounts)
    {
        foreach (var kvp in panelMap)
        {
            Trait trait = kvp.Key;
            TraitUIPanel panel = kvp.Value;

            int count = traitCounts.ContainsKey(trait) ? traitCounts[trait] : 0;

            if (count == 0)
            {
                panel.gameObject.SetActive(false);
                continue;
            }

            panel.gameObject.SetActive(true);

            int activeTier = TraitManager.Instance.GetCurrentTier(trait, count);
            (int activeThreshold, int nextThreshold) = TraitManager.Instance.GetBreakpoints(trait, count);

            bool isActive = activeTier > 0;

            panel.UpdateTexts(count, activeThreshold, nextThreshold, isActive);
        }
    }
}
