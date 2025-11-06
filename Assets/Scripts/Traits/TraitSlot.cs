using UnityEngine;

public class TraitSlot : MonoBehaviour
{
    [Header("Slot Info")]
    public int slotIndex;

    [Header("Current Occupant")]
    public TraitUIPanel currentPanel;
    public Trait currentTrait;

    public bool IsOccupied => currentPanel != null;

    public void AssignPanel(TraitUIPanel panel, Trait trait)
    {
        if (currentPanel != null)
        {
            currentPanel.transform.SetParent(null);
            Destroy(currentPanel.gameObject);
        }

        currentPanel = panel;
        currentTrait = trait;

        if (panel != null)
        {
            panel.transform.SetParent(transform, false);
            panel.transform.localPosition = Vector3.zero;
            panel.transform.localScale = Vector3.one;

            RectTransform rect = panel.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }
    }

    public void Clear()
    {
        if (currentPanel != null)
        {
            Destroy(currentPanel.gameObject);
        }
        currentPanel = null;
        currentTrait = Trait.None;
    }
}
