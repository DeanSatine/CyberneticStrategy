using System.Collections;
using UnityEngine;

public class CameraShakeManager : MonoBehaviour
{
    private static CameraShakeManager instance;
    public static CameraShakeManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("CameraShakeManager");
                instance = go.AddComponent<CameraShakeManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private Vector3 originalCameraPosition;
    private bool isShaking = false;
    private Coroutine currentShake;
    private bool hasStoredOriginalPosition = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StoreOriginalCameraPosition();
    }

    private void StoreOriginalCameraPosition()
    {
        if (Camera.main != null && !hasStoredOriginalPosition)
        {
            originalCameraPosition = Camera.main.transform.position;
            hasStoredOriginalPosition = true;
            Debug.Log($"📷 Camera original position stored: {originalCameraPosition}");
        }
    }

    public void Shake(float intensity, float duration)
    {
        if (Camera.main == null) return;

        if (!isShaking)
        {
            StoreOriginalCameraPosition();
        }

        if (currentShake != null)
        {
            StopCoroutine(currentShake);
        }

        currentShake = StartCoroutine(ShakeCoroutine(intensity, duration));
    }

    private IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float strength = Mathf.Lerp(intensity, 0f, elapsed / duration);

            Vector3 offset = Random.insideUnitSphere * strength;
            offset.y = 0f;

            Camera.main.transform.position = originalCameraPosition + offset;
            yield return null;
        }

        Camera.main.transform.position = originalCameraPosition;
        isShaking = false;
        currentShake = null;

        Debug.Log($"📷 Camera shake complete, returned to: {Camera.main.transform.position}");
    }

    public void ResetCamera()
    {
        if (Camera.main != null)
        {
            Camera.main.transform.position = originalCameraPosition;
            isShaking = false;

            if (currentShake != null)
            {
                StopCoroutine(currentShake);
                currentShake = null;
            }

            Debug.Log($"📷 Camera forcefully reset to: {originalCameraPosition}");
        }
    }

    public void UpdateOriginalPosition()
    {
        if (Camera.main != null && !isShaking)
        {
            originalCameraPosition = Camera.main.transform.position;
            hasStoredOriginalPosition = true;
            Debug.Log($"📷 Camera original position updated to: {originalCameraPosition}");
        }
    }
}
