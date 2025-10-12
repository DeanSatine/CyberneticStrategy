using UnityEngine;

public class CameraZoomController : MonoBehaviour
{
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 10f;
    [SerializeField] private float zoomSmoothTime = 0.2f;

    [Header("Camera Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float returnSmoothTime = 1f;

    private Camera cam;
    private Vector3 originalPosition;
    private float originalZoom;
    private float targetZoom;
    private Vector3 targetPosition;

    // For smooth transitions
    private float zoomVelocity;
    private Vector3 positionVelocity;

    // Zoom state tracking
    private bool isZoomedIn = false;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("❌ No camera found! Please attach this script to a camera or ensure Camera.main exists.");
                enabled = false;
                return;
            }
        }
    }

    private void Start()
    {
        // Store the original camera state
        originalPosition = transform.position;

        if (cam.orthographic)
        {
            originalZoom = cam.orthographicSize;
            targetZoom = originalZoom;
        }
        else
        {
            originalZoom = cam.fieldOfView;
            targetZoom = originalZoom;
        }

        targetPosition = originalPosition;

        Debug.Log($"🎥 Camera zoom controller initialized - Original position: {originalPosition}, Original zoom: {originalZoom}");
    }

    private void Update()
    {
        HandleZoomInput();
        UpdateCameraTransition();
    }

    private void HandleZoomInput()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scrollInput) < 0.01f) return;

        if (scrollInput > 0) // Zoom in
        {
            ZoomInToCursor();
        }
        else if (scrollInput < 0) // Zoom out
        {
            ZoomOutToOriginal();
        }
    }

    private void ZoomInToCursor()
    {
        // Get the world position under the cursor
        Vector3 cursorWorldPos = GetCursorWorldPosition();

        if (cursorWorldPos != Vector3.zero)
        {
            // Calculate zoom factor
            float zoomFactor = cam.orthographic ?
                Mathf.Max(minZoom, targetZoom - zoomSpeed) / targetZoom :
                Mathf.Max(minZoom, targetZoom - zoomSpeed * 5f) / targetZoom;

            // Move camera towards cursor position
            Vector3 directionToCursor = cursorWorldPos - transform.position;
            Vector3 newPosition = transform.position + (directionToCursor * (1f - zoomFactor) * 0.5f);

            // Update targets
            targetPosition = newPosition;

            if (cam.orthographic)
            {
                targetZoom = Mathf.Max(minZoom, targetZoom - zoomSpeed);
            }
            else
            {
                targetZoom = Mathf.Max(minZoom, targetZoom - zoomSpeed * 5f);
            }

            isZoomedIn = true;

            Debug.Log($"🔍 Zooming in to cursor at {cursorWorldPos} - New zoom: {targetZoom:F2}");
        }
    }

    private void ZoomOutToOriginal()
    {
        // Always return to original position and zoom when zooming out
        targetPosition = originalPosition;
        targetZoom = originalZoom;
        isZoomedIn = false;

        Debug.Log($"🔍 Zooming out to original state - Target zoom: {targetZoom:F2}");
    }

    private Vector3 GetCursorWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;

        // Add the camera's current Z position to maintain proper depth
        mouseScreenPos.z = Mathf.Abs(transform.position.z);

        Vector3 worldPos = cam.ScreenToWorldPoint(mouseScreenPos);

        // For orthographic cameras, we want to keep the same Y level as the camera
        if (cam.orthographic)
        {
            worldPos.y = transform.position.y;
        }

        return worldPos;
    }

    private void UpdateCameraTransition()
    {
        // Smooth zoom transition
        if (cam.orthographic)
        {
            float newSize = Mathf.SmoothDamp(cam.orthographicSize, targetZoom, ref zoomVelocity, zoomSmoothTime);
            cam.orthographicSize = newSize;
        }
        else
        {
            float newFOV = Mathf.SmoothDamp(cam.fieldOfView, targetZoom, ref zoomVelocity, zoomSmoothTime);
            cam.fieldOfView = newFOV;
        }

        // Smooth position transition
        float smoothTime = isZoomedIn ? zoomSmoothTime : returnSmoothTime;
        Vector3 newPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref positionVelocity, smoothTime);
        transform.position = newPosition;
    }

    // Public methods for external control
    public void ResetToOriginalState()
    {
        targetPosition = originalPosition;
        targetZoom = originalZoom;
        isZoomedIn = false;
        Debug.Log("🎥 Camera reset to original state");
    }

    public void SetZoomLimits(float min, float max)
    {
        minZoom = min;
        maxZoom = max;
        Debug.Log($"🎥 Zoom limits updated - Min: {min}, Max: {max}");
    }

    // Debug methods
    [ContextMenu("Reset Camera")]
    public void DebugResetCamera()
    {
        ResetToOriginalState();
    }

    [ContextMenu("Debug Camera State")]
    public void DebugCameraState()
    {
        float currentZoom = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;
        Debug.Log($"🎥 Camera State:");
        Debug.Log($"   - Position: {transform.position}");
        Debug.Log($"   - Target Position: {targetPosition}");
        Debug.Log($"   - Original Position: {originalPosition}");
        Debug.Log($"   - Current Zoom: {currentZoom:F2}");
        Debug.Log($"   - Target Zoom: {targetZoom:F2}");
        Debug.Log($"   - Original Zoom: {originalZoom:F2}");
        Debug.Log($"   - Is Zoomed In: {isZoomedIn}");
        Debug.Log($"   - Camera Type: {(cam.orthographic ? "Orthographic" : "Perspective")}");
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            // Draw original position
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(originalPosition, 1f);

            // Draw target position
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, 0.5f);

            // Draw line between current and target
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }
}
