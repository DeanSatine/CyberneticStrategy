using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AugmentViewer : MonoBehaviour
{
    [Header("UI References")]
    public GameObject augmentViewerUI;
    public Transform augmentListParent;
    public GameObject compactAugmentItemPrefab;
    public TMP_Text titleText;

    [Header("Manual UI Positioning")]
    [SerializeField] private Vector3 fixedUIPosition = new Vector3(400, 300, 0);
    [SerializeField] private bool useFixedPosition = true;

    [Header("Editor Preview")]
    [SerializeField] private bool showUIInEditor = false;

    [Header("Input Settings")]
    [SerializeField] private bool leftClickAnywhereToClose = true;
    [SerializeField] private bool rightClickToToggle = true;

    private bool isUIOpen = false;
    private List<GameObject> currentAugmentItems = new List<GameObject>();

    private void Start()
    {
        // Clean up any existing tooltips first
        CleanupExistingTooltips();

        // Ensure UI is hidden at start
        if (augmentViewerUI != null)
        {
            augmentViewerUI.SetActive(false);
            isUIOpen = false;
        }

        Debug.Log("🔍 AugmentViewer initialized with clean state");
    }

    private void CleanupExistingTooltips()
    {
        // Find and clean up any existing tooltips
        SimpleTooltipSystem[] existingSystems = FindObjectsOfType<SimpleTooltipSystem>();
        if (existingSystems.Length > 1)
        {
            Debug.Log($"🗑️ Found {existingSystems.Length} SimpleTooltipSystems, cleaning up duplicates");
            for (int i = 1; i < existingSystems.Length; i++)
            {
                Destroy(existingSystems[i].gameObject);
            }
        }

        // Force hide any visible tooltips
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Tooltip") || obj.name.Contains("tooltip"))
            {
                obj.SetActive(false);
                Debug.Log($"🔒 Hidden existing tooltip: {obj.name}");
            }
        }
    }

    private void Update()
    {
        // Handle editor preview
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            HandleEditorPreview();
            return;
        }
#endif

        // Runtime input handling
        HandleInputs();
    }

    private void HandleInputs()
    {
        // Handle right click to toggle (open/close on object)
        if (rightClickToToggle && Input.GetMouseButtonDown(1))
        {
            CheckForRightClick();
        }

        // Handle left click anywhere to close when UI is open
        if (leftClickAnywhereToClose && isUIOpen && Input.GetMouseButtonDown(0))
        {
            // Check if the click was on the UI itself
            if (!IsClickOnUI())
            {
                Debug.Log("👆 Left click detected outside UI - closing AugmentViewer");
                CloseAugmentViewer();
            }
        }

        // Handle Escape key to close
        if (isUIOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAugmentViewer();
        }
    }

    /// <summary>
    /// Checks if the mouse click was on the UI panel itself
    /// </summary>
    private bool IsClickOnUI()
    {
        if (augmentViewerUI == null || !augmentViewerUI.activeSelf) return false;

        // Use Unity's built-in UI raycast system
        Vector2 mousePosition = Input.mousePosition;

        // Check if mouse is over any UI element
        UnityEngine.EventSystems.PointerEventData eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        {
            position = mousePosition
        };

        List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

        // Check if any of the raycast hits are part of our UI
        foreach (var result in results)
        {
            if (result.gameObject != null)
            {
                // Check if the hit object is a child of our augmentViewerUI
                Transform checkTransform = result.gameObject.transform;
                while (checkTransform != null)
                {
                    if (checkTransform.gameObject == augmentViewerUI)
                    {
                        Debug.Log($"👆 Click was on UI element: {result.gameObject.name}");
                        return true;
                    }
                    checkTransform = checkTransform.parent;
                }
            }
        }

        return false;
    }

#if UNITY_EDITOR
    private void HandleEditorPreview()
    {
        if (showUIInEditor && augmentViewerUI != null)
        {
            if (!augmentViewerUI.activeSelf)
            {
                augmentViewerUI.SetActive(true);
                PositionUI();
                CreateEditorPreviewItems();
            }
        }
        else if (!showUIInEditor && augmentViewerUI != null)
        {
            if (augmentViewerUI.activeSelf)
            {
                augmentViewerUI.SetActive(false);
                ClearAugmentItems();
            }
        }
    }

#if UNITY_EDITOR
    private void CreateEditorPreviewItems()
    {
        ClearAugmentItems();

        if (titleText != null)
        {
            titleText.text = "AUGMENTS (PREVIEW) - Play mode to see real data";
        }

        // Skip creating preview items to avoid compilation issues
        Debug.Log("🔍 Preview mode - play the scene to see actual augment data");
    }
