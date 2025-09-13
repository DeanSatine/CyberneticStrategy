using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TraitUIPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public TextMeshProUGUI currentCountText;
    public TextMeshProUGUI activeBreakpointText;
    public TextMeshProUGUI nextBreakpointText;

    private CanvasGroup canvasGroup;

    [Header("Tooltip Settings")]
    public GameObject tooltipPrefab;   // Assign the tooltip object already in your scene (disabled by default)

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (tooltipPrefab != null)
            tooltipPrefab.SetActive(false); // ensure it's hidden at start
    }

    public void UpdateTexts(int count, int activeThreshold, int nextThreshold, bool isActive)
    {
        if (currentCountText) currentCountText.text = count.ToString();
        if (activeBreakpointText) activeBreakpointText.text = activeThreshold > 0 ? activeThreshold.ToString() : "-";
        if (nextBreakpointText) nextBreakpointText.text = nextThreshold > 0 ? nextThreshold.ToString() : "-";

        // fade if not active
        canvasGroup.alpha = isActive ? 1f : 0.5f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipPrefab != null)
            tooltipPrefab.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipPrefab != null)
            tooltipPrefab.SetActive(false);
    }
}
