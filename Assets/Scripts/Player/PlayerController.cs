using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPun
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float stoppingDistance = 0.1f;

    [Header("Animation (Optional)")]
    public Animator animator;

    [Header("Ground Detection")]
    public LayerMask groundLayer = ~0;

    private Vector3 targetPosition;
    private bool isMoving = false;
    private CharacterController characterController;
    private Camera mainCamera;

    private static readonly int SpeedParam = Animator.StringToHash("Speed");

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.radius = 0.3f;
            characterController.height = 1f;
            characterController.center = new Vector3(0, 0.5f, 0);
        }

        // ✅ Find main camera, but don't error if it's not ready yet
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("⚠️ Camera.main not found in Awake, will search in Update");
        }

        targetPosition = transform.position;
    }

    private void Update()
    {
        // ✅ Only control your own player
        if (!photonView.IsMine)
            return;

        // ✅ Try to find camera if we don't have one yet
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // Find first enabled camera
                Camera[] cameras = FindObjectsOfType<Camera>();
                foreach (Camera cam in cameras)
                {
                    if (cam.enabled)
                    {
                        mainCamera = cam;
                        Debug.Log($"✅ Found enabled camera: {cam.name}");
                        break;
                    }
                }
            }

            if (mainCamera == null)
                return; // Still no camera, skip this frame
        }

        HandleRightClick();
        MoveToTarget();
        UpdateAnimation();
    }

    private void HandleRightClick()
    {
        if (mainCamera == null)
            return;

        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 1000f))
            {
                if (hit.collider.GetComponent<AugmentViewer>() != null ||
                    hit.collider.GetComponent<Draggable>() != null)
                {
                    return;
                }

                if (((1 << hit.collider.gameObject.layer) & groundLayer) != 0)
                {
                    targetPosition = hit.point;
                    targetPosition.y = transform.position.y;
                    isMoving = true;
                }
            }
        }
    }

    private void MoveToTarget()
    {
        if (!isMoving) return;

        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        if (distanceToTarget <= stoppingDistance)
        {
            isMoving = false;
            return;
        }

        Vector3 direction = (targetPosition - transform.position).normalized;
        characterController.Move(direction * moveSpeed * Time.deltaTime);

        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void UpdateAnimation()
    {
        if (animator != null)
        {
            float speed = isMoving ? moveSpeed : 0f;
            animator.SetFloat(SpeedParam, speed);
        }
    }
}
