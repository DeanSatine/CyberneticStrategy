using UnityEngine;
using System.Collections;

public class EradicatorTrait : MonoBehaviour
{
    [HideInInspector] public float executeThreshold;
    [HideInInspector] public GameObject pressPrefab;

    // ✅ Simplified press management - Direct execution only!
    private static GameObject pressInstance;
    private static Vector3 pressIdlePosition = new Vector3(0, 5f, -12f);
    private static bool isPressing;
    private static bool isSlammingDown;
    private static bool isCommittedToExecution; // ✅ Once true, NO BACKING DOWN!
    private static Vector3 lockedSlamPosition; // ✅ Locked slam location
    private static int activeEradicatorCount = 0;

    [Header("VFX")]
    public GameObject slamEffectPrefab;
    public float cameraShakeIntensity = 0.5f;
    public float cameraShakeDuration = 0.4f;

    [Header("🚀 Lightning Fast Execution Timing")]
    public float moveToTargetDuration = 0.4f; // ✅ Move from idle to above target
    public float lightningFastSlamDuration = 0.1f; // ✅ Lightning fast slam down!
    public float impactSettleDuration = 0.2f; // ✅ Dramatic pause after impact
    public float riseUpDuration = 0.3f; // ✅ Rise back up
    public float returnToIdleDuration = 0.5f; // ✅ Return to idle position

    [Header("🎯 Execution Settings")]
    public float executionHeight = 7f; // ✅ How high above target to start slam

    private void OnEnable()
    {
        activeEradicatorCount++;
        Debug.Log($"✅ EradicatorTrait enabled. Active count: {activeEradicatorCount}");
    }

    private void OnDisable()
    {
        activeEradicatorCount--;
        Debug.Log($"❌ EradicatorTrait disabled. Active count: {activeEradicatorCount}");

        // ✅ Only clean up if no more eradicators AND not committed to execution
        if (activeEradicatorCount <= 0 && !isCommittedToExecution)
        {
            CleanupPress();
        }
    }

    private void Update()
    {
        if (executeThreshold <= 0) return;
        if (!EnsurePressExists()) return;

        // ✅ COMMITMENT CHECK: Once committed to execution, no more decisions!
        if (isCommittedToExecution)
        {
            // Press is committed - let the animation finish
            return;
        }

        // ✅ If already executing, don't start another execution
        if (isPressing || isSlammingDown)
        {
            return;
        }

        // ✅ SIMPLE: Just scan for executable enemies
        CheckForExecutableEnemies();
    }

    // ✅ SIMPLIFIED: Direct execution detection
    private void CheckForExecutableEnemies()
    {
        UnitAI[] enemies = FindObjectsOfType<UnitAI>();

        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.isAlive || enemy.team != Team.Enemy) continue;

            float hpPercent = (float)enemy.currentHealth / enemy.maxHealth;

