using UnityEngine;
using UnityEngine.UI;

public class SegmentedHealthBar : MonoBehaviour
{
    [Header("References")]
    public Image healthFillImage;
    public Image shieldFillImage;
    public Transform segmentDividerContainer;
    public GameObject segmentDividerPrefab;

    [Header("Settings")]
    public int healthPerSegment = 150;

    [Header("Colors")]
    public Color fullHealthColor = Color.green;
    public Color mediumHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;
    public Color overhealColor = Color.cyan;
    public Color shieldColor = new Color(0.3f, 0.7f, 1f, 0.8f);

    private float currentHealth;
    private float maxHealth;
    private float currentShield;

    public void Init(float maxHp)
    {
        maxHealth = maxHp;
        currentHealth = maxHp;
        currentShield = 0f;
        CreateSegmentDividers();
        UpdateHealthDisplay(maxHp, 0f);
    }

    private void CreateSegmentDividers()
    {
        if (segmentDividerContainer == null || segmentDividerPrefab == null)
        {
            Debug.Log("Segmented dividers skipped - missing container or prefab reference");
            return;
        }

        foreach (Transform child in segmentDividerContainer)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        int totalSegments = Mathf.CeilToInt(maxHealth / healthPerSegment);

        if (totalSegments <= 1) return;

        for (int i = 1; i < totalSegments; i++)
        {
            GameObject divider = Instantiate(segmentDividerPrefab, segmentDividerContainer);
            RectTransform dividerRect = divider.GetComponent<RectTransform>();

            float xPosition = (float)i / totalSegments;

            dividerRect.anchorMin = new Vector2(xPosition, 0);
            dividerRect.anchorMax = new Vector2(xPosition, 1);
            dividerRect.anchoredPosition = Vector2.zero;
            dividerRect.sizeDelta = new Vector2(2, 0);
        }
    }

    public void UpdateHealth(float newCurrentHealth, float newMaxHealth = -1)
    {
        currentHealth = newCurrentHealth;

        if (newMaxHealth > 0 && newMaxHealth != maxHealth)
        {
            maxHealth = newMaxHealth;
            CreateSegmentDividers();
        }

        UpdateHealthDisplay(currentHealth, currentShield);
    }

    public void UpdateShield(float shieldAmount)
    {
        currentShield = shieldAmount;
        UpdateHealthDisplay(currentHealth, currentShield);
    }

    private void UpdateHealthDisplay(float health, float shield)
    {
        if (healthFillImage == null) return;

        float healthFillAmount = Mathf.Clamp01(health / maxHealth);
        healthFillImage.fillAmount = healthFillAmount;

        bool isOverhealing = health > maxHealth;
        healthFillImage.color = GetHealthColor(health, isOverhealing);

        if (shieldFillImage != null)
        {
            if (shield > 0)
            {
                float totalHealthWithShield = health + shield;
                float shieldFillAmount = Mathf.Clamp01(totalHealthWithShield / maxHealth);

                shieldFillImage.fillAmount = shieldFillAmount;
                shieldFillImage.color = shieldColor;
                shieldFillImage.gameObject.SetActive(true);
            }
            else
            {
                shieldFillImage.gameObject.SetActive(false);
            }
        }

        if (maxHealth > 800)
        {
            Debug.Log($"🔧 High HP Unit: {health:F0}/{maxHealth:F0} (Shield: {shield:F0}) = {healthFillAmount:F3} fill amount");
        }
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
