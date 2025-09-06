using UnityEngine;
using System.Collections;

public class EradicatorTrait : MonoBehaviour
{
    [HideInInspector] public float executeThreshold;
    [HideInInspector] public GameObject pressPrefab;

    private static GameObject pressInstance;       // shared instance
    private static Vector3 pressIdlePosition;
    private static bool isPressing;

    [Header("VFX")]
    public GameObject slamEffectPrefab; // assign particle effect prefab
    public float cameraShakeIntensity = 0.3f;
    public float cameraShakeDuration = 0.2f;

    private void Start()
    {
        // Spawn press only once globally
        if (pressPrefab != null && pressInstance == null)
        {
            Vector3 boardCenter = FindBoardCenter();
            pressIdlePosition = boardCenter + Vector3.up * 4f;
            pressInstance = Instantiate(pressPrefab, pressIdlePosition, Quaternion.identity);
        }
    }

    private void Update()
    {
        if (isPressing || pressInstance == null) return;

        // Look for any enemy under threshold
        UnitAI[] enemies = FindObjectsOfType<UnitAI>();
        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.isAlive) continue;

            float hpPercent = (float)enemy.currentHealth / enemy.maxHealth;
            if (hpPercent <= executeThreshold)
            {
                StartCoroutine(PressSequence(enemy));
                break;
            }
        }
    }

    private IEnumerator PressSequence(UnitAI target)
    {
        isPressing = true;

        // Move press above target
        Vector3 targetAbove = target.transform.position + Vector3.up * 5f;
        yield return MovePress(targetAbove, 0.6f);

        // Slam down
        Vector3 crushPos = target.transform.position + Vector3.up * 1.3f;
        yield return MovePress(crushPos, 0.2f);

        // 🔥 Slam effect
        if (slamEffectPrefab != null)
        {
            GameObject fx = Instantiate(slamEffectPrefab, target.transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }
        StartCoroutine(CameraShake(cameraShakeIntensity, cameraShakeDuration));

        // Kill instantly
        target.TakeDamage(99999);

        // Rise back above target
        yield return MovePress(targetAbove, 0.4f);

        // Return to idle (board center)
        yield return MovePress(pressIdlePosition, 0.8f);

        isPressing = false;
    }

    private IEnumerator MovePress(Vector3 dest, float duration)
    {
        Vector3 start = pressInstance.transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            pressInstance.transform.position = Vector3.Lerp(start, dest, t);
            yield return null;
        }
    }
    public void SpawnPressIfNeeded()
    {
        if (pressPrefab != null && pressInstance == null)
        {
            pressIdlePosition = new Vector3(0, 5f, -12); // board center or adjust
            pressInstance = Instantiate(pressPrefab, pressIdlePosition, Quaternion.identity);
        }
    }
    public void DespawnPress()
    {
        if (pressInstance != null)
        {
            Destroy(pressInstance);
            pressInstance = null;
        }
    }

    private Vector3 FindBoardCenter()
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        if (allUnits.Length == 0) return Vector3.zero;

        Vector3 sum = Vector3.zero;
        foreach (var u in allUnits)
            sum += u.transform.position;

        return sum / allUnits.Length;
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
}
