using System.Collections.Generic;
using UnityEngine;

public class UnitInfoPanelManager : MonoBehaviour
{
    public static UnitInfoPanelManager Instance;

    [System.Serializable]
    public class PanelEntry
    {
        public string unitName;
        public UnitInfoPanelUI panel;
    }

    [Header("Unit Panels")]
    public List<PanelEntry> unitPanels = new List<PanelEntry>();

    private Dictionary<string, UnitInfoPanelUI> panelMap = new Dictionary<string, UnitInfoPanelUI>();
    private UnitInfoPanelUI activePanel;

    private void Awake()
    {
        Instance = this;
        foreach (var entry in unitPanels)
        {
            if (!panelMap.ContainsKey(entry.unitName))
            {
                panelMap[entry.unitName] = entry.panel;
                entry.panel.Hide(); // start hidden
            }
        }
    }
    private void Update()
    {
        if (activePanel != null && Input.GetMouseButtonDown(0))
        {
            UnitInfoPanelManager.Instance.HideActivePanel();
        }
    }
    public void RefreshActivePanelIfMatches(UnitAI unit)
    {
        if (activePanel != null && activePanel.IsShowingUnit(unit))
        {
            activePanel.ForceRefresh();
        }
    }

    public void ShowPanel(UnitAI unit)
    {
        // Hide old one if open
        if (activePanel != null)
            activePanel.Hide();

        // Find the right panel
        if (panelMap.TryGetValue(unit.unitName, out var panel))
        {
            activePanel = panel;
            activePanel.Show(unit);
        }
        else
        {
            Debug.LogWarning($"No panel assigned for unit {unit.unitName}");
        }
    }

    public void HideActivePanel()
    {
        if (activePanel != null)
        {
            activePanel.Hide();
            activePanel = null;
        }
    }
}
