// Updated /Assets/Scripts/UI/AugmentSelectionUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AugmentSelectionUI : MonoBehaviour
{
    [Header("UI References - Scene Based")]
    [SerializeField] private GameObject augmentSelectionPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button skipButton;

    [Header("Pre-built Augment Buttons")]
    [SerializeField] private AugmentButton[] augmentButtons = new AugmentButton[3];

    [Header("Active Augments Display")]
    [SerializeField] private Transform activeAugmentsParent;
    [SerializeField] private Image[] activeAugmentIcons = new Image[3]; // Pre-built icons

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private CanvasGroup canvasGroup;

    private List<BaseAugment> currentChoices = new List<BaseAugment>();
    private int activeAugmentCount = 0;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // Setup skip button
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipPressed);
        }

        // Setup augment buttons
        for (int i = 0; i < augmentButtons.Length; i++)
        {
            if (augmentButtons[i] != null)
            {
                int buttonIndex = i; // Capture for closure
                augmentButtons[i].button.onClick.AddListener(() => OnAugmentSelected(buttonIndex));
            }
        }

        // Hide initially
        Hide();

        // Deactivate all augment buttons
        SetAugmentButtonsActive(false);
    }

    public void ShowAugmentSelection(List<BaseAugment> augmentChoices, int currentStage)
    {
        currentChoices = augmentChoices;

        // Update title
        if (titleText != null)
        {
            titleText.text = $"Choose an Augment - Stage {currentStage}";
        }

        // Configure and activate augment buttons
        ConfigureAugmentButtons();

        // Show the panel
        Show();

        Debug.Log($"🎯 Showing augment selection with {augmentChoices.Count} choices");
    }

    private void ConfigureAugmentButtons()
    {
        // First deactivate all buttons
        SetAugmentButtonsActive(false);

        // Then configure and activate buttons for available choices
        for (int i = 0; i < currentChoices.Count && i < augmentButtons.Length; i++)
        {
            if (augmentButtons[i] != null)
            {
                augmentButtons[i].ConfigureForAugment(currentChoices[i]);
                augmentButtons[i].gameObject.SetActive(true);
            }
        }
    }

    private void SetAugmentButtonsActive(bool active)
    {
        foreach (var button in augmentButtons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(active);
            }
        }
    }

    private void OnAugmentSelected(int buttonIndex)
    {
        if (buttonIndex < currentChoices.Count)
        {
            BaseAugment selectedAugment = currentChoices[buttonIndex];

            // Apply the augment through the manager
            AugmentManager.Instance.SelectAugment(selectedAugment);

            // Add to active display
            AddActiveAugmentIcon(selectedAugment);

            // Close the UI
            Hide();

            // Resume game
            ResumeGame();
        }
    }

    private void OnSkipPressed()
    {
        Debug.Log("🎯 Player skipped augment selection");
        Hide();
        ResumeGame();
    }

    public void AddActiveAugmentIcon(BaseAugment augment)
    {
        if (activeAugmentCount < activeAugmentIcons.Length && activeAugmentIcons[activeAugmentCount] != null)
        {
            activeAugmentIcons[activeAugmentCount].gameObject.SetActive(true);

            if (augment.icon != null)
            {
                activeAugmentIcons[activeAugmentCount].sprite = augment.icon;
            }

            // Set augment color
            activeAugmentIcons[activeAugmentCount].color = augment.augmentColor;

            // Add tooltip if available
            AugmentTooltip tooltip = activeAugmentIcons[activeAugmentCount].GetComponent<AugmentTooltip>();
            if (tooltip != null)
            {
                tooltip.Initialize(augment);
            }

            activeAugmentCount++;
        }
    }

    private void Show()
    {
        augmentSelectionPanel.SetActive(true);

        // Pause the game
        Time.timeScale = 0f;

        // Fade in
        StartCoroutine(FadeIn());
    }

    private void Hide()
    {
        StartCoroutine(FadeOut());
    }

    private System.Collections.IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private System.Collections.IEnumerator FadeOut()
    {
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        augmentSelectionPanel.SetActive(false);

        // Deactivate augment buttons after hiding
        SetAugmentButtonsActive(false);
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;
    }
}
