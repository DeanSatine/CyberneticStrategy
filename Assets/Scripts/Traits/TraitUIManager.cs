using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TraitUIManager : MonoBehaviour
{
    public static TraitUIManager Instance;

    [Header("UI References")]
    public Canvas canvas;                 // your main canvas
    public GameObject traitRowPrefab;     // assign TraitUIRow prefab
    public Vector2 startPos = new Vector2(50, -50); // where the first trait spawns
    public float rowSpacing = 60f;        // space between rows

    private Dictionary<Trait, GameObject> activeRows = new Dictionary<Trait, GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    public void UpdateTraitUI(Dictionary<Trait, int> traitCounts)
    {
        int index = 0;

        foreach (var kvp in traitCounts)
        {
            Trait trait = kvp.Key;
            int count = kvp.Value;

            // Spawn if not exists
            if (!activeRows.ContainsKey(trait))
            {
                GameObject newRow = Instantiate(traitRowPrefab, canvas.transform);
                activeRows[trait] = newRow;
            }

            GameObject rowObj = activeRows[trait];
            rowObj.SetActive(true);

            // Position manually like TFT HUD
            RectTransform rt = rowObj.GetComponent<RectTransform>();
            rt.anchoredPosition = startPos + new Vector2(0, -index * rowSpacing);

            // Update text
            TextMeshProUGUI text = rowObj.GetComponentInChildren<TextMeshProUGUI>();
            int tier = TraitManager.Instance.GetCurrentTier(trait, count);
            text.text = tier > 0
                ? $"{trait} {count} (Active {tier})"
                : $"{trait} {count}";


            // Highlight if active threshold reached
            bool active = TraitManager.Instance.IsTraitActive(trait, count);
            text.color = active ? Color.yellow : Color.white;

            index++;
        }

        // Hide any extra rows that are no longer in traitCounts
        List<Trait> traitsToHide = new List<Trait>();
        foreach (var kvp in activeRows)
        {
            if (!traitCounts.ContainsKey(kvp.Key))
            {
                kvp.Value.SetActive(false);
                traitsToHide.Add(kvp.Key);
            }
        }
        foreach (var t in traitsToHide)
        {
            activeRows.Remove(t);
        }
    }
}
