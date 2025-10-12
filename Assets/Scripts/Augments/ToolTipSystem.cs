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
    public TextMeshProUGUI tooltipText; // Changed to TextMeshProUGUI
    public Image tooltipBackground;
    public CanvasGroup tooltipCanvasGroup;

    [Header("Settings")]
    public float fadeSpeed = 5f;
    public Vector2 offset = new Vector2(20, 20); // Increased offset
    public float maxWidth = 250f;

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
            instance.Show(text, position);
        }
    }

    public static void HideTooltip()
    {
        if (instance != null)
        {
            instance.Hide();
        }
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

        // IMPORTANT: Wait one frame for layout to update, then position
        StartCoroutine(PositionTooltipNextFrame(position));

        // Fade in
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTooltip(1f));

        isShowing = true;
        Debug.Log($"🔍 Showing tooltip: {text} at {position}");
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
        // Wait for layout to rebuild
        yield return null;
        PositionTooltip(screenPosition);
    }

    private void PositionTooltip(Vector2 screenPosition)
    {
        if (tooltipRect == null || tooltipCanvas == null)
        {
            Debug.LogError("❌ Tooltip positioning failed - missing components");
            return;
        }

        // FIX: Convert screen position to canvas space properly
        RectTransform canvasRect = tooltipCanvas.transform as RectTransform;

        Vector2 localPoint;
        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            tooltipCanvas.worldCamera,
            out localPoint);

        if (!success)
        {
            Debug.LogError("❌ Failed to convert screen point to local point");
            return;
        }

        // Add offset
        localPoint += offset;

        // Get tooltip size after content is set
        Vector2 tooltipSize = tooltipRect.sizeDelta;
        Vector2 canvasSize = canvasRect.sizeDelta;

        // FIX: Clamp to screen bounds more accurately
        float halfCanvasWidth = canvasSize.x * 0.5f;
        float halfCanvasHeight = canvasSize.y * 0.5f;
        float halfTooltipWidth = tooltipSize.x * 0.5f;
        float halfTooltipHeight = tooltipSize.y * 0.5f;

        // Clamp X (keep within screen)
        if (localPoint.x + halfTooltipWidth > halfCanvasWidth)
        {
            localPoint.x = screenPosition.x - tooltipSize.x - offset.x; // Show on left side instead
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, new Vector2(localPoint.x, screenPosition.y), tooltipCanvas.worldCamera, out localPoint);
        }

        // Clamp Y (keep within screen)
        localPoint.y = Mathf.Clamp(localPoint.y, -halfCanvasHeight + halfTooltipHeight, halfCanvasHeight - halfTooltipHeight);

        tooltipRect.localPosition = localPoint;

        Debug.Log($"🔍 Positioned tooltip at local: {localPoint}, screen: {screenPosition}, size: {tooltipSize}");
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

        // Add background image
        tooltipBackground = tooltipObject.AddComponent<Image>();
        tooltipBackground.color = new Color(0.05f, 0.05f, 0.05f, 0.95f); // Darker background

        // Add outline
        Outline outline = tooltipObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        outline.effectDistance = new Vector2(1, -1);

        // Set initial size (will be adjusted by ContentSizeFitter)
        RectTransform tooltipRect = tooltipObject.GetComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(maxWidth, 50);
        tooltipRect.pivot = new Vector2(0, 1); // Top-left pivot for easier positioning

        // Add text
        GameObject textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(tooltipObject.transform, false);

        tooltipText = textObj.AddComponent<TextMeshProUGUI>();
        tooltipText.text = "Tooltip Text";
        tooltipText.fontSize = 12;
        tooltipText.color = Color.white;
        tooltipText.alignment = TextAlignmentOptions.TopLeft;
        tooltipText.enableWordWrapping = true;

        // Set text rect to fill parent with padding
        RectTransform textRect = tooltipText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8, 8);
        textRect.offsetMax = new Vector2(-8, -8);

        // Add content size fitter for auto-sizing
        ContentSizeFitter sizeFitter = tooltipObject.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        Debug.Log("✅ Enhanced Tooltip UI created");
    }
}
