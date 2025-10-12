#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
public class PreviewAugment : BaseAugment
{
    public PreviewAugment()
    {
        augmentName = "Its Clobbering Time!";
        // Use scene data for preview too
        description = SceneAugmentData.GetAugmentDescriptionFromScene("Its Clobbering Time!") ?? "Preview: Clobbertrons jump to their target...";
        type = AugmentType.Class;
        augmentColor = Color.blue;
        icon = SceneAugmentData.GetAugmentIconFromScene("Its Clobbering Time!");
    }

    public override void ApplyAugment() { }
    public override void OnCombatStart() { }
    public override void OnCombatEnd() { }
    public override void OnUnitSpawned(UnitAI unit) { }
}
#endif
