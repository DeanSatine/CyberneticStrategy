using UnityEngine;
using System.Collections;

public class ClobbertronJumpBehaviour : MonoBehaviour
{
    [HideInInspector] public float lowHealthThreshold = 0.1f;
    [HideInInspector] public Color augmentColor = Color.blue;

    [Header("Jump Settings")]
    public float jumpHeight = 3f;
    public float jumpDuration = 0.5f;
    public GameObject jumpVFXPrefab;

    private UnitAI unitAI;
    private bool hasLowHealthJumped = false;
    private bool isJumping = false;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
    }

    private void Update()
    {
        if (unitAI == null || !unitAI.isAlive || isJumping) return;

        // Check for low health jump (10% HP threshold)
        float healthPercent = (float)unitAI.currentHealth / unitAI.maxHealth;
        if (!hasLowHealthJumped && healthPercent <= lowHealthThreshold)
        {
            hasLowHealthJumped = true;
            JumpToTarget();
        }
    }

    public void JumpToTarget()
    {
        if (isJumping) return;

        UnitAI target = null;

        // Fix: Get UnitAI component from the currentTarget transform
        if (unitAI.currentTarget != null)
        {
            target = unitAI.currentTarget.GetComponent<UnitAI>();
        }

        // If no valid target, find closest enemy
        if (target == null || !target.isAlive)
        {
            target = FindClosestEnemy();
        }

        if (target != null)
        {
            StartCoroutine(JumpToPosition(target.transform.position));
        }
    }

    private UnitAI FindClosestEnemy()
    {
        UnitAI[] enemies = FindObjectsOfType<UnitAI>();
        UnitAI closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy.team != unitAI.team && enemy.isAlive)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }
        }

        return closestEnemy;
    }

    private IEnumerator JumpToPosition(Vector3 targetPosition)
    {
        if (isJumping) yield break;

        isJumping = true;
        Vector3 startPos = transform.position;
        Vector3 endPos = targetPosition + Vector3.forward * 1.5f; // Land slightly in front of target
        Vector3 jumpPeakPos = Vector3.Lerp(startPos, endPos, 0.5f) + Vector3.up * jumpHeight;

        // Play jump VFX at start position
        if (jumpVFXPrefab != null)
        {
            GameObject startVFX = Instantiate(jumpVFXPrefab, startPos, Quaternion.identity);
            Destroy(startVFX, 2f);
        }

        Debug.Log($"🔨 {unitAI.unitName} jumping to target!");

        // Disable unit movement during jump
        bool originalCanMove = unitAI.canMove;
        unitAI.canMove = false;

        // Jump up to peak (first half of jump)
        float halfJumpTime = jumpDuration * 0.5f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / halfJumpTime;
            Vector3 currentPos = Vector3.Lerp(startPos, jumpPeakPos, t);
            transform.position = currentPos;

            // Rotate towards target during jump
            Vector3 directionToTarget = (endPos - startPos).normalized;
            if (directionToTarget != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 3f);
            }

            yield return null;
        }

        // Jump down to target (second half of jump)
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / halfJumpTime;
            Vector3 currentPos = Vector3.Lerp(jumpPeakPos, endPos, t);
            transform.position = currentPos;
            yield return null;
        }

        transform.position = endPos;

        // Play landing VFX
        if (jumpVFXPrefab != null)
        {
            GameObject landVFX = Instantiate(jumpVFXPrefab, endPos, Quaternion.identity);

            // Make landing VFX use augment color
            ParticleSystem particles = landVFX.GetComponent<ParticleSystem>();
            if (particles != null)
            {
                var main = particles.main;
                main.startColor = augmentColor;
            }

            Destroy(landVFX, 2f);
        }

        // Do landing impact damage in small radius
        DoLandingImpact(endPos);

        // Re-enable movement
        unitAI.canMove = originalCanMove;
        isJumping = false;

        Debug.Log($"🔨 {unitAI.unitName} completed jump landing!");
    }

    private void DoLandingImpact(Vector3 impactPosition)
    {
        float impactRadius = 2f;
        float impactDamage = unitAI.attackDamage * 0.5f; // 50% of attack damage as impact damage

        Collider[] hits = Physics.OverlapSphere(impactPosition, impactRadius);
        foreach (var hit in hits)
        {
            UnitAI enemy = hit.GetComponent<UnitAI>();
            if (enemy != null && enemy.team != unitAI.team && enemy.isAlive)
            {
                enemy.TakeDamage((int)impactDamage);
                Debug.Log($"🔨 {unitAI.unitName} jump impact damaged {enemy.unitName} for {impactDamage}!");
            }
        }

        // Optional: Add camera shake for impact
        StartCoroutine(CameraShake(0.3f, 0.2f));
    }

    private IEnumerator CameraShake(float intensity, float duration)
    {
        if (Camera.main == null) yield break;

        Vector3 originalPos = Camera.main.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 offset = Random.insideUnitSphere * intensity;
            Camera.main.transform.position = originalPos + offset;
            yield return null;
        }

        Camera.main.transform.position = originalPos;
    }

    // Reset for new combat
    public void ResetJumpBehavior()
    {
        hasLowHealthJumped = false;
        isJumping = false;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw jump arc preview in editor
        if (unitAI != null && unitAI.currentTarget != null)
        {
            Gizmos.color = augmentColor;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawLine(transform.position, unitAI.currentTarget.position);
        }
    }
}
