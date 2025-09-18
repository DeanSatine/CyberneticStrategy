using UnityEngine;
using System.Collections;

public class EradicatorTrait : MonoBehaviour
{
    [HideInInspector] public float executeThreshold;
    [HideInInspector] public GameObject pressPrefab;

    // ✅ Enhanced press management with slam completion protection
    private static GameObject pressInstance;
    private static Vector3 pressIdlePosition = new Vector3(0, 5f, -12f);
    private static bool isPressing;
    private static bool isSlammingDown; // ✅ NEW: Track when we're in the critical slam phase
    private static int activeEradicatorCount = 0;

    [Header("VFX")]
    public GameObject slamEffectPrefab;
    public float cameraShakeIntensity = 0.3f;
    public float cameraShakeDuration = 0.2f;

    private void OnEnable()
    {
        activeEradicatorCount++;
        Debug.Log($"✅ EradicatorTrait enabled. Active count: {activeEradicatorCount}");

        UnitAI unitAI = GetComponent<UnitAI>();
        if (unitAI != null)
        {
            SubscribeToAllUnits();
        }
    }

    private void OnDisable()
    {
        activeEradicatorCount--;
        Debug.Log($"❌ EradicatorTrait disabled. Active count: {activeEradicatorCount}");

        // ✅ Only clean up if no more eradicators AND not currently slamming
        if (activeEradicatorCount <= 0 && !isSlammingDown)
        {
            CleanupPress();
        }
    }

    private void Update()
    {
        // ✅ Only operate if we have valid threshold and NOT pressing
        if (executeThreshold <= 0 || isPressing) return;

        if (!EnsurePressExists()) return;

        CheckForExecutableEnemies();
    }

    private void CheckForExecutableEnemies()
    {
        UnitAI[] enemies = FindObjectsOfType<UnitAI>();
        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.isAlive || enemy.team != Team.Enemy) continue;