#endif


#endif

    private void CheckForRightClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            if (hit.collider.gameObject == gameObject)
            {
                Debug.Log("🔍 Right-clicked on AugmentViewer!");
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

        PositionUI();
        UpdateAugmentList();

        isUIOpen = true;
        augmentViewerUI.SetActive(true);

        Debug.Log("🔍 Augment Viewer opened!");
    }

    private void CloseAugmentViewer()
    {
        if (!isUIOpen) return;

        isUIOpen = false;
        if (augmentViewerUI != null)
        {
            augmentViewerUI.SetActive(false);
        }

        // Hide any tooltips when closing
        SimpleTooltipSystem.HideTooltip();

        Debug.Log("🔍 Augment Viewer closed");
    }

    private void PositionUI()
    {
        if (augmentViewerUI == null) return;

        RectTransform uiRect = augmentViewerUI.GetComponent<RectTransform>();
        if (uiRect == null) return;

        if (useFixedPosition)
        {
            // SIMPLE FIX: Use localPosition instead of position for consistent placement
            uiRect.localPosition = new Vector3(fixedUIPosition.x - 960f, fixedUIPosition.y - 540f, 0);
            // Note: Adjust -960, -540 based on your canvas reference resolution
        }
        else
        {
            // Automatic positioning - convert world to screen, then to local canvas position
            Vector3 worldPos = transform.position;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // Convert to local canvas coordinates (adjust for your reference resolution)
            float canvasScaleFactor = GetCanvasScaleFactor();
            Vector2 localPos = new Vector2(
                (screenPos.x / canvasScaleFactor) - 960f,  // Assuming 1920x1080 reference
                (screenPos.y / canvasScaleFactor) - 540f
            );

            uiRect.localPosition = localPos + new Vector2(150f, 100f); // Apply offset
        }

        Debug.Log($"🔍 UI positioned at local: {uiRect.localPosition}");
    }

    private float GetCanvasScaleFactor()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                return Screen.width / scaler.referenceResolution.x;
            }
        }
        return 1f;
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

        if (activeAugments.Count == 0)
        {
            ShowNoAugmentsMessage();
            return;
        }

        if (titleText != null)
        {
            titleText.text = $"AUGMENTS ({activeAugments.Count})";
        }

        foreach (BaseAugment augment in activeAugments)
        {
            CreateCompactAugmentItem(augment);
        }

        Debug.Log($"🔍 Created {currentAugmentItems.Count} augment items");
    }

    private void CreateCompactAugmentItem(BaseAugment augment)
    {
        if (compactAugmentItemPrefab == null || augmentListParent == null) return;

        GameObject item = Instantiate(compactAugmentItemPrefab, augmentListParent);
        currentAugmentItems.Add(item);

        CompactAugmentItem itemScript = item.GetComponent<CompactAugmentItem>();
        if (itemScript != null)
        {
            itemScript.Setup(augment);
        }
    }

    private void ShowNoAugmentsMessage()
    {
        if (titleText != null)
        {
            titleText.text = "NO AUGMENTS";
        }
    }

    private void ClearAugmentItems()
    {
        foreach (GameObject item in currentAugmentItems)
        {
            if (item != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(item);
                else
                    Destroy(item);
#else
                Destroy(item);
#endif
            }
        }
        currentAugmentItems.Clear();
    }

    // Context menu methods for easier setup
    [ContextMenu("Set Fixed Position to Current")]
    public void SetFixedPositionToCurrent()
    {
        if (augmentViewerUI != null)
        {
            RectTransform uiRect = augmentViewerUI.GetComponent<RectTransform>();
            if (uiRect != null)
            {
                fixedUIPosition = uiRect.position;
                Debug.Log($"✅ Fixed position set to: {fixedUIPosition}");
            }
        }
    }

    [ContextMenu("Show UI Preview")]
    public void ShowUIPreview()
    {
        showUIInEditor = true;
    }

    [ContextMenu("Hide UI Preview")]
    public void HideUIPreview()
    {
        showUIInEditor = false;
    }

    [ContextMenu("Force Close UI")]
    public void ForceCloseUI()
    {
        CloseAugmentViewer();
    }

    [ContextMenu("Test UI Click Detection")]
    public void TestUIClickDetection()
    {
        bool clickOnUI = IsClickOnUI();
        Debug.Log($"🖱️ Current mouse position over UI: {clickOnUI}");
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            HandleEditorPreview();
        }
#endif
    }
}