            // ✅ Direct execution when threshold is reached
            if (hpPercent <= executeThreshold)
            {
                Debug.Log($"🔥⚡ EXECUTION DETECTED! Target {enemy.unitName} at {hpPercent:P} HP");

                // ✅ COMMIT TO EXECUTION - NO BACKING DOWN NOW!
                CommitToExecution(enemy.transform.position);
                StartCoroutine(DirectExecutionSequence(enemy));
                return; // Only execute one at a time
            }
        }
    }

    // ✅ Commit to execution at specific location - NO RETREAT POSSIBLE!
    private void CommitToExecution(Vector3 targetPosition)
    {
        isCommittedToExecution = true;
        lockedSlamPosition = targetPosition;
        Debug.Log($"🔒 EXECUTION COMMITTED! Slam locked to position: {lockedSlamPosition}");
        Debug.Log($"⚠️  NO RETREAT POSSIBLE - PRESS WILL COMPLETE SLAM NO MATTER WHAT!");
    }

    // ✅ DIRECT EXECUTION: Straight from idle to slam completion
    private IEnumerator DirectExecutionSequence(UnitAI target)
    {
        if (!EnsurePressExists())
        {
            Debug.Log("❌ Execution failed - no press available");
            yield break;
        }

        isPressing = true;
        Debug.Log($"🚀⚡ DIRECT EXECUTION SEQUENCE for {target?.unitName ?? "unknown target"}");

        string targetName = target?.unitName ?? "unknown";
        Vector3 executionPosition = lockedSlamPosition + Vector3.up * executionHeight;
        Vector3 crushPosition = lockedSlamPosition + Vector3.up * 1.3f;

        // ===== PHASE 1: MOVE TO EXECUTION POSITION =====
        Debug.Log($"🚀 PHASE 1: Moving press to execution position above {targetName}");
        yield return MovePress(executionPosition, moveToTargetDuration);

        // ===== PHASE 2: LIGHTNING SLAM - ALWAYS HAPPENS =====
        Debug.Log($"⚡ PHASE 2: LIGHTNING SLAM - NO RETREAT!");
        isSlammingDown = true;
        yield return MovePress(crushPosition, lightningFastSlamDuration);

        // ===== PHASE 3: GUARANTEED IMPACT EFFECTS =====
        Debug.Log($"💥⚡ PHASE 3: GUARANTEED IMPACT at {lockedSlamPosition}!");

        // ALWAYS spawn slam effects at locked position
        if (slamEffectPrefab != null)
        {
            GameObject fx = Instantiate(slamEffectPrefab, lockedSlamPosition, Quaternion.identity);
            Destroy(fx, 2f);
        }

        // ALWAYS trigger camera shake
        StartCoroutine(CameraShake(cameraShakeIntensity, cameraShakeDuration));

        // ✅ Try to damage original target if still in range
        if (target != null && target.isAlive)
        {
            float distanceToSlam = Vector3.Distance(target.transform.position, lockedSlamPosition);
            if (distanceToSlam <= 2f)
            {
                Debug.Log($"💀⚡ EXECUTION SUCCESSFUL: {target.unitName} obliterated!");
                target.TakeDamage(999999);
            }
            else
            {
                Debug.Log($"💨 {target.unitName} escaped but slam completed at locked position!");
            }
        }
        else
        {
            Debug.Log($"💀 Target {targetName} died but COMMITTED SLAM completed anyway!");
        }

        // ===== PHASE 4: DRAMATIC SETTLE =====
        Debug.Log($"⏳ PHASE 4: Impact settling...");
        yield return new WaitForSeconds(impactSettleDuration);

        // ===== PHASE 5: RISE BACK UP =====
        Debug.Log($"⬆️ PHASE 5: Rising press back up");
        yield return MovePress(executionPosition, riseUpDuration);

        // ===== PHASE 6: RETURN TO IDLE =====
        Debug.Log($"🏠 PHASE 6: Returning press to idle position");
        yield return MovePress(pressIdlePosition, returnToIdleDuration);

        // ✅ RESET ALL FLAGS - EXECUTION COMPLETE
        isSlammingDown = false;
        isPressing = false;
        isCommittedToExecution = false;
        lockedSlamPosition = Vector3.zero;
        Debug.Log("✅⚡ DIRECT EXECUTION COMPLETED!");
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
        if (!isCommittedToExecution)
        {
            CleanupPress();
        }
        else
        {
            Debug.Log("🚫 Cannot despawn press - committed execution in progress!");
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
        if (isCommittedToExecution)
        {
            Debug.Log("🚫 Cannot cleanup press - committed execution in progress!");
            return;
        }

        if (pressInstance != null)
        {
            Destroy(pressInstance);
            pressInstance = null;
            isPressing = false;
            isSlammingDown = false;
            isCommittedToExecution = false;
            lockedSlamPosition = Vector3.zero;
            Debug.Log("🧹 Hydraulic press cleaned up");
        }
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

    public static void ResetAllEradicators()
    {
        if (isCommittedToExecution)
        {
            Debug.Log("🚫 Cannot reset Eradicators - committed execution in progress!");
            return;
        }

        CleanupPress();
        activeEradicatorCount = 0;
        isPressing = false;
        isSlammingDown = false;
        isCommittedToExecution = false;
        lockedSlamPosition = Vector3.zero;
        pressIdlePosition = new Vector3(0, 5f, -12f);
        Debug.Log("🔄 All Eradicator traits reset for new round");
    }

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
        isCommittedToExecution = false;
        lockedSlamPosition = Vector3.zero;
        pressIdlePosition = new Vector3(0, 5f, -12f);
        Debug.Log("🚨 FORCE RESET: All Eradicator traits forcefully reset!");
    }
}
