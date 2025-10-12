using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SimpleTooltipSystem : MonoBehaviour
{
    private static SimpleTooltipSystem instance;

    [Header("Settings")]
    public TooltipSettings tooltipSettings;

    [Header("UI Elements (Auto-Created if not assigned)")]
    public GameObject tooltipObject;
    public TextMeshProUGUI tooltipText;
    public Image tooltipBackground;
    public CanvasGroup tooltipCanvasGroup;

    private RectTransform tooltipRect;
    private Canvas tooltipCanvas;
    private bool isShowing = false;
    private Coroutine fadeCoroutine;

    public static SimpleTooltipSystem Instance => instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            SetupTooltip();
            Debug.Log("✅ Simple TooltipSystem initialized");
        }
        else if (instance != this)
        {
            Debug.Log("🗑️ Destroying duplicate SimpleTooltipSystem");
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        ForceHideTooltip();
    }

    private void SetupTooltip()
    {
        if (tooltipObject == null) CreateTooltipUI();

        tooltipRect = tooltipObject.GetComponent<RectTransform>();
        tooltipCanvas = GetComponentInParent<Canvas>();

        // Apply settings immediately
        ApplySettings();

        ForceHideTooltip();
    }

    public void ApplySettings()
    {
        if (tooltipSettings == null || tooltipObject == null) return;

        // Apply text settings
        if (tooltipText != null)
        {
            tooltipText.fontSize = tooltipSettings.fontSize;
            tooltipText.fontStyle = tooltipSettings.fontStyle;
            tooltipText.color = tooltipSettings.textColor;
            tooltipText.alignment = tooltipSettings.textAlignment;
            tooltipText.enableWordWrapping = true;

            if (tooltipSettings.customFont != null)
            {
                tooltipText.font = tooltipSettings.customFont;
            }
        }

        // Apply background settings
        if (tooltipBackground != null)
        {
            tooltipBackground.color = tooltipSettings.backgroundColor;
        }

        // Apply outline settings
        Outline outline = tooltipObject.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = tooltipSettings.useOutline;
            outline.effectColor = tooltipSettings.outlineColor;
            outline.effectDistance = tooltipSettings.outlineDistance;
        }

        // Apply padding
        if (tooltipText != null)
        {
            RectTransform textRect = tooltipText.GetComponent<RectTransform>();
            textRect.offsetMin = new Vector2(tooltipSettings.leftPadding, tooltipSettings.bottomPadding);
            textRect.offsetMax = new Vector2(-tooltipSettings.rightPadding, -tooltipSettings.topPadding);
        }

        // Apply dimensions
        if (tooltipRect != null)
        {
            tooltipRect.sizeDelta = new Vector2(tooltipSettings.maxWidth, tooltipSettings.minHeight);
        }

        Debug.Log("✅ Tooltip settings applied");
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
        if (string.IsNullOrEmpty(originalText) || tooltipSettings == null) return originalText;

        string[] words = originalText.Split(' ');
        string formattedText = "";
        string currentLine = "";

        foreach (string word in words)
        {
            if ((currentLine + " " + word).Length > tooltipSettings.maxCharactersPerLine && currentLine.Length > 0)
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
        // Apply current settings before showing
        ApplySettings();

        if (tooltipText != null)
        {
            tooltipText.text = text;
        }

        if (tooltipObject != null)
        {
            tooltipObject.SetActive(true);
        }

        StartCoroutine(PositionTooltipNextFrame(position));

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTooltip(1f));

        isShowing = true;
    }

    private void Hide()
    {
        if (!isShowing) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTooltip(0f));

        isShowing = false;
    }

    private IEnumerator PositionTooltipNextFrame(Vector2 screenPosition)
    {
        yield return null;
        PositionTooltip(screenPosition);
    }

    private void PositionTooltip(Vector2 screenPosition)
    {
        if (tooltipRect == null || tooltipCanvas == null || tooltipSettings == null) return;

        RectTransform canvasRect = tooltipCanvas.transform as RectTransform;
        Vector2 localPoint;

        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPosition, tooltipCanvas.worldCamera, out localPoint);

        if (!success) return;

        localPoint += tooltipSettings.offsetFromCursor;

        Vector2 tooltipSize = tooltipRect.sizeDelta;
        Vector2 canvasSize = canvasRect.sizeDelta;

        float halfCanvasWidth = canvasSize.x * 0.5f;
        float halfCanvasHeight = canvasSize.y * 0.5f;
        float halfTooltipWidth = tooltipSize.x * 0.5f;
        float halfTooltipHeight = tooltipSize.y * 0.5f;

        if (localPoint.x + halfTooltipWidth > halfCanvasWidth)
        {
            localPoint.x = localPoint.x - tooltipSize.x - (tooltipSettings.offsetFromCursor.x * 2);
        }

        if (localPoint.y + halfTooltipHeight > halfCanvasHeight)
        {
            localPoint.y = localPoint.y - tooltipSize.y - (tooltipSettings.offsetFromCursor.y * 2);
        }

        localPoint.x = Mathf.Clamp(localPoint.x, -halfCanvasWidth + halfTooltipWidth, halfCanvasWidth - halfTooltipWidth);
        localPoint.y = Mathf.Clamp(localPoint.y, -halfCanvasHeight + halfTooltipHeight, halfCanvasHeight - halfTooltipHeight);

        tooltipRect.localPosition = localPoint;
    }

    private IEnumerator FadeTooltip(float targetAlpha)
    {
        if (tooltipCanvasGroup == null || tooltipSettings == null) yield break;

        float startAlpha = tooltipCanvasGroup.alpha;
        float elapsed = 0f;
        float duration = 1f / tooltipSettings.fadeSpeed;

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
        tooltipObject = new GameObject("SimpleTooltip");
        tooltipObject.transform.SetParent(transform, false);

        tooltipCanvasGroup = tooltipObject.AddComponent<CanvasGroup>();
        tooltipCanvasGroup.alpha = 0f;

        tooltipBackground = tooltipObject.AddComponent<Image>();

        Outline outline = tooltipObject.AddComponent<Outline>();

        RectTransform tooltipRect = tooltipObject.GetComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(300, 50);
        tooltipRect.pivot = new Vector2(0, 1);

        GameObject textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(tooltipObject.transform, false);

        tooltipText = textObj.AddComponent<TextMeshProUGUI>();
        tooltipText.text = "Tooltip Text";
        tooltipText.overflowMode = TextOverflowModes.Overflow;
        tooltipText.enableWordWrapping = true;

        RectTransform textRect = tooltipText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;

        ContentSizeFitter sizeFitter = tooltipObject.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        tooltipObject.SetActive(false);

        Debug.Log("✅ Simple Tooltip UI created");
    }

    // Context menu for testing
    [ContextMenu("Apply Settings Now")]
    public void ApplySettingsNow()
    {
        ApplySettings();
    }

    [ContextMenu("Test Show Tooltip")]
    public void TestShowTooltip()
    {
        ShowTooltip("This is a test tooltip with longer text to see how it wraps and formats properly in the UI system!", Input.mousePosition);
    }

    // Update settings in real-time
    private void OnValidate()
    {
        if (Application.isPlaying && tooltipSettings != null)
        {
            ApplySettings();
        }
    }
}
