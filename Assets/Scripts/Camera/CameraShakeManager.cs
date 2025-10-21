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
        if (Camera.main != null)
        {
            originalCameraPosition = Camera.main.transform.position;
        }
    }

    public void Shake(float intensity, float duration)
    {
        if (Camera.main == null) return;

        if (!isShaking)
        {
            originalCameraPosition = Camera.main.transform.position;
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
            offset.y = Mathf.Abs(offset.y);
            Camera.main.transform.position = originalCameraPosition + offset;
            yield return null;
        }

        Camera.main.transform.position = originalCameraPosition;
        isShaking = false;
        currentShake = null;
    }

    public void ResetCamera()
    {
        if (Camera.main != null)
        {
            Camera.main.transform.position = originalCameraPosition;
            isShaking = false;
        }
    }
}
