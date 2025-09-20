using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class PauseManager : MonoBehaviour
{
    [Header("Pause Menu UI")]
    [Tooltip("The pause menu GameObject to show/hide")]
    public GameObject pauseMenuPanel;

    [Tooltip("Resume game button")]
    public Button resumeButton;

    [Tooltip("Back to menu button")]
    public Button backToMenuButton;

    [Header("Settings")]
    [Tooltip("Name of the main menu scene")]
    public string mainMenuSceneName = "MainMenu";

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

    private bool isPaused = false;

    private void Start()
    {
        // Setup button listeners
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(BackToMenu);

        // Make sure pause menu starts hidden
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

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
        // Check for Escape key press
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    // ✅ Auto-detect all buttons in pause menu
    private void AutoDetectButtons()
    {
        if (pauseMenuPanel != null)
        {
            Button[] foundButtons = pauseMenuPanel.GetComponentsInChildren<Button>();

            foreach (Button button in foundButtons)
            {
                animatedButtons.Add(button);
                Debug.Log($"🎯 Auto-detected pause menu button for animation: {button.name}");
            }
        }

        Debug.Log($"✅ Found {animatedButtons.Count} pause menu buttons for hover animations");
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
            PauseMenuButtonHover hoverComponent = button.gameObject.GetComponent<PauseMenuButtonHover>();
            if (hoverComponent == null)
            {
                hoverComponent = button.gameObject.AddComponent<PauseMenuButtonHover>();
            }

            // ✅ Connect the hover events
            hoverComponent.Initialize(this, button);

            Debug.Log($"✅ Setup hover animation for pause button: {button.name}");
        }
    }

    // ✅ Called when mouse enters button
    public void OnButtonHover(Button button)
    {
        if (buttonStates.ContainsKey(button) && !buttonStates[button])
        {
            buttonStates[button] = true;
            StartCoroutine(AnimateButtonScale(button, hoverScale, true));
            Debug.Log($"🖱️ Hovering over pause button: {button.name}");
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
            Debug.Log($"🖱️ Stopped hovering over pause button: {button.name}");
        }
    }

    // ✅ Smooth scale animation with bounce effect (same as MenuManager)
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
            yield return new WaitForSecondsRealtime(0.05f); // Use realtime for pause menu
        }

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime; // Use unscaled time for pause menu
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

    // ✅ Easing functions for smooth animations (same as MenuManager)
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

    // ===== PAUSE MENU FUNCTIONS =====

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        Debug.Log("⏸️ Game Paused");
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        Debug.Log("▶️ Game Resumed");
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;

        if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(0);
        }

        Debug.Log("🔙 Returning to Main Menu");
    }

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }
}

// ✅ Helper component for detecting hover events (same as MenuManager)
public class PauseMenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private PauseManager pauseManager;
    private Button button;

    public void Initialize(PauseManager manager, Button btn)
    {
        pauseManager = manager;
        button = btn;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (pauseManager != null && button != null)
        {
            pauseManager.OnButtonHover(button);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (pauseManager != null && button != null)
        {
            pauseManager.OnButtonExit(button);
        }
    }
}
