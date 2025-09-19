using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class MenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Name of your game scene to load when pressing Play.")]
    public string gameSceneName = "GameScene";

    [Header("UI References")]
    public GameObject creditsCanvas;

    [Header("🎨 Button Animation Settings")]
    [Tooltip("Buttons that will have hover animations (auto-detected if empty)")]
    public List<Button> animatedButtons = new List<Button>();

    [Space(10)]
    [Range(1.05f, 1.3f)]
    public float hoverScale = 1.15f; // How much bigger on hover

    [Range(0.1f, 0.5f)]
    public float animationDuration = 0.2f; // How fast the animation is

    [Range(0f, 30f)]
    public float bounceAmount = 10f; // Subtle bounce effect

    [Range(0.5f, 2f)]
    public float punchStrength = 1.2f; // Initial pop when hovering

    // ✅ Store original scales for each button
    private Dictionary<Button, Vector3> originalScales = new Dictionary<Button, Vector3>();
    private Dictionary<Button, bool> buttonStates = new Dictionary<Button, bool>(); // true = hovered

    private void Start()
    {
        if (creditsCanvas != null)
            creditsCanvas.SetActive(false);

        // ✅ Auto-detect buttons if none assigned
        if (animatedButtons.Count == 0)
        {
            AutoDetectButtons();
        }

        // ✅ Setup hover animations for all buttons
        SetupButtonAnimations();
    }

    private void Update()
    {
        // Close credits with Escape key
        if (creditsCanvas != null && creditsCanvas.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            HideCredits();
        }
    }

    // ✅ Auto-detect all buttons in the scene
    private void AutoDetectButtons()
    {
        Button[] foundButtons = FindObjectsOfType<Button>();

        foreach (Button button in foundButtons)
        {
            // Skip buttons that are inside inactive GameObjects
            if (button.gameObject.activeInHierarchy)
            {
                animatedButtons.Add(button);
                Debug.Log($"🎯 Auto-detected button for animation: {button.name}");
            }
        }

        Debug.Log($"✅ Found {animatedButtons.Count} buttons for hover animations");
    }

    // ✅ Setup hover animations for all buttons
    private void SetupButtonAnimations()
    {
        foreach (Button button in animatedButtons)
        {
            if (button == null) continue;

            // Store original scale
            originalScales[button] = button.transform.localScale;
            buttonStates[button] = false;

            // ✅ Add hover detection components
            MenuButtonHover hoverComponent = button.gameObject.GetComponent<MenuButtonHover>();
            if (hoverComponent == null)
            {
                hoverComponent = button.gameObject.AddComponent<MenuButtonHover>();
            }

            // ✅ Connect the hover events
            hoverComponent.Initialize(this, button);

            Debug.Log($"✅ Setup hover animation for button: {button.name}");
        }
    }

    // ✅ Called when mouse enters button
    public void OnButtonHover(Button button)
    {
        if (buttonStates.ContainsKey(button) && !buttonStates[button])
        {
            buttonStates[button] = true;
            StartCoroutine(AnimateButtonScale(button, hoverScale, true));
            Debug.Log($"🖱️ Hovering over button: {button.name}");
        }
    }

    // ✅ Called when mouse exits button
    public void OnButtonExit(Button button)
    {
        if (buttonStates.ContainsKey(button) && buttonStates[button])
        {
            buttonStates[button] = false;
            Vector3 originalScale = originalScales.ContainsKey(button) ? originalScales[button] : Vector3.one;
            StartCoroutine(AnimateButtonScale(button, originalScale.x, false));
            Debug.Log($"🖱️ Stopped hovering over button: {button.name}");
        }
    }

    // ✅ Smooth scale animation with bounce effect
    private IEnumerator AnimateButtonScale(Button button, float targetScale, bool isHovering)
    {
        if (button == null) yield break;

        Vector3 originalScale = originalScales.ContainsKey(button) ? originalScales[button] : Vector3.one;
        Vector3 startScale = button.transform.localScale;
        Vector3 endScale = originalScale * targetScale;

        float elapsedTime = 0f;

        // ✅ Add punch effect when starting to hover
        if (isHovering)
        {
            Vector3 punchScale = originalScale * (targetScale * punchStrength);
            button.transform.localScale = punchScale;
            yield return new WaitForSeconds(0.05f); // Quick punch
        }

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / animationDuration;

            // ✅ Smooth easing with slight bounce
            float easedProgress;
            if (isHovering)
            {
                // Ease out back for hover (slight overshoot)
                easedProgress = EaseOutBack(progress);
            }
            else
            {
                // Ease out for exit (smooth return)
                easedProgress = EaseOutQuart(progress);
            }

            Vector3 currentScale = Vector3.Lerp(startScale, endScale, easedProgress);

            if (button != null)
                button.transform.localScale = currentScale;

            yield return null;
        }

        // ✅ Ensure we reach the exact target
        if (button != null)
            button.transform.localScale = endScale;
    }

    // ✅ Easing functions for smooth animations
    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private float EaseOutQuart(float t)
    {
        return 1f - Mathf.Pow(1f - t, 4f);
    }

    // ===== ORIGINAL MENU FUNCTIONS =====

    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void LoadTestScene()
    {
        SceneManager.LoadScene("Test");
    }

    public void ShowCredits()
    {
        if (creditsCanvas != null)
            creditsCanvas.SetActive(true);
    }

    public void HideCredits()
    {
        if (creditsCanvas != null)
            creditsCanvas.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit game!");
    }
}

// ✅ Helper component for detecting hover events
public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private MenuManager menuManager;
    private Button button;

    public void Initialize(MenuManager manager, Button btn)
    {
        menuManager = manager;
        button = btn;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (menuManager != null && button != null)
        {
            menuManager.OnButtonHover(button);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (menuManager != null && button != null)
        {
            menuManager.OnButtonExit(button);
        }
    }
}
