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
    public Image tooltipIcon; 

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

    public static void ShowTooltip(string text, Sprite icon = null, Vector2 position = default)
    {
        if (instance != null && !string.IsNullOrEmpty(text))
        {
            string formattedText = instance.FormatTooltipText(text);
            instance.Show(formattedText, icon, position);
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

    private void Show(string text, Sprite icon, Vector2 position)
    {
        // Apply current settings before showing
        ApplySettings();

        if (tooltipText != null)
        {
            tooltipText.text = text;
        }

        // Handle icon display
        if (tooltipIcon != null)
        {
            if (icon != null)
            {
                tooltipIcon.sprite = icon;
                tooltipIcon.gameObject.SetActive(true);
                tooltipIcon.color = Color.white;
            }
            else
            {
                tooltipIcon.gameObject.SetActive(false);
            }
        }

        if (tooltipObject != null)
        {
            tooltipObject.SetActive(true);
        }

        // Use fixed position or passed position based on settings
        if (tooltipSettings != null && tooltipSettings.useFixedPosition)
        {
            StartCoroutine(PositionTooltipNextFrame(Vector2.zero)); // Position ignored for fixed
        }
        else
        {
            StartCoroutine(PositionTooltipNextFrame(position));
        }

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

        if (tooltipSettings.useFixedPosition)
        {
            // FIXED: Use anchored position instead of local position for true fixed positioning
            SetFixedAnchoredPosition();
            Debug.Log($"🔍 Using fixed tooltip anchored position: {tooltipRect.anchoredPosition}");
        }
        else
        {
            // Mouse following mode (existing logic)
            Vector2 localPoint;
            RectTransform canvasRect = tooltipCanvas.transform as RectTransform;
            bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPosition, tooltipCanvas.worldCamera, out localPoint);

            if (!success)
            {
                Debug.LogWarning("⚠️ Failed to convert screen position to canvas coordinates");
                return;
            }

            localPoint += tooltipSettings.offsetFromCursor;

            // Clamp to canvas bounds
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
    }

    /// <summary>
    /// Sets the tooltip to a fixed position using anchoring for consistent screen placement
    /// </summary>
    private void SetFixedAnchoredPosition()
    {
        if (tooltipRect == null || tooltipSettings == null) return;

        if (tooltipSettings.usePercentagePositioning)
        {
            // PERCENTAGE MODE: Always consistent across screen sizes
            Vector2 anchorPos = new Vector2(
                tooltipSettings.screenPercentageX,
                tooltipSettings.screenPercentageY
            );

            tooltipRect.anchorMin = anchorPos;
            tooltipRect.anchorMax = anchorPos;
            tooltipRect.anchoredPosition = Vector2.zero;
            tooltipRect.pivot = new Vector2(0f, 1f); // Top-left pivot

            Debug.Log($"🔧 Set tooltip to percentage: {anchorPos}");
        }
        else
        {
            // LEGACY COORDINATE MODE: Convert coordinates to anchor-based positioning
            Vector2 fixedPos = tooltipSettings.fixedPosition;

            Vector2 anchorMin, anchorMax, anchoredPosition;

            if (fixedPos.x < 0) // Left side
            {
                if (fixedPos.y > 0) // Top-left
                {
                    anchorMin = anchorMax = new Vector2(0f, 1f);
                    anchoredPosition = new Vector2(Mathf.Abs(fixedPos.x), -fixedPos.y);
                }
                else // Bottom-left  
                {
                    anchorMin = anchorMax = new Vector2(0f, 0f);
                    anchoredPosition = new Vector2(Mathf.Abs(fixedPos.x), Mathf.Abs(fixedPos.y));
                }
            }
            else // Right side
            {
                if (fixedPos.y > 0) // Top-right
                {
                    anchorMin = anchorMax = new Vector2(1f, 1f);
                    anchoredPosition = new Vector2(-fixedPos.x, -fixedPos.y);
                }
                else // Bottom-right
                {
                    anchorMin = anchorMax = new Vector2(1f, 0f);
                    anchoredPosition = new Vector2(-fixedPos.x, Mathf.Abs(fixedPos.y));
                }
            }

            tooltipRect.anchorMin = anchorMin;
            tooltipRect.anchorMax = anchorMax;
            tooltipRect.anchoredPosition = anchoredPosition;
            tooltipRect.pivot = anchorMin;

            Debug.Log($"🔧 Set tooltip anchor to: {anchorMin} with position: {anchoredPosition}");
        }
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

        // Create icon container
        GameObject iconObj = new GameObject("TooltipIcon");
        iconObj.transform.SetParent(tooltipObject.transform, false);

        tooltipIcon = iconObj.AddComponent<Image>();
        tooltipIcon.preserveAspect = true;

        RectTransform iconRect = tooltipIcon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 1);
        iconRect.anchorMax = new Vector2(0, 1);
        iconRect.anchoredPosition = new Vector2(15, -15); // Top-left with padding
        iconRect.sizeDelta = new Vector2(32, 32); // Icon size

        // Create text container
        GameObject textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(tooltipObject.transform, false);

        tooltipText = textObj.AddComponent<TextMeshProUGUI>();
        tooltipText.text = "Tooltip Text";
        tooltipText.overflowMode = TextOverflowModes.Overflow;
        tooltipText.enableWordWrapping = true;

        RectTransform textRect = tooltipText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        // Add left margin for icon space
        textRect.offsetMin = new Vector2(55, 10); // Left margin for icon + padding
        textRect.offsetMax = new Vector2(-10, -10); // Right and vertical padding

        ContentSizeFitter sizeFitter = tooltipObject.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        tooltipObject.SetActive(false);

        Debug.Log("✅ Simple Tooltip UI with icon support created");
    }

    // Context menu for testing
    [ContextMenu("Apply Settings Now")]
    public void ApplySettingsNow()
    {
        ApplySettings();
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
