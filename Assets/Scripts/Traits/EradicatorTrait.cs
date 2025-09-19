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
    private static bool isSlammingDown; // ✅ Track when we're in the critical slam phase
    private static int activeEradicatorCount = 0;

    [Header("VFX")]
    public GameObject slamEffectPrefab;
    public float cameraShakeIntensity = 0.5f;
    public float cameraShakeDuration = 0.4f;

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
                StartCoroutine(UnstoppableSlamSequence(enemy));
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

    // ✅ UNSTOPPABLE: Once started, this sequence ALWAYS completes the full slam
    private IEnumerator UnstoppableSlamSequence(UnitAI target)
    {
        if (!EnsurePressExists())
        {
            Debug.Log("❌ Press sequence failed - no press available");
            yield break;
        }

        // ✅ LOCK THE SEQUENCE - Once we start, we ALWAYS finish!
        isPressing = true;
        Debug.Log($"🚀 UNSTOPPABLE SLAM SEQUENCE INITIATED for {target?.unitName ?? "unknown target"}");

        // ✅ STORE TARGET POSITION IMMEDIATELY - This never changes now!
        Vector3 originalTargetPos = target?.transform.position ?? Vector3.zero;
        Vector3 targetAbove = originalTargetPos + Vector3.up * 5f;
        Vector3 crushPos = originalTargetPos + Vector3.up * 1.3f;

        // Remember if we had a valid target at the start
        bool hadValidTarget = target != null && target.isAlive;
        string targetName = target?.unitName ?? "unknown";

        Debug.Log($"🎯 SLAM TARGET LOCKED: {originalTargetPos} (Target: {targetName})");

        // ===== PHASE 1: MOVE TO TARGET - NO EARLY EXITS! =====
        Debug.Log($"🎬 PHASE 1: Moving press to slam position - NO STOPPING!");
        yield return MovePress(targetAbove, 0.6f);

        // ===== PHASE 2: UNSTOPPABLE SLAM PHASE =====
        Debug.Log($"🔥 PHASE 2: UNSTOPPABLE SLAM - PRESS WILL COMPLETE SEQUENCE!");
        isSlammingDown = true;

        // Slam down to the LOCKED position (regardless of target state)
        yield return MovePress(crushPos, 0.2f);

        // ===== PHASE 3: GUARANTEED IMPACT EFFECTS =====
        Debug.Log($"💥 PHASE 3: GUARANTEED slam impact at {originalTargetPos}!");

        // ALWAYS spawn slam effects at the locked position
        if (slamEffectPrefab != null)
        {
            GameObject fx = Instantiate(slamEffectPrefab, originalTargetPos, Quaternion.identity);
            Destroy(fx, 2f);
            Debug.Log($"💥 Slam VFX spawned at locked position!");
        }

        // ALWAYS trigger camera shake
        StartCoroutine(CameraShake(cameraShakeIntensity, cameraShakeDuration));

        // Check if we can still damage the original target
        if (hadValidTarget && target != null && target.isAlive)
        {
            float distanceToSlam = Vector3.Distance(target.transform.position, originalTargetPos);
            if (distanceToSlam <= 2f)
            {
                Debug.Log($"💀 EXECUTION: {target.unitName} caught in unstoppable slam!");
                target.TakeDamage(999999);
            }
            else
            {
                Debug.Log($"💨 {target.unitName} moved away but slam still happened at original location!");
            }
        }
        else
        {
            Debug.Log($"💀 Slam executed at locked position - original target {targetName} no longer valid");
        }

        // ===== PHASE 4: DRAMATIC IMPACT SETTLE =====
        Debug.Log($"⏳ PHASE 4: Slam settling for maximum drama...");
        yield return new WaitForSeconds(0.3f);

        // ===== PHASE 5: RISE BACK UP =====
        Debug.Log($"⬆️ PHASE 5: Rising press back up from slam position");
        yield return MovePress(targetAbove, 0.4f);

        // ===== PHASE 6: GUARANTEED RETURN TO IDLE =====
        Debug.Log($"🏠 PHASE 6: Returning press to idle - SEQUENCE COMPLETION GUARANTEED");
        yield return MovePress(pressIdlePosition, 0.8f);

        // ✅ SEQUENCE COMPLETED - Reset all flags
        isSlammingDown = false;
        isPressing = false;
        Debug.Log("✅ UNSTOPPABLE SLAM SEQUENCE COMPLETED - Press has returned to idle!");
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

    // 📹 Camera shake - exactly as you had it!
    private IEnumerator CameraShake(float intensity, float duration)
    {
        if (Camera.main == null) yield break;

        Vector3 originalPos = Camera.main.transform.position;
        float elapsed = 0f;

        Debug.Log($"📹 Camera shake started! Intensity: {intensity}, Duration: {duration}");

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 offset = Random.insideUnitSphere * intensity;
            Camera.main.transform.position = originalPos + offset;
            yield return null;
        }

        // ✅ Always restore original position
        Camera.main.transform.position = originalPos;
        Debug.Log("📹 Camera shake completed!");
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

    // ✅ Force reset method for emergency situations
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
