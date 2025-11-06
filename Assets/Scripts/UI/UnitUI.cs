using UnityEngine;
using UnityEngine.UI;

public class UnitUI : MonoBehaviour
{
    [Header("Bars")]
    public Image healthFill;
    public Image manaFill;

    [Header("Segmented Health Bar")]
    public SegmentedHealthBar segmentedHealthBar;
    public bool useSegmentedHealthBar = true;

    private Transform target;
    private float maxHealth;
    private float maxMana;
    private Vector3 offset = new Vector3(0, 2f, 0);

    public void Init(Transform followTarget, float maxHp, float maxMp)
    {
        target = followTarget;
        maxHealth = maxHp;
        maxMana = maxMp;

        if (useSegmentedHealthBar && segmentedHealthBar != null)
        {
            segmentedHealthBar.Init(maxHp);
        }
        else
        {
            UpdateHealth(maxHp);
        }

        UpdateMana(0f);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 adjustedOffset = offset;
        UnitAI unitAI = target.GetComponent<UnitAI>();

        if (unitAI != null)
        {
            if (unitAI.starLevel == 2)
            {
                adjustedOffset.y += 0.4f;
            }
            else if (unitAI.starLevel == 3)
            {
                adjustedOffset.y += 0.8f;
            }

            ShieldComponent shield = unitAI.GetComponent<ShieldComponent>();
            if (shield != null && useSegmentedHealthBar && segmentedHealthBar != null)
            {
                segmentedHealthBar.UpdateShield(shield.CurrentShield);
            }
        }

        transform.position = target.position + adjustedOffset;

        if (Camera.main != null)
            transform.rotation = Camera.main.transform.rotation;
    }

    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;

        if (useSegmentedHealthBar && segmentedHealthBar != null)
        {
            segmentedHealthBar.UpdateHealth(target.GetComponent<UnitAI>().currentHealth, newMaxHealth);
        }
    }

    public void UpdateHealth(float currentHealth)
    {
        if (useSegmentedHealthBar && segmentedHealthBar != null)
        {
            segmentedHealthBar.UpdateHealth(currentHealth, maxHealth);
        }
        else if (healthFill != null)
        {
            float healthPercentage = currentHealth / maxHealth;

            if (currentHealth > maxHealth)
            {
                healthFill.fillAmount = 1f;
                healthFill.color = Color.yellow;
            }
            else
            {
                healthFill.fillAmount = Mathf.Clamp01(healthPercentage);
                healthFill.color = Color.green;
            }
        }
    }

    public void UpdateMana(float currentMana)
    {
        if (manaFill != null)
            manaFill.fillAmount = Mathf.Clamp01(currentMana / maxMana);
    }
}
