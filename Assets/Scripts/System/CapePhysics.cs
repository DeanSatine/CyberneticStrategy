using UnityEngine;

public class CapePhysics : MonoBehaviour
{
    [Header("Cape Chain Settings")]
    public Transform[] capeSegments;  // Each "bone" of the cape
    public float segmentLength = 0.2f;

    [Header("Physics Settings")]
    public float stiffness = 200f;   // How strongly each segment returns to its target
    public float damping = 10f;      // How much velocity is reduced each frame
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public float maxStretch = 0.3f;  // Limit to prevent unrealistic stretching

    private Vector3[] velocities;
    private Vector3[] prevPositions;

    void Start()
    {
        velocities = new Vector3[capeSegments.Length];
        prevPositions = new Vector3[capeSegments.Length];
        for (int i = 0; i < capeSegments.Length; i++)
            prevPositions[i] = capeSegments[i].position;
    }

    void LateUpdate()
    {
        for (int i = 1; i < capeSegments.Length; i++)
        {
            Vector3 currentPos = capeSegments[i].position;
            Vector3 targetPos = capeSegments[i - 1].position - capeSegments[i - 1].forward * segmentLength;

            // Apply spring force
            Vector3 force = (targetPos - currentPos) * stiffness;
            velocities[i] += (force + gravity) * Time.deltaTime;

            // Apply damping
            velocities[i] *= 1f - (damping * Time.deltaTime);

            // Move segment
            currentPos += velocities[i] * Time.deltaTime;

            // Stretch constraint
            float dist = Vector3.Distance(currentPos, capeSegments[i - 1].position);
            if (dist > segmentLength + maxStretch)
                currentPos = capeSegments[i - 1].position -
                    (capeSegments[i - 1].position - currentPos).normalized * (segmentLength + maxStretch);

            capeSegments[i].position = currentPos;

            // Reorient segment toward previous
            Vector3 dir = capeSegments[i - 1].position - capeSegments[i].position;
            if (dir.sqrMagnitude > 0.001f)
                capeSegments[i].rotation = Quaternion.LookRotation(dir);
        }
    }
}
