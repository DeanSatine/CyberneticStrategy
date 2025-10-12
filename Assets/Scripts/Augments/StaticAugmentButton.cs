// Updated /Assets/Scripts/Augments/StaticAugmentButton.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class StaticAugmentButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Augment Configuration")]
    public AugmentType augmentType;
    public string augmentId;

    [Header("Visual Elements")]
    public Button button;
    public GameObject panelBackground;
    public CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.3f;
    public float hoverScale = 1.1f;
    public float hoverDuration = 0.2f;
    public AnimationCurve hoverCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 originalScale;
    private Coroutine hoverCoroutine;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        // Store original scale
        originalScale = transform.localScale;

        // Start invisible for fade-in effect
        canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        // Fade in when the button appears
        FadeIn();
    }

    public void FadeIn()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeCoroutine(0f, 1f, fadeInDuration));
    }

    public void FadeOut()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeCoroutine(canvasGroup.alpha, 0f, fadeOutDuration));
    }

    private IEnumerator FadeCoroutine(float startAlpha, float targetAlpha, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time
            float t = elapsed / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Scale up on hover
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
        hoverCoroutine = StartCoroutine(ScaleCoroutine(transform.localScale, originalScale * hoverScale, hoverDuration));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Scale back to normal
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
        hoverCoroutine = StartCoroutine(ScaleCoroutine(transform.localScale, originalScale, hoverDuration));
    }

    private IEnumerator ScaleCoroutine(Vector3 startScale, Vector3 targetScale, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time
            float t = elapsed / duration;
            float easedT = hoverCurve.Evaluate(t);
            transform.localScale = Vector3.Lerp(startScale, targetScale, easedT);
            yield return null;
        }

        transform.localScale = targetScale;
    }

    private void OnButtonClicked()
    {
        Debug.Log($"🎯 Player selected augment: {augmentId}");

        // Check if we can still select augments
        if (AugmentManager.Instance.GetActiveAugments().Count >= 3)
        {
            Debug.LogWarning("⚠️ Maximum augments already selected!");
            return;
        }

        // Play click animation
        StartCoroutine(ClickAnimation());

        // Apply the specific augment based on ID
        ApplyAugment();
    }

    private IEnumerator ClickAnimation()
    {
        // Quick shrink and grow back
        Vector3 clickScale = originalScale * 0.9f;

        // Shrink
        yield return StartCoroutine(ScaleCoroutine(transform.localScale, clickScale, 0.1f));

        // Grow back
        yield return StartCoroutine(ScaleCoroutine(transform.localScale, originalScale, 0.1f));
    }

    private void ApplyAugment()
    {
        if (AugmentManager.Instance == null) return;

        BaseAugment augmentToApply = AugmentManager.Instance.CreateAugmentFromId(augmentId);

        if (augmentToApply != null)
        {
            AugmentManager.Instance.SelectAugment(augmentToApply);
            Debug.Log($"✅ Applied augment: {augmentToApply.augmentName}");
        }
    }
}
