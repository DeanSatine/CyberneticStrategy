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
    private bool isPreviewMode = false;

    public void Setup(BaseAugment augment)
    {
        this.augment = augment;
        this.isPreviewMode = false;
        SetupUI(augment);
    }

    public void SetupPreview(BaseAugment augment)
    {
        this.augment = augment;
        this.isPreviewMode = true;
        SetupUI(augment);
    }

    private void SetupUI(BaseAugment augment)
    {
        // Set icon
        if (iconImage != null)
        {
            if (augment.icon != null)
            {
                iconImage.sprite = augment.icon;
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.color = GetAugmentColor(augment.type);
            }
        }

        // Set name with perfect centering
        if (nameText != null)
        {
            nameText.text = augment.augmentName;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.verticalAlignment = VerticalAlignmentOptions.Middle;
        }

        // Set background color
        if (backgroundImage != null)
        {
            Color bgColor = GetAugmentColor(augment.type);
            bgColor.a = 0.3f; // Semi-transparent
            backgroundImage.color = bgColor;
        }

        Debug.Log($"🔍 Setup {(isPreviewMode ? "preview" : "runtime")} item: {augment.augmentName}");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (augment == null || string.IsNullOrEmpty(augment.description)) return;

#if UNITY_EDITOR
        if (!Application.isPlaying && isPreviewMode)
        {
            Debug.Log($"🔍 Would show tooltip: {augment.description}");
            return;
        }
#endif

        // Only show tooltip in play mode
        if (Application.isPlaying)
        {
            Vector3 worldPos = transform.position;
            Vector2 screenPos = worldPos;
            screenPos.x += 100f; // Offset to the right
            screenPos.y += 50f;  // Offset upward

            TooltipSystem.ShowTooltip(augment.description, screenPos);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (Application.isPlaying)
        {
            TooltipSystem.HideTooltip();
        }
    }

    private Color GetAugmentColor(AugmentType type)
    {
        switch (type)
        {
            case AugmentType.Origin:
                return new Color(0.2f, 0.8f, 0.2f, 1f); // Green
            case AugmentType.Class:
                return new Color(0.2f, 0.4f, 0.9f, 1f); // Blue
            case AugmentType.Generic:
                return new Color(0.8f, 0.4f, 0.9f, 1f); // Purple
            default:
                return new Color(0.6f, 0.6f, 0.6f, 1f); // Gray
        }
    }

    private void OnDestroy()
    {
        if (Application.isPlaying)
        {
            TooltipSystem.HideTooltip();
        }
    }
}