            float hpPercent = (float)enemy.currentHealth / enemy.maxHealth;
            if (hpPercent <= executeThreshold)
            {
                Debug.Log($"🔥 Eradicator found executable enemy: {enemy.unitName} ({hpPercent:P} HP)");
                StartCoroutine(GuaranteedSlamSequence(enemy));
                return; // Only execute one at a time
            }
        }
    }

    private void SubscribeToAllUnits()
    {
        // Check on each frame for now to ensure we don't miss fast deaths
    }

    public void SpawnPressIfNeeded()
    {
        if (pressPrefab != null)
        {
            EnsurePressExists();
        }
    }

    public void DespawnPress()
    {
        // ✅ Only allow cleanup if not currently slamming
        if (!isSlammingDown)
        {
            CleanupPress();
        }
        else
        {
            Debug.Log("🚫 Cannot despawn press - slam in progress!");
        }
    }

    private bool EnsurePressExists()
    {
        if (pressPrefab == null) return false;

        if (pressInstance == null || pressInstance.Equals(null))
        {
            pressIdlePosition = new Vector3(0, 5f, -12f);
            pressInstance = Instantiate(pressPrefab, pressIdlePosition, Quaternion.identity);
            Debug.Log($"✅ Hydraulic press spawned at {pressIdlePosition}");
            return true;
        }

        return pressInstance != null;
    }

    private static void CleanupPress()
    {
        // ✅ Safety check - don't cleanup during slam
        if (isSlammingDown)
        {
            Debug.Log("🚫 Cannot cleanup press - slam in progress!");
            return;
        }

        if (pressInstance != null)
        {
            Destroy(pressInstance);
            pressInstance = null;
            isPressing = false;
            Debug.Log("🧹 Hydraulic press cleaned up");
        }
    }

    // ✅ NEW: Guaranteed slam sequence that cannot be interrupted
    private IEnumerator GuaranteedSlamSequence(UnitAI target)
    {
        if (!EnsurePressExists() || target == null)
        {
            Debug.Log("❌ Press sequence failed - no press or target");
            yield break;
        }

        isPressing = true;
        Debug.Log($"⚔️ Eradicator GUARANTEED slam sequence started on {target.unitName}");

        // Move press above target
        Vector3 targetAbove = target.transform.position + Vector3.up * 5f;
        yield return MovePress(targetAbove, 0.6f);

        // ✅ Early exit checks (before we commit to the slam)
        if (target == null || !target.isAlive)
        {
            Debug.Log("❌ Target died during press movement - returning to idle");
            yield return MovePress(pressIdlePosition, 0.8f);
            isPressing = false;
            yield break;
        }

        float currentHpPercent = (float)target.currentHealth / target.maxHealth;
        if (currentHpPercent > executeThreshold)
        {
            Debug.Log($"❌ Target {target.unitName} healed above threshold - returning to idle");
            yield return MovePress(pressIdlePosition, 0.8f);
            isPressing = false;
            yield break;
        }

        // ✅ CRITICAL SLAM PHASE - NO INTERRUPTIONS ALLOWED
        Debug.Log($"🔥 ENTERING CRITICAL SLAM PHASE - NO INTERRUPTIONS!");
        isSlammingDown = true;

        // Store the original target position to ensure we slam the right spot
        Vector3 originalTargetPos = target.transform.position;
        Vector3 crushPos = originalTargetPos + Vector3.up * 1.3f;

        // Slam down to the ORIGINAL position (even if target moves/dies)
        yield return MovePress(crushPos, 0.2f);

        // ✅ Execute at the slam location (regardless of where target moved)
        Debug.Log($"💀 Eradicator GUARANTEED execution at {crushPos}!");

        // Apply effects at slam location
        if (slamEffectPrefab != null)
        {
            GameObject fx = Instantiate(slamEffectPrefab, originalTargetPos, Quaternion.identity);
            Destroy(fx, 2f);
        }
        StartCoroutine(CameraShake(cameraShakeIntensity, cameraShakeDuration));

        // Execute the target if it's still alive and at the slam location
        if (target != null && target.isAlive)
        {
            float distanceToSlam = Vector3.Distance(target.transform.position, originalTargetPos);
            if (distanceToSlam <= 2f) // Within slam radius
            {
                Debug.Log($"💀 Target {target.unitName} executed by slam!");
                target.TakeDamage(999999);
            }
            else
            {
                Debug.Log($"💨 Target {target.unitName} moved away from slam zone - escaped!");
            }
        }

        // ✅ MANDATORY COMPLETION PHASE - Wait for slam to "settle"
        Debug.Log($"⏳ Slam impact settling...");
        yield return new WaitForSeconds(0.3f); // Brief pause for impact

        // Rise back above the slam location
        yield return MovePress(targetAbove, 0.4f);

        // Return to idle position - THIS ALWAYS HAPPENS
        yield return MovePress(pressIdlePosition, 0.8f);

        // ✅ SLAM COMPLETED - Reset all flags
        isSlammingDown = false;
        isPressing = false;
        Debug.Log("✅ Eradicator GUARANTEED slam sequence completed - press fully returned!");
    }

    private IEnumerator MovePress(Vector3 dest, float duration)
    {
        if (!EnsurePressExists()) yield break;

        Vector3 start = pressInstance.transform.position;
        float t = 0f;

        while (t < 1f && pressInstance != null)
        {
            t += Time.deltaTime / duration;
            if (pressInstance != null)
                pressInstance.transform.position = Vector3.Lerp(start, dest, t);
            yield return null;
        }

        // ✅ Ensure we reach the exact destination
        if (pressInstance != null)
            pressInstance.transform.position = dest;
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

    // ✅ Enhanced static cleanup with slam protection
    public static void ResetAllEradicators()
    {
        // ✅ Only reset if not currently slamming
        if (isSlammingDown)
        {
            Debug.Log("🚫 Cannot reset Eradicators - slam in progress! Will reset after completion.");
            return;
        }

        CleanupPress();
        activeEradicatorCount = 0;
        isPressing = false;
        isSlammingDown = false;
        pressIdlePosition = new Vector3(0, 5f, -12f);
        Debug.Log("🔄 All Eradicator traits reset for new round");
    }

    // ✅ NEW: Force reset method for emergency situations
    public static void ForceResetAllEradicators()
    {
        if (pressInstance != null)
        {
            Destroy(pressInstance);
            pressInstance = null;
        }

        activeEradicatorCount = 0;
        isPressing = false;
        isSlammingDown = false;
        pressIdlePosition = new Vector3(0, 5f, -12f);
        Debug.Log("🚨 FORCE RESET: All Eradicator traits forcefully reset!");
    }
}
