using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AugmentViewerSetup : MonoBehaviour
{
    [ContextMenu("Create Complete Augment Viewer")]
    public void CreateCompleteAugmentViewer()
    {
        CreateAugmentViewerObject();
        CreateAugmentItemPrefab();
    }

    [ContextMenu("1. Create Augment Viewer Object")]
    public void CreateAugmentViewerObject()
    {
        // Create the 3D object
        GameObject viewerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        viewerObj.name = "AugmentViewerObject";
        viewerObj.transform.position = new Vector3(0, 1, 0);
        viewerObj.transform.localScale = new Vector3(1, 2, 1);

        // Add the viewer script
        AugmentViewer viewer = viewerObj.AddComponent<AugmentViewer>();

        // Create the UI
        CreateAugmentViewerUI(viewer);

        Debug.Log("✅ Augment Viewer Object created! Right-click it to test.");
    }

    [ContextMenu("2. Create Augment Item Prefab")]
    public void CreateAugmentItemPrefab()
    {
        // Create prefab structure
        GameObject itemPrefab = new GameObject("AugmentItemPrefab");

        // Add background image
        Image bgImage = itemPrefab.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        RectTransform itemRect = itemPrefab.GetComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(280, 60);

        // Add layout
        HorizontalLayoutGroup layout = itemPrefab.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.spacing = 10;
        layout.childControlWidth = false;
        layout.childControlHeight = true;

        // Create icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(itemPrefab.transform, false);
        Image iconImage = iconObj.AddComponent<Image>();
        RectTransform iconRect = iconImage.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(40, 40);
        LayoutElement iconLayout = iconObj.AddComponent<LayoutElement>();
        iconLayout.minWidth = iconLayout.preferredWidth = 40;

        // Create text container
        GameObject textContainer = new GameObject("TextContainer");
        textContainer.transform.SetParent(itemPrefab.transform, false);
        VerticalLayoutGroup textLayout = textContainer.AddComponent<VerticalLayoutGroup>();
        textLayout.spacing = 2;
        LayoutElement textLayoutElement = textContainer.AddComponent<LayoutElement>();
        textLayoutElement.flexibleWidth = 1;

        // Create name text
        GameObject nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(textContainer.transform, false);
        TMP_Text nameText = nameObj.AddComponent<TMP_Text>();
        nameText.text = "Augment Name";
        nameText.fontSize = 16;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = Color.white;

        // Create description text
        GameObject descObj = new GameObject("DescriptionText");
        descObj.transform.SetParent(textContainer.transform, false);
        TMP_Text descText = descObj.AddComponent<TMP_Text>();
        descText.text = "Augment description goes here...";
        descText.fontSize = 12;
        descText.color = new Color(0.8f, 0.8f, 0.8f);

        // Add viewer item script
        itemPrefab.AddComponent<AugmentViewerItem>();

        Debug.Log("✅ Augment Item Prefab created! Save it as a prefab in your Project window.");
    }

    private void CreateAugmentViewerUI(AugmentViewer viewer)
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("AugmentViewerCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Create UI Panel
        GameObject panelObj = new GameObject("AugmentPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.12f, 0.15f, 0.95f);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(300, 200);
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);

        // Create title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        TMP_Text titleText = titleObj.AddComponent<TMP_Text>();
        titleText.text = "Active Augments";
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.9f, 0.8f, 0.6f);
        titleText.alignment = TextAlignmentOptions.Center;

        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.8f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;

        // Create scroll view
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(panelObj.transform, false);
        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();

        RectTransform scrollRectTrans = scrollObj.GetComponent<RectTransform>();
        scrollRectTrans.anchorMin = new Vector2(0, 0);
        scrollRectTrans.anchorMax = new Vector2(1, 0.8f);
        scrollRectTrans.offsetMin = scrollRectTrans.offsetMax = Vector2.zero;

        // Create content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollObj.transform, false);
        VerticalLayoutGroup contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 5;
        contentLayout.padding = new RectOffset(10, 10, 10, 10);

        ContentSizeFitter contentFitter = contentObj.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);

        scrollRect.content = contentRect;
        scrollRect.vertical = true;
        scrollRect.horizontal = false;

        // Assign references to the viewer
        viewer.augmentViewerUI = panelObj;
        viewer.augmentListParent = contentObj.transform;

        // Get title text component
        viewer.titleText = titleText;

        panelObj.SetActive(false);
    }
}
