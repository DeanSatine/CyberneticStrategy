using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How fast the little legend moves")]
    public float moveSpeed = 5f;

    [Tooltip("How fast the legend rotates to face movement direction")]
    public float rotationSpeed = 10f;

    [Tooltip("How close to target before stopping")]
    public float stoppingDistance = 0.1f;

    [Header("Animation (Optional)")]
    [Tooltip("Animator component if you want walk/idle animations")]
    public Animator animator;

    [Header("Ground Detection")]
    [Tooltip("What layers count as ground (probably just Default or Stage)")]
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

        mainCamera = Camera.main;
        targetPosition = transform.position;
    }

    private void Update()
    {
        HandleRightClick();
        MoveToTarget();
        UpdateAnimation();
    }

    private void HandleRightClick()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 1000f))
            {
                Debug.Log($"[LittleLegend] Right-click detected hit: {hit.collider.gameObject.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

                if (hit.collider.GetComponent<AugmentViewer>() != null)
                {
                    Debug.Log($"[LittleLegend] Hit AugmentViewer - ignoring movement");
                    return;
                }

                if (hit.collider.GetComponent<Draggable>() != null)
                {
                    Debug.Log($"[LittleLegend] Hit Draggable unit - ignoring movement");
                    return;
                }

                if (((1 << hit.collider.gameObject.layer) & groundLayer) != 0)
                {
                    targetPosition = hit.point;
                    targetPosition.y = transform.position.y;
                    isMoving = true;
                    Debug.Log($"[LittleLegend] Moving to {targetPosition}");
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

    private void OnDrawGizmos()
    {
        if (isMoving)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetPosition, 0.3f);
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }
}
