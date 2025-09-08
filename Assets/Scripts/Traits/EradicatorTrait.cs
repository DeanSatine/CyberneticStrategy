using UnityEngine;
using System.Collections;

public class EradicatorTrait : MonoBehaviour
{
    [HideInInspector] public float executeThreshold;
    [HideInInspector] public GameObject pressPrefab;

    // ✅ Make press management more robust
    private static GameObject pressInstance;
    private static Vector3 pressIdlePosition = new Vector3(0, 5f, -12f); // ✅ Your preferred position
    private static bool isPressing;
    private static int activeEradicatorCount = 0;

    [Header("VFX")]
    public GameObject slamEffectPrefab;
    public float cameraShakeIntensity = 0.3f;
    public float cameraShakeDuration = 0.2f;

    private void OnEnable()
    {
        activeEradicatorCount++;
        Debug.Log($"✅ EradicatorTrait enabled. Active count: {activeEradicatorCount}");

        // ✅ Subscribe to damage events to catch low-health enemies
        UnitAI unitAI = GetComponent<UnitAI>();
        if (unitAI != null)
        {
            // Subscribe to all unit damage events
            SubscribeToAllUnits();
        }
    }

    private void OnDisable()
    {
        activeEradicatorCount--;
        Debug.Log($"❌ EradicatorTrait disabled. Active count: {activeEradicatorCount}");

        // ✅ Clean up press if no more eradicators
        if (activeEradicatorCount <= 0)
        {
            CleanupPress();
        }
    }

    private void Update()
    {
        // ✅ Only operate if we have valid threshold and press
        if (executeThreshold <= 0 || isPressing) return;

        // ✅ Ensure press exists and is valid
        if (!EnsurePressExists()) return;

        // ✅ Check for enemies that are already below threshold
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
                StartCoroutine(PressSequence(enemy));
                return; // Only execute one at a time
            }
        }
    }

    // ✅ Subscribe to damage events on all enemy units
    private void SubscribeToAllUnits()
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        foreach (var unit in allUnits)
        {
            if (unit.team == Team.Enemy)
            {
                // We'll check on each frame instead of events for now
                // This ensures we don't miss fast deaths
            }
        }
    }

    // ✅ Robust press spawning with your preferred position
    public void SpawnPressIfNeeded()
    {
        if (pressPrefab != null)
        {
            EnsurePressExists();
        }
    }

    // ✅ Safe press cleanup
    public void DespawnPress()
    {
        CleanupPress();
    }

    private bool EnsurePressExists()
    {
        if (pressPrefab == null) return false;

        // ✅ Check if press is destroyed or null
        if (pressInstance == null || pressInstance.Equals(null))
        {
            // ✅ Use your preferred spawn position
            pressIdlePosition = new Vector3(0, 5f, -12f);
            pressInstance = Instantiate(pressPrefab, pressIdlePosition, Quaternion.identity);
            Debug.Log($"✅ Hydraulic press spawned at {pressIdlePosition}");
            return true;
        }

        return pressInstance != null;
    }

    private static void CleanupPress()
    {
        if (pressInstance != null)
        {
            Destroy(pressInstance);
            pressInstance = null;
            isPressing = false;
            Debug.Log("🧹 Hydraulic press cleaned up");
        }
    }

    private IEnumerator PressSequence(UnitAI target)
    {
        if (!EnsurePressExists() || target == null)
        {
            Debug.Log("❌ Press sequence failed - no press or target");
            yield break;
        }

        isPressing = true;
        Debug.Log($"⚔️ Eradicator press sequence started on {target.unitName}");

        // Move press above target
        Vector3 targetAbove = target.transform.position + Vector3.up * 5f;
        yield return MovePress(targetAbove, 0.6f);

        // ✅ Check target is still valid and below threshold before slamming
        if (target == null || !target.isAlive)
        {
            Debug.Log("❌ Target died during press movement");
            yield return MovePress(pressIdlePosition, 0.8f);
            isPressing = false;
            yield break;
        }

        // ✅ Double-check threshold right before execution
        float currentHpPercent = (float)target.currentHealth / target.maxHealth;
        if (currentHpPercent > executeThreshold)
        {
            Debug.Log($"❌ Target {target.unitName} healed above threshold ({currentHpPercent:P} > {executeThreshold:P})");
            yield return MovePress(pressIdlePosition, 0.8f);
            isPressing = false;
            yield break;
        }

        // Slam down
        Vector3 crushPos = target.transform.position + Vector3.up * 1.3f;
        yield return MovePress(crushPos, 0.2f);

        // 🔥 Execute the target
        if (target != null && target.isAlive)
        {
            Debug.Log($"💀 Eradicator executed {target.unitName}!");

            if (slamEffectPrefab != null)
            {
                GameObject fx = Instantiate(slamEffectPrefab, target.transform.position, Quaternion.identity);
                Destroy(fx, 2f);
            }
            StartCoroutine(CameraShake(cameraShakeIntensity, cameraShakeDuration));

            // ✅ Kill instantly with massive damage
            target.TakeDamage(999999);
        }

        // Rise back above target
        yield return MovePress(targetAbove, 0.4f);

        // Return to idle position
        yield return MovePress(pressIdlePosition, 0.8f);

        isPressing = false;
        Debug.Log("✅ Eradicator press sequence completed");
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

    // ✅ Static cleanup method for round resets
    public static void ResetAllEradicators()
    {
        CleanupPress();
        activeEradicatorCount = 0;
        isPressing = false;
        pressIdlePosition = new Vector3(0, 5f, -12f); // ✅ Reset to your preferred position
        Debug.Log("🔄 All Eradicator traits reset for new round");
    }
}
