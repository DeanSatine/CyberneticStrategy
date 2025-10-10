// /Assets/Scripts/UI/AugmentSelectionButton.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class AugmentSelectionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;

    [Header("Visual Settings")]
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private float hoverScale = 1.05f;

    private BaseAugment augment;
    private AugmentSelectionUI selectionUI;
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(OnButtonPressed);
        }
    }

    public void Initialize(BaseAugment augment, AugmentSelectionUI selectionUI)
    {
        this.augment = augment;
        this.selectionUI = selectionUI;

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
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

    private void OnButtonPressed()
    {
        if (selectionUI != null && augment != null)
        {
            selectionUI.OnAugmentSelected(augment);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Scale up slightly
        transform.localScale = originalScale * hoverScale;

        // Change background color
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.Lerp(hoverColor, augment.augmentColor, 0.5f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Reset scale
        transform.localScale = originalScale;

        // Reset background color
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.Lerp(normalColor, augment.augmentColor, 0.3f);
        }
    }
}
