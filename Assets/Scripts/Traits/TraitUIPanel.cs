using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TraitUIPanel : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI currentCountText;
    public TextMeshProUGUI activeBreakpointText;
    public TextMeshProUGUI nextBreakpointText;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // Add a CanvasGroup if missing
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void UpdateTexts(int count, int activeThreshold, int nextThreshold, bool isActive)
    {
        if (currentCountText) currentCountText.text = count.ToString();
        if (activeBreakpointText) activeBreakpointText.text = activeThreshold > 0 ? activeThreshold.ToString() : "-";
        if (nextBreakpointText) nextBreakpointText.text = nextThreshold > 0 ? nextThreshold.ToString() : "-";

        // fade if not active
        canvasGroup.alpha = isActive ? 1f : 0.5f;
    }
}
