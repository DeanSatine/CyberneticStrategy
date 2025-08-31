using UnityEngine;
using UnityEngine.UI;

public class UnitUI : MonoBehaviour
{
    public Slider healthBar;
    public Slider manaBar;

    private Transform target;
    private Camera cam;

    [Header("Offset")]
    public Vector3 offset = new Vector3(0, 2.5f, 0); // height above unit

    [Header("Scaling")]
    public float baseScale = 0.01f;   // adjust to taste
    public float scaleFactor = 0.02f; // how much size adjusts by distance

    public void Init(Transform followTarget, float maxHealth, float maxMana)
    {
        target = followTarget;
        cam = Camera.main;

        healthBar.maxValue = maxHealth;
        healthBar.value = maxHealth;

        manaBar.maxValue = maxMana;
        manaBar.value = 0;
    }

    private void LateUpdate()
    {
        if (target == null || cam == null) return;

        // Position above unit
        transform.position = target.position + offset;

        // Always face camera
        transform.rotation = cam.transform.rotation;

        // Auto-scale by distance (so UI stays readable)
        float distance = Vector3.Distance(cam.transform.position, target.position);
        transform.localScale = Vector3.one * (baseScale + distance * scaleFactor);
    }

    public void UpdateHealth(float current)
    {
        healthBar.value = current;
    }

    public void UpdateMana(float current)
    {
        manaBar.value = current;
    }
}
