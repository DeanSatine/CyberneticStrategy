// Updated /Assets/Scripts/UI/StaticAugmentButton.cs
using UnityEngine;
using UnityEngine.UI;

public class StaticAugmentButton : MonoBehaviour
{
    [Header("Augment Configuration")]
    public AugmentType augmentType;
    public string augmentId;

    [Header("Visual Elements")]
    public Button button;
    public GameObject panelBackground;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
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

        // Apply the specific augment based on ID
        ApplyAugment();
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
