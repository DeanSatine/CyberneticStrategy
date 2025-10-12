using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CompactAugmentPrefabCreator : MonoBehaviour
{
    [ContextMenu("Create Compact Augment Item Prefab")]
    public void CreateCompactAugmentItemPrefab()
    {
        // Create main item container
        GameObject itemPrefab = new GameObject("CompactAugmentItem");

        // Add background image
        Image bgImage = itemPrefab.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        RectTransform itemRect = itemPrefab.GetComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(280, 40);

        // Add the CompactAugmentItem script
        CompactAugmentItem itemScript = itemPrefab.AddComponent<CompactAugmentItem>();

        // Add horizontal layout
        HorizontalLayoutGroup layout = itemPrefab.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.spacing = 10;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childAlignment = TextAnchor.MiddleLeft; // This centers children vertically
        layout.childForceExpandHeight = false; // IMPORTANT: Don't force expand height

        // Create icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(itemPrefab.transform, false);
        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.color = Color.white;

        RectTransform iconRect = iconImage.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(28, 28);

        LayoutElement iconLayout = iconObj.AddComponent<LayoutElement>();
        iconLayout.minWidth = iconLayout.preferredWidth = 28;
        iconLayout.minHeight = iconLayout.preferredHeight = 28;

        // Create name text - FIXED: Better alignment settings
        GameObject nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(itemPrefab.transform, false);
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "Augment Name";
        nameText.fontSize = 14;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = Color.white;
        nameText.alignment = TextAlignmentOptions.Left; // Horizontal alignment
        nameText.verticalAlignment = VerticalAlignmentOptions.Middle; // FIX: Center vertically!

        // FIX: Set the text rect to fill the available space properly
        RectTransform nameRect = nameText.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;

        LayoutElement nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.flexibleWidth = 1;
        nameLayout.preferredHeight = 40; // FIX: Match the item height

        // Assign references to the script
        itemScript.iconImage = iconImage;
        itemScript.nameText = nameText;
        itemScript.backgroundImage = bgImage;

        Debug.Log("✅ Compact Augment Item Prefab created with proper alignment!");
    }
}
