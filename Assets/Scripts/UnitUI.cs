using UnityEngine;
using UnityEngine.UI;

public class UnitUI : MonoBehaviour
{
    [Header("Bars")]
    public Image healthFill;
    public Image manaFill;

    private Transform target; // unit to follow
    private float maxHealth;
    private float maxMana;
    private Vector3 offset = new Vector3(0, 2f, 0); // adjustable in inspector

    public void Init(Transform followTarget, float maxHp, float maxMp)
    {
        target = followTarget;
        maxHealth = maxHp;
        maxMana = maxMp;

        UpdateHealth(maxHp);
        UpdateMana(0f);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // follow above unit’s head
        transform.position = target.position + offset;

        // face the camera
        if (Camera.main != null)
            transform.rotation = Camera.main.transform.rotation;
    }
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
    }
    public void UpdateHealth(float currentHealth)
    {
        if (healthFill != null)
            healthFill.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
    }

    public void UpdateMana(float currentMana)
    {
        if (manaFill != null)
            manaFill.fillAmount = Mathf.Clamp01(currentMana / maxMana);
    }
}
