using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CompactAugmentItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public Image backgroundImage;

    private BaseAugment augment;

    public void Setup(BaseAugment augment)
    {
        this.augment = augment;

        // Set icon
        if (iconImage != null && augment.icon != null)
        {
            iconImage.sprite = augment.icon;
            iconImage.color = Color.white;
        }
        else if (iconImage != null)
        {
            // Use a default icon or hide
            iconImage.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        }

        // Set name
        if (nameText != null)
        {
            nameText.text = augment.augmentName;
            nameText.fontStyle = FontStyles.Bold;
        }

        // Set background color based on augment type
        if (backgroundImage != null)
        {
            Color bgColor = GetAugmentColor(augment.type);
            backgroundImage.color = bgColor;
        }

        Debug.Log($"🔍 Setup compact augment item: {augment.augmentName}");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (augment != null && !string.IsNullOrEmpty(augment.description))
        {
            // FIX: Use the UI element's position instead of just mouse position
            RectTransform rectTransform = GetComponent<RectTransform>();
            Vector3 worldPos = rectTransform.position;
            Vector2 screenPos = worldPos;

            // Add some offset to show tooltip to the right of the item
            screenPos.x += rectTransform.sizeDelta.x * 0.5f;

            TooltipSystem.ShowTooltip(augment.description, screenPos);
            Debug.Log($"🔍 Showing tooltip for: {augment.augmentName} at {screenPos}");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipSystem.HideTooltip();
    }

    private Color GetAugmentColor(AugmentType type)
    {
        switch (type)
        {
            case AugmentType.Origin:
                return new Color(0.2f, 0.6f, 0.2f, 0.8f); // Green
            case AugmentType.Class:
                return new Color(0.2f, 0.4f, 0.8f, 0.8f); // Blue
            case AugmentType.Generic:
                return new Color(0.6f, 0.4f, 0.8f, 0.8f); // Purple
            default:
                return new Color(0.3f, 0.3f, 0.3f, 0.8f); // Gray
        }
    }

    private void OnDestroy()
    {
        TooltipSystem.HideTooltip();
    }
}
