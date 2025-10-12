using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AugmentViewer : MonoBehaviour
{
    [Header("UI References")]
    public GameObject augmentViewerUI;
    public Transform augmentListParent;
    public GameObject compactAugmentItemPrefab; // New compact prefab
    public TMP_Text titleText;

    [Header("Visual Settings")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material highlightMaterial;

    private bool isUIOpen = false;
    private List<GameObject> currentAugmentItems = new List<GameObject>();
    private Renderer objectRenderer;

    private void Start()
    {
        objectRenderer = GetComponent<Renderer>();

        // Create materials if not assigned
        if (normalMaterial == null) CreateDefaultMaterials();

        // Ensure tooltip system exists
        if (TooltipSystem.Instance == null)
        {
            CreateTooltipSystem();
        }

        // Initially hide the UI
        if (augmentViewerUI != null)
        {
            augmentViewerUI.SetActive(false);
            Debug.Log("🔍 AugmentViewer UI initially hidden");
        }

        Debug.Log("🔍 Compact AugmentViewer ready for right-clicking!");
    }

    private void Update()
    {
        // Check for right-click
        if (Input.GetMouseButtonDown(1))
        {
            CheckForRightClick();
        }

        // Close with escape key
        if (isUIOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAugmentViewer();
        }
    }

    private void CheckForRightClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            if (hit.collider.gameObject == gameObject)
            {
                Debug.Log("🔍 Right-clicked on AugmentViewer object!");
                ToggleAugmentViewer();
            }
        }
    }

    private void ToggleAugmentViewer()
    {
        if (isUIOpen)
        {
            CloseAugmentViewer();
        }
        else
        {
            ShowAugmentViewer();
        }
    }

    private void ShowAugmentViewer()
    {
        if (augmentViewerUI == null)
        {
            Debug.LogError("❌ AugmentViewer UI is not assigned!");
            return;
        }

        // Position UI near the object
        PositionUIAtObject();

        // Update the augment list
        UpdateAugmentList();

        // Show the UI
        isUIOpen = true;
        augmentViewerUI.SetActive(true);

        Debug.Log("🔍 Compact Augment Viewer opened!");
    }

    private void CloseAugmentViewer()
    {
        if (!isUIOpen) return;

        isUIOpen = false;
        if (augmentViewerUI != null)
        {
            augmentViewerUI.SetActive(false);
        }

        // Hide any open tooltips
        TooltipSystem.HideTooltip();

        Debug.Log("🔍 Compact Augment Viewer closed");
    }

    private void PositionUIAtObject()
    {
        if (augmentViewerUI == null || Camera.main == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        screenPos.x += 150f;
        screenPos.y += 100f;

        screenPos.x = Mathf.Clamp(screenPos.x, 200f, Screen.width - 200f);
        screenPos.y = Mathf.Clamp(screenPos.y, 100f, Screen.height - 100f);

        RectTransform uiRect = augmentViewerUI.GetComponent<RectTransform>();
        if (uiRect != null)
        {
            uiRect.position = screenPos;
        }
    }

    private void UpdateAugmentList()
    {
        ClearAugmentItems();

        if (AugmentManager.Instance == null)
        {
            ShowNoAugmentsMessage();
            return;
        }

        List<BaseAugment> activeAugments = AugmentManager.Instance.GetActiveAugments();
        Debug.Log($"🔍 Found {activeAugments.Count} active augments");

        if (activeAugments.Count == 0)
        {
            ShowNoAugmentsMessage();
            return;
        }

        // Update title
        if (titleText != null)
        {
            titleText.text = $"Augments ({activeAugments.Count})";
        }

        // Create compact UI items for each augment
        foreach (BaseAugment augment in activeAugments)
        {
            CreateCompactAugmentItem(augment);
        }

        Debug.Log($"🔍 Created {currentAugmentItems.Count} compact augment items");
    }

    private void CreateCompactAugmentItem(BaseAugment augment)
    {
        if (compactAugmentItemPrefab == null)
        {
            Debug.LogError("❌ Compact Augment Item Prefab is not assigned!");
            return;
        }

        if (augmentListParent == null)
        {
            Debug.LogError("❌ Augment List Parent is not assigned!");
            return;
        }

        GameObject item = Instantiate(compactAugmentItemPrefab, augmentListParent);
        currentAugmentItems.Add(item);

        // Set up the compact item
        CompactAugmentItem itemScript = item.GetComponent<CompactAugmentItem>();
        if (itemScript != null)
        {
            itemScript.Setup(augment);
        }
        else
        {
            Debug.LogError("❌ CompactAugmentItem script not found on prefab!");
        }

        Debug.Log($"🔍 Created compact item for: {augment.augmentName}");
    }

    private void ShowNoAugmentsMessage()
    {
        if (titleText != null)
        {
            titleText.text = "No Augments";
        }

        Debug.Log("🔍 Showing 'No Augments' message");
    }

    private void ClearAugmentItems()
    {
        foreach (GameObject item in currentAugmentItems)
        {
            if (item != null) DestroyImmediate(item);
        }
        currentAugmentItems.Clear();
    }

    private void CreateTooltipSystem()
    {
        // Find or create tooltip canvas
        Canvas tooltipCanvas = FindObjectOfType<Canvas>();
        if (tooltipCanvas == null)
        {
            GameObject canvasObj = new GameObject("TooltipCanvas");
            tooltipCanvas = canvasObj.AddComponent<Canvas>();
            tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tooltipCanvas.sortingOrder = 9999; // Very high priority
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Add tooltip system to canvas
        GameObject tooltipSystemObj = new GameObject("TooltipSystem");
        tooltipSystemObj.transform.SetParent(tooltipCanvas.transform, false);
        tooltipSystemObj.AddComponent<TooltipSystem>();

        Debug.Log("✅ Tooltip system created");
    }

    private void CreateDefaultMaterials()
    {
        normalMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        normalMaterial.color = new Color(0.3f, 0.8f, 1f, 1f);

        highlightMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        highlightMaterial.color = new Color(0.8f, 1f, 0.3f, 1f);
        highlightMaterial.SetFloat("_Metallic", 0.8f);
        highlightMaterial.SetFloat("_Smoothness", 0.9f);

        if (objectRenderer != null)
        {
            objectRenderer.material = normalMaterial;
        }
    }

    private void OnMouseEnter()
    {
        if (objectRenderer != null && highlightMaterial != null)
        {
            objectRenderer.material = highlightMaterial;
        }
    }

    private void OnMouseExit()
    {
        if (objectRenderer != null && normalMaterial != null)
        {
            objectRenderer.material = normalMaterial;
        }
    }

    [ContextMenu("Test Show UI")]
    public void TestShowUI()
    {
        ShowAugmentViewer();
    }
}
