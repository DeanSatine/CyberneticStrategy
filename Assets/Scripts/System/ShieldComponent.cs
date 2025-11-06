using System.Collections;
using UnityEngine;

public class ShieldComponent : MonoBehaviour
{
    private float currentShield = 0f;
    private Coroutine shieldDecayCoroutine;

    public float CurrentShield => currentShield;

    public void AddShield(float amount, float duration)
    {
        currentShield += amount;

        if (shieldDecayCoroutine != null)
        {
            StopCoroutine(shieldDecayCoroutine);
        }

        shieldDecayCoroutine = StartCoroutine(DecayShield(duration));

        Debug.Log($"🛡️ Shield added: {amount} for {duration}s. Total: {currentShield}");
    }

    public float AbsorbDamage(float incomingDamage)
    {
        if (currentShield <= 0f)
        {
            return incomingDamage;
        }

        if (incomingDamage <= currentShield)
        {
            currentShield -= incomingDamage;
            Debug.Log($"🛡️ Shield absorbed {incomingDamage} damage. Remaining: {currentShield}");
            return 0f;
        }
        else
        {
            float remainingDamage = incomingDamage - currentShield;
            Debug.Log($"🛡️ Shield absorbed {currentShield} damage, {remainingDamage} damage through!");
            currentShield = 0f;
            return remainingDamage;
        }
    }

    private IEnumerator DecayShield(float duration)
    {
        yield return new WaitForSeconds(duration);

        currentShield = 0f;
        Debug.Log($"🛡️ Shield expired");
    }

    public void ClearShield()
    {
        if (shieldDecayCoroutine != null)
        {
            StopCoroutine(shieldDecayCoroutine);
            shieldDecayCoroutine = null;
        }
        currentShield = 0f;
    }

    private void OnDisable()
    {
        ClearShield();
    }
}
