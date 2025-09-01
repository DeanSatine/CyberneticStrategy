using TMPro;
using UnityEngine;

public class TraitUIPanel : MonoBehaviour
{
    [Header("Text References")]
    public TextMeshProUGUI currentCountText;
    public TextMeshProUGUI activeBreakpointText;
    public TextMeshProUGUI nextBreakpointText;

    // Called by TraitUIManager
    public void UpdateTexts(int count, int activeThreshold, int nextThreshold)
    {
        if (currentCountText) currentCountText.text = count.ToString();
        if (activeBreakpointText) activeBreakpointText.text = activeThreshold > 0 ? activeThreshold.ToString() : "-";
        if (nextBreakpointText) nextBreakpointText.text = nextThreshold > 0 ? nextThreshold.ToString() : "-";
    }
}
