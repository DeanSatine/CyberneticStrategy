using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
    public float maxWidth = 300f;
    public int maxCharactersPerLine = 45;

    private RectTransform tooltipRect;
    private Canvas tooltipCanvas;
    private bool isShowing = false;
    private Coroutine fadeCoroutine;

    public static TooltipSystem Instance => instance;

    private void Awake()
    {
        // Only create if no instance exists
        if (instance == null)
        {
            instance = this;
            SetupTooltip();
            Debug.Log("✅ TooltipSystem initialized and hidden");
        }
        else if (instance != this)
        {
            // Destroy duplicate tooltip systems
            Debug.Log("🗑️ Destroying duplicate TooltipSystem");
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // CRITICAL: Ensure tooltip is completely hidden at start
        ForceHideTooltip();
    }

    private void SetupTooltip()
    {
        if (tooltipObject == null) CreateTooltipUI();

        tooltipRect = tooltipObject.GetComponent<RectTransform>();
        tooltipCanvas = GetComponentInParent<Canvas>();

        // IMPORTANT: Start completely hidden
        ForceHideTooltip();
    }

    private void ForceHideTooltip()
    {
        if (tooltipObject != null)
        {
            tooltipObject.SetActive(false);
        }

        if (tooltipCanvasGroup != null)
        {
            tooltipCanvasGroup.alpha = 0f;
        }

        isShowing = false;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        Debug.Log("🔒 Tooltip forcibly hidden");
    }

    public static void ShowTooltip(string text, Vector2 position)
    {
        if (instance != null && !string.IsNullOrEmpty(text))
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

        // Position tooltip after one frame
        StartCoroutine(PositionTooltipNextFrame(position));

        // Fade in
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTooltip(1f));

        isShowing = true;
    }

    private void Hide()
    {
        if (!isShowing) return;

        // Fade out and deactivate
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTooltip(0f));

        isShowing = false;
    }

    private IEnumerator PositionTooltipNextFrame(Vector2 screenPosition)
    {
        yield return null; // Wait for layout
        PositionTooltip(screenPosition);
    }

    private void PositionTooltip(Vector2 screenPosition)
    {
        if (tooltipRect == null || tooltipCanvas == null) return;

        RectTransform canvasRect = tooltipCanvas.transform as RectTransform;
        Vector2 localPoint;

        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPosition, tooltipCanvas.worldCamera, out localPoint);

        if (!success) return;

        localPoint += offset;

        Vector2 tooltipSize = tooltipRect.sizeDelta;
        Vector2 canvasSize = canvasRect.sizeDelta;

        float halfCanvasWidth = canvasSize.x * 0.5f;
        float halfCanvasHeight = canvasSize.y * 0.5f;
        float halfTooltipWidth = tooltipSize.x * 0.5f;
        float halfTooltipHeight = tooltipSize.y * 0.5f;

        // Smart positioning
        if (localPoint.x + halfTooltipWidth > halfCanvasWidth)
        {
            localPoint.x = localPoint.x - tooltipSize.x - (offset.x * 2);
        }

        if (localPoint.y + halfTooltipHeight > halfCanvasHeight)
        {
            localPoint.y = localPoint.y - tooltipSize.y - (offset.y * 2);
        }

        // Clamp to bounds
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

        // Hide object when faded out completely
        if (targetAlpha <= 0f && tooltipObject != null)
        {
            tooltipObject.SetActive(false);
        }
    }

    private void CreateTooltipUI()
    {
        // Create tooltip object
        tooltipObject = new GameObject("RuntimeTooltip");
        tooltipObject.transform.SetParent(transform, false);

        // Add canvas group
        tooltipCanvasGroup = tooltipObject.AddComponent<CanvasGroup>();
        tooltipCanvasGroup.alpha = 0f; // Start invisible

        // Add background
        tooltipBackground = tooltipObject.AddComponent<Image>();
        tooltipBackground.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);

        // Add outline
        Outline outline = tooltipObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.3f);
        outline.effectDistance = new Vector2(1, -1);

        // Set rect transform
        RectTransform tooltipRect = tooltipObject.GetComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(maxWidth, 50);
        tooltipRect.pivot = new Vector2(0, 1);

        // Add text
        GameObject textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(tooltipObject.transform, false);

        tooltipText = textObj.AddComponent<TextMeshProUGUI>();
        tooltipText.fontSize = 14;
        tooltipText.fontStyle = FontStyles.Normal;
        tooltipText.color = Color.white;
        tooltipText.alignment = TextAlignmentOptions.TopLeft;
        tooltipText.enableWordWrapping = true;
        tooltipText.overflowMode = TextOverflowModes.Overflow;

        // Set text rect
        RectTransform textRect = tooltipText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 8);
        textRect.offsetMax = new Vector2(-10, -8);

        // Add content size fitter
        ContentSizeFitter sizeFitter = tooltipObject.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Start hidden
        tooltipObject.SetActive(false);

        Debug.Log("✅ Runtime Tooltip UI created (hidden by default)");
    }

    // Debug methods
    [ContextMenu("Force Hide Tooltip")]
    public void DebugForceHide()
    {
        ForceHideTooltip();
    }

    [ContextMenu("Test Show Tooltip")]
    public void DebugTestShow()
    {
        ShowTooltip("Test tooltip text for debugging purposes!", Input.mousePosition);
    }
}
