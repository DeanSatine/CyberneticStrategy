// /Assets/Scripts/UI/AugmentButton.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class AugmentButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Image backgroundImage;
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text typeText;
    public TMP_Text descriptionText;
    public Button button;

    [Header("Visual Settings")]
    public Color hoverColor = Color.yellow;
    public Color normalColor = Color.white;
    public float hoverScale = 1.05f;

    private BaseAugment currentAugment;
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    public void ConfigureForAugment(BaseAugment augment)
    {
        currentAugment = augment;

        if (augment == null) return;

        // Set icon
        if (iconImage != null && augment.icon != null)
        {
            iconImage.sprite = augment.icon;
        }

        // Set name
        if (nameText != null)
        {
            nameText.text = augment.augmentName;
        }

        // Set description
        if (descriptionText != null)
        {
            descriptionText.text = augment.description;
        }

        // Set type with color coding
        if (typeText != null)
        {
            typeText.text = augment.type.ToString();
            typeText.color = GetTypeColor(augment.type);
        }

        // Set background color
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.Lerp(normalColor, augment.augmentColor, 0.3f);
        }
    }

    private Color GetTypeColor(AugmentType type)
    {
        switch (type)
        {
            case AugmentType.Origin: return Color.red;
            case AugmentType.Class: return Color.blue;
            case AugmentType.Generic: return Color.green;
            default: return Color.white;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Scale up slightly
        transform.localScale = originalScale * hoverScale;

        // Change background color
        if (backgroundImage != null && currentAugment != null)
        {
            backgroundImage.color = Color.Lerp(hoverColor, currentAugment.augmentColor, 0.5f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Reset scale
        transform.localScale = originalScale;

        // Reset background color
        if (backgroundImage != null && currentAugment != null)
        {
            backgroundImage.color = Color.Lerp(normalColor, currentAugment.augmentColor, 0.3f);
        }
    }
}
