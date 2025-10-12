using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EditorPreviewTooltip : MonoBehaviour
{
    [Header("Preview Settings")]
    [SerializeField] private bool showPreview = false;
    [SerializeField] private string previewText = "Clobbertrons jump to their target on combat start and jump once more at 10% health, and gain 20 Attack Damage and no armour instead. Gain a B.O.P.";
    [SerializeField] private Vector2 previewPosition = new Vector2(400, 300);

    [Header("Tooltip Settings")]
    [SerializeField] private float maxWidth = 300f;
    [SerializeField] private int maxCharactersPerLine = 45;
    [SerializeField] private float padding = 10f;
    [SerializeField] private int fontSize = 14;

    private GameObject previewTooltipObject;
    private Canvas previewCanvas;

    private void Update()
    {
        // Only work in edit mode
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (showPreview && previewTooltipObject == null)
            {
                CreatePreviewTooltip();
            }
            else if (!showPreview && previewTooltipObject != null)
            {
                DestroyPreviewTooltip();
            }
            else if (showPreview && previewTooltipObject != null)
            {
                UpdatePreviewTooltip();
            }
        }
#endif
    }

#if UNITY_EDITOR
    private void CreatePreviewTooltip()
    {
        // Find or create canvas
        previewCanvas = FindObjectOfType<Canvas>();
        if (previewCanvas == null)
        {
            GameObject canvasObj = new GameObject("PreviewCanvas");
            previewCanvas = canvasObj.AddComponent<Canvas>();
            previewCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            previewCanvas.sortingOrder = 9999;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create tooltip object
        previewTooltipObject = new GameObject("PreviewTooltip");
        previewTooltipObject.transform.SetParent(previewCanvas.transform, false);

        // Add background
        Image bgImage = previewTooltipObject.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);

        // Add outline
        Outline outline = previewTooltipObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.3f);
        outline.effectDistance = new Vector2(1, -1);

        // Set rect transform
        RectTransform tooltipRect = previewTooltipObject.GetComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(maxWidth, 50);
        tooltipRect.pivot = new Vector2(0, 1);

        // Add text
        GameObject textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(previewTooltipObject.transform, false);

        TextMeshProUGUI tooltipText = textObj.AddComponent<TextMeshProUGUI>();
        tooltipText.fontSize = fontSize;
        tooltipText.fontStyle = FontStyles.Normal;
        tooltipText.color = Color.white;
        tooltipText.alignment = TextAlignmentOptions.TopLeft;
        tooltipText.enableWordWrapping = true;
        tooltipText.overflowMode = TextOverflowModes.Overflow;

        // Set text rect
        RectTransform textRect = tooltipText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(padding, padding);
        textRect.offsetMax = new Vector2(-padding, -padding);

        // Add content size fitter
        ContentSizeFitter sizeFitter = previewTooltipObject.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        UpdatePreviewTooltip();

        Debug.Log("✅ Preview tooltip created for editing!");
    }

    private void UpdatePreviewTooltip()
    {
        if (previewTooltipObject == null) return;

        // Update text
        TextMeshProUGUI tooltipText = previewTooltipObject.GetComponentInChildren<TextMeshProUGUI>();
        if (tooltipText != null)
        {
            tooltipText.text = FormatTooltipText(previewText);
            tooltipText.fontSize = fontSize;
        }

        // Update position
        RectTransform tooltipRect = previewTooltipObject.GetComponent<RectTransform>();
        if (tooltipRect != null)
        {
            tooltipRect.position = previewPosition;
            tooltipRect.sizeDelta = new Vector2(maxWidth, tooltipRect.sizeDelta.y);
        }

        // Update background
        Image bgImage = previewTooltipObject.GetComponent<Image>();
        if (bgImage != null)
        {
            bgImage.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);
        }
    }

    private void DestroyPreviewTooltip()
    {
        if (previewTooltipObject != null)
        {
            DestroyImmediate(previewTooltipObject);
            previewTooltipObject = null;
        }
    }

    private string FormatTooltipText(string originalText)
    {
        if (string.IsNullOrEmpty(originalText)) return originalText;

        string[] words = originalText.Split(' ');
        string formattedText = "";
        string currentLine = "";

        foreach (string word in words)
        {
            if ((currentLine + " " + word).Length > maxCharactersPerLine && currentLine.Length > 0)
            {
                formattedText += currentLine + "\n";
                currentLine = word;
            }
            else
            {
                if (currentLine.Length > 0)
                    currentLine += " " + word;
                else
                    currentLine = word;
            }
        }

        if (currentLine.Length > 0)
        {
            formattedText += currentLine;
        }

        return formattedText;
    }

    [ContextMenu("Show Preview Tooltip")]
    public void ShowPreviewTooltip()
    {
        showPreview = true;
    }

    [ContextMenu("Hide Preview Tooltip")]
    public void HidePreviewTooltip()
    {
        showPreview = false;
    }

    [ContextMenu("Save Current Position")]
    public void SaveCurrentPosition()
    {
        if (previewTooltipObject != null)
        {
            RectTransform rect = previewTooltipObject.GetComponent<RectTransform>();
            if (rect != null)
            {
                previewPosition = rect.position;
                Debug.Log($"✅ Saved tooltip position: {previewPosition}");
            }
        }
    }
#endif

    private void OnValidate()
    {
        // Update preview when values change in inspector
#if UNITY_EDITOR
        if (!Application.isPlaying && showPreview)
        {
            UpdatePreviewTooltip();
        }
#endif
    }
}
