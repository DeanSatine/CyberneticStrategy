using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AugmentViewerItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;

    public void Setup(BaseAugment augment)
    {
        if (nameText != null) nameText.text = augment.augmentName;
        if (descriptionText != null) descriptionText.text = augment.description;
        if (iconImage != null && augment.icon != null) iconImage.sprite = augment.icon;

        // Set background color based on augment type
        if (backgroundImage != null)
        {
            Color bgColor = GetAugmentColor(augment.type);
            backgroundImage.color = bgColor;
        }
    }

    private Color GetAugmentColor(AugmentType type)
    {
        switch (type)
        {
            case AugmentType.Origin: return new Color(0.2f, 0.6f, 0.2f, 0.8f);
            case AugmentType.Class: return new Color(0.2f, 0.4f, 0.8f, 0.8f);
            case AugmentType.Generic: return new Color(0.6f, 0.4f, 0.8f, 0.8f);
            default: return new Color(0.3f, 0.3f, 0.3f, 0.8f);
        }
    }
}
