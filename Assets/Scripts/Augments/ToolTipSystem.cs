using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class TooltipSystem : MonoBehaviour
{
    private static TooltipSystem instance;

    [Header("Tooltip UI")]
    public GameObject tooltipObject;
    public TextMeshProUGUI tooltipText;
    public Image tooltipBackground;
    public CanvasGroup tooltipCanvasGroup;

    [Header("Settings")]
    public float fadeSpeed = 8f;
    public Vector2 offset = new Vector2(15, 15);
    public float maxWidth = 300f; // Increased max width
    public int maxCharactersPerLine = 45; // Character limit per line

    private RectTransform tooltipRect;
    private Canvas tooltipCanvas;
    private bool isShowing = false;
    private Coroutine fadeCoroutine;

    public static TooltipSystem Instance => instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SetupTooltip();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupTooltip()
    {
        if (tooltipObject == null) CreateTooltipUI();

        tooltipRect = tooltipObject.GetComponent<RectTransform>();
        tooltipCanvas = GetComponentInParent<Canvas>();

        // Start with tooltip hidden
        HideTooltip();
    }

    public static void ShowTooltip(string text, Vector2 position)
    {
        if (instance != null)
        {
            string formattedText = instance.FormatTooltipText(text);
            instance.Show(formattedText, position);
        }
    }

    public static void HideTooltip()
    {
        if (instance != null)
        {
            instance.Hide();
        }
    }

    private string FormatTooltipText(string originalText)
    {
        if (string.IsNullOrEmpty(originalText)) return originalText;

        // Break long text into manageable chunks
        string[] words = originalText.Split(' ');
        string formattedText = "";
        string currentLine = "";

        foreach (string word in words)
        {
            // Check if adding this word would exceed the line limit
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

        // Add the last line
        if (currentLine.Length > 0)
        {
            formattedText += currentLine;
        }

        return formattedText;
    }

    private void Show(string text, Vector2 position)
    {
        if (tooltipText != null)
        {
            tooltipText.text = text;
        }

        if (tooltipObject != null)
        {
            tooltipObject.SetActive(true);
        }

        // Wait one frame for layout to update, then position
        StartCoroutine(PositionTooltipNextFrame(position));

        // Fade in
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTooltip(1f));

        isShowing = true;
    }

    private void Hide()
    {
        if (!isShowing) return;

        // Fade out
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTooltip(0f));

        isShowing = false;
    }

    private IEnumerator PositionTooltipNextFrame(Vector2 screenPosition)
    {
        yield return null; // Wait for layout rebuild
        PositionTooltip(screenPosition);
    }

    private void PositionTooltip(Vector2 screenPosition)
    {
        if (tooltipRect == null || tooltipCanvas == null) return;

        RectTransform canvasRect = tooltipCanvas.transform as RectTransform;

        Vector2 localPoint;
        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            tooltipCanvas.worldCamera,
            out localPoint);

        if (!success) return;

        // Add offset
        localPoint += offset;

        // Get tooltip size after content is set
        Vector2 tooltipSize = tooltipRect.sizeDelta;
        Vector2 canvasSize = canvasRect.sizeDelta;

        // Smart positioning to keep tooltip on screen
        float halfCanvasWidth = canvasSize.x * 0.5f;
        float halfCanvasHeight = canvasSize.y * 0.5f;
        float halfTooltipWidth = tooltipSize.x * 0.5f;
        float halfTooltipHeight = tooltipSize.y * 0.5f;

        // If tooltip would go off right edge, show it on the left instead
        if (localPoint.x + halfTooltipWidth > halfCanvasWidth)
        {
            localPoint.x = localPoint.x - tooltipSize.x - (offset.x * 2);
        }

        // If tooltip would go off top edge, show it below instead  
        if (localPoint.y + halfTooltipHeight > halfCanvasHeight)
        {
            localPoint.y = localPoint.y - tooltipSize.y - (offset.y * 2);
        }

        // Clamp to screen bounds as final safety
        localPoint.x = Mathf.Clamp(localPoint.x, -halfCanvasWidth + halfTooltipWidth, halfCanvasWidth - halfTooltipWidth);
        localPoint.y = Mathf.Clamp(localPoint.y, -halfCanvasHeight + halfTooltipHeight, halfCanvasHeight - halfTooltipHeight);

        tooltipRect.localPosition = localPoint;
    }

    private IEnumerator FadeTooltip(float targetAlpha)
    {
        if (tooltipCanvasGroup == null) yield break;

        float startAlpha = tooltipCanvasGroup.alpha;
        float elapsed = 0f;
        float duration = 1f / fadeSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            tooltipCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null;
        }

        tooltipCanvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f && tooltipObject != null)
        {
            tooltipObject.SetActive(false);
        }
    }

    private void CreateTooltipUI()
    {
        // Create tooltip object
        tooltipObject = new GameObject("Tooltip");
        tooltipObject.transform.SetParent(transform, false);

        // Add canvas group for fading
        tooltipCanvasGroup = tooltipObject.AddComponent<CanvasGroup>();

        // Add background image with better styling
        tooltipBackground = tooltipObject.AddComponent<Image>();
        tooltipBackground.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);

        // Add outline for better visibility
        Outline outline = tooltipObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.3f);
        outline.effectDistance = new Vector2(1, -1);

        // Set rect transform
        RectTransform tooltipRect = tooltipObject.GetComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(maxWidth, 50);
        tooltipRect.pivot = new Vector2(0, 1); // Top-left pivot

        // Add text
        GameObject textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(tooltipObject.transform, false);

        tooltipText = textObj.AddComponent<TextMeshProUGUI>();
        tooltipText.text = "Tooltip Text";
        tooltipText.fontSize = 14; // Slightly larger font
        tooltipText.fontStyle = FontStyles.Normal;
        tooltipText.color = Color.white;
        tooltipText.alignment = TextAlignmentOptions.TopLeft;
        tooltipText.enableWordWrapping = true;
        tooltipText.overflowMode = TextOverflowModes.Overflow;

        // Set text rect to fill parent with padding
        RectTransform textRect = tooltipText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 8); // More padding
        textRect.offsetMax = new Vector2(-10, -8);

        // Add content size fitter for auto-sizing
        ContentSizeFitter sizeFitter = tooltipObject.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        Debug.Log("✅ Enhanced Tooltip UI created with better formatting");
    }
}
