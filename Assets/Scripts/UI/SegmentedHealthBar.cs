using UnityEngine;
using UnityEngine.UI;

public class SegmentedHealthBar : MonoBehaviour
{
    [Header("References")]
    public Image healthFillImage; // Your existing HealthBarFill
    public Transform segmentDividerContainer; // Container for divider lines
    public GameObject segmentDividerPrefab; // The SegmentDivider prefab you just created

    [Header("Settings")]
    public int healthPerSegment = 150;

    [Header("Colors")]
    public Color fullHealthColor = Color.green;
    public Color mediumHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;
    public Color overhealColor = Color.cyan;

    private float currentHealth;
    private float maxHealth;

    public void Init(float maxHp)
    {
        maxHealth = maxHp;
        currentHealth = maxHp;
        CreateSegmentDividers();
        UpdateHealthDisplay(maxHp);
    }

    private void CreateSegmentDividers()
    {
        // Skip if no container or prefab is set
        if (segmentDividerContainer == null || segmentDividerPrefab == null)
        {
            Debug.Log("Segmented dividers skipped - missing container or prefab reference");
            return;
        }

        // Clear existing dividers
        foreach (Transform child in segmentDividerContainer)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        int totalSegments = Mathf.CeilToInt(maxHealth / healthPerSegment);

        // Don't create dividers if only 1 segment
        if (totalSegments <= 1) return;

        // Create divider lines between segments
        for (int i = 1; i < totalSegments; i++)
        {
            GameObject divider = Instantiate(segmentDividerPrefab, segmentDividerContainer);
            RectTransform dividerRect = divider.GetComponent<RectTransform>();

            // Position divider at segment boundary
            float xPosition = (float)i / totalSegments;

            // Set anchors to position along the width
            dividerRect.anchorMin = new Vector2(xPosition, 0);
            dividerRect.anchorMax = new Vector2(xPosition, 1);
            dividerRect.anchoredPosition = Vector2.zero;
            dividerRect.sizeDelta = new Vector2(2, 0); // 2px wide, full height
        }
    }

    public void UpdateHealth(float newCurrentHealth, float newMaxHealth = -1)
    {
        currentHealth = newCurrentHealth;

        if (newMaxHealth > 0 && newMaxHealth != maxHealth)
        {
            maxHealth = newMaxHealth;
            CreateSegmentDividers(); // Recreate dividers if max health changed
        }

        UpdateHealthDisplay(currentHealth);
    }

    private void UpdateHealthDisplay(float health)
    {
        if (healthFillImage == null) return;

        int totalSegments = Mathf.CeilToInt(maxHealth / healthPerSegment);

        // Calculate fill amount with segment snapping
        int filledSegments = Mathf.FloorToInt(health / healthPerSegment);
        float segmentProgress = (health % healthPerSegment) / healthPerSegment;

        // Snap to discrete segments (optional - remove for smooth filling)
        float fillAmount;
        if (segmentProgress > 0.05f) // Small threshold to avoid tiny slivers
        {
            fillAmount = (filledSegments + segmentProgress) / totalSegments;
        }
        else
        {
            fillAmount = (float)filledSegments / totalSegments;
        }

        healthFillImage.fillAmount = Mathf.Clamp01(fillAmount);

        // Set color based on health
        bool isOverhealing = health > maxHealth;
        healthFillImage.color = GetHealthColor(health, isOverhealing);
    }

    private Color GetHealthColor(float health, bool isOverhealing)
    {
        if (isOverhealing)
            return overhealColor;

        float healthPercentage = health / maxHealth;

        if (healthPercentage > 0.75f)
            return fullHealthColor;
        else if (healthPercentage > 0.35f)
            return mediumHealthColor;
        else
            return lowHealthColor;
    }
}
