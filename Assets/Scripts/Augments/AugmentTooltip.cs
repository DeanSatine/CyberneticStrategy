// /Assets/Scripts/UI/AugmentTooltip.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class AugmentTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tooltip Settings")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private float showDelay = 0.5f;

    private BaseAugment augment;
    private Coroutine showTooltipCoroutine;

    public void Initialize(BaseAugment augment)
    {
        this.augment = augment;

        // Create tooltip if it doesn't exist
        if (tooltipPanel == null)
        {
            CreateTooltip();
        }

        tooltipPanel.SetActive(false);
    }

    private void CreateTooltip()
    {
        // Create a simple tooltip panel
        tooltipPanel = new GameObject("Tooltip");
        tooltipPanel.transform.SetParent(transform);

        // Add background
        Image background = tooltipPanel.AddComponent<Image>();
        background.color = new Color(0, 0, 0, 0.8f);

        // Add text
        GameObject textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(tooltipPanel.transform);
        tooltipText = textObj.AddComponent<TMP_Text>();
        tooltipText.fontSize = 14;
        tooltipText.color = Color.white;

        // Position tooltip
        RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(300, 100);
        tooltipRect.anchoredPosition = new Vector2(0, 50);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (showTooltipCoroutine != null)
        {
            StopCoroutine(showTooltipCoroutine);
        }

        showTooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (showTooltipCoroutine != null)
        {
            StopCoroutine(showTooltipCoroutine);
            showTooltipCoroutine = null;
        }

        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    private System.Collections.IEnumerator ShowTooltipAfterDelay()
    {
        yield return new WaitForSecondsRealtime(showDelay);

        if (tooltipPanel != null && augment != null)
        {
            tooltipText.text = $"{augment.augmentName}\n{augment.description}";
            tooltipPanel.SetActive(true);
        }
    }
}
