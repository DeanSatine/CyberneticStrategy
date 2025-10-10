using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VictoryDanceManager : MonoBehaviour
{
    public static VictoryDanceManager Instance;

    [Header("Victory Dance Settings")]
    [Tooltip("How long the victory dance lasts")]
    public float danceDuration = 3.0f;

    [Tooltip("Delay before starting dance after victory")]
    public float danceStartDelay = 0.5f;

    [Tooltip("Audio clip to play during victory dance")]
    public AudioClip victoryMusic;

    [Tooltip("Volume for victory music")]
    [Range(0f, 1f)]
    public float musicVolume = 0.7f;

    [Header("Dance Animation Triggers")]
    [Tooltip("Animation trigger names for different unit types")]
    public string[] danceAnimationTriggers = { "DanceTrigger", "VictoryTrigger", "CelebrateTrigger" };

    [Tooltip("Fallback trigger if unit doesn't have specific dance animations")]
    public string fallbackTrigger = "AbilityTrigger";

    private AudioSource audioSource;
    private bool isDancing = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            SetupAudioSource();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D audio for victory music
        audioSource.volume = musicVolume;
        audioSource.loop = false;
    }

    /// <summary>
    /// Start victory dance for all surviving player units
    /// </summary>
    public void StartVictoryDance()
    {
        if (isDancing)
        {
            Debug.Log("🎉 Victory dance already in progress, skipping");
            return;
        }

        Debug.Log("🎉 Starting victory dance celebration!");
        StartCoroutine(VictoryDanceSequence());
    }

    private IEnumerator VictoryDanceSequence()
    {
        isDancing = true;

        // Wait for initial delay
        yield return new WaitForSeconds(danceStartDelay);

        // Find all surviving player units
        List<UnitAI> survivingUnits = GetSurvivingPlayerUnits();

        if (survivingUnits.Count == 0)
        {
            Debug.Log("🎉 No surviving units to dance, ending celebration");
            isDancing = false;
            yield break;
        }

        Debug.Log($"🎉 {survivingUnits.Count} units joining the victory dance!");

        // Play victory music
        PlayVictoryMusic();

        // Start dance animations for all surviving units
        foreach (var unit in survivingUnits)
        {
            StartUnitDance(unit);
        }

        // Wait for dance duration
        yield return new WaitForSeconds(danceDuration);

        // Stop dance animations
        foreach (var unit in survivingUnits)
        {
            StopUnitDance(unit);
        }

        // Stop victory music
        StopVictoryMusic();

        Debug.Log("🎉 Victory dance celebration complete!");
        isDancing = false;
    }

    private List<UnitAI> GetSurvivingPlayerUnits()
    {
        List<UnitAI> survivingUnits = new List<UnitAI>();
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();

        foreach (var unit in allUnits)
        {
            if (unit != null &&
                unit.team == Team.Player &&
                unit.isAlive &&
                unit.currentState != UnitAI.UnitState.Bench)
            {
                survivingUnits.Add(unit);
            }
        }

        return survivingUnits;
    }

    private void StartUnitDance(UnitAI unit)
    {
        if (unit == null || unit.animator == null)
        {
            Debug.LogWarning($"🎉 Cannot start dance for {unit?.unitName} - missing animator");
            return;
        }

        // Try different dance animation triggers
        string triggerToUse = GetBestDanceTrigger(unit);

        // Set the dance trigger
        unit.animator.SetTrigger(triggerToUse);

        // Temporarily disable unit movement/attacks during dance
        unit.canMove = false;
        unit.canAttack = false;
        unit.currentTarget = null;

        Debug.Log($"🕺 {unit.unitName} started dancing with trigger: {triggerToUse}");
    }

    private void StopUnitDance(UnitAI unit)
    {
        if (unit == null)
        {
            return;
        }

        // Re-enable normal behavior
        unit.canMove = true;
        unit.canAttack = true;

        // Reset animator to idle state
        if (unit.animator != null)
        {
            unit.animator.SetTrigger("IdleTrigger");
        }

        Debug.Log($"🎉 {unit.unitName} finished dancing");
    }

    private string GetBestDanceTrigger(UnitAI unit)
    {
        if (unit.animator == null) return fallbackTrigger;

        // Check if the animator has any of our preferred dance triggers
        foreach (string trigger in danceAnimationTriggers)
        {
            if (HasAnimatorParameter(unit.animator, trigger))
            {
                return trigger;
            }
        }

        // Check if fallback trigger exists
        if (HasAnimatorParameter(unit.animator, fallbackTrigger))
        {
            return fallbackTrigger;
        }

        // Last resort - just use the first trigger we can find
        return "AbilityTrigger";
    }

    private bool HasAnimatorParameter(Animator animator, string parameterName)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
            return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == parameterName && param.type == AnimatorControllerParameterType.Trigger)
            {
                return true;
            }
        }

        return false;
    }

    private void PlayVictoryMusic()
    {
        if (victoryMusic != null && audioSource != null)
        {
            audioSource.clip = victoryMusic;
            audioSource.Play();
            Debug.Log("🎵 Victory music started!");
        }
    }

    private void StopVictoryMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("🎵 Victory music stopped");
        }
    }

    /// <summary>
    /// Public method to check if victory dance is currently active
    /// </summary>
    public bool IsDancing()
    {
        return isDancing;
    }

    /// <summary>
    /// Force stop victory dance (useful for interruptions)
    /// </summary>
    public void ForceStopDance()
    {
        if (isDancing)
        {
            StopAllCoroutines();

            // Stop all unit dances
            List<UnitAI> allUnits = GetSurvivingPlayerUnits();
            foreach (var unit in allUnits)
            {
                StopUnitDance(unit);
            }

            StopVictoryMusic();
            isDancing = false;

            Debug.Log("🎉 Victory dance force stopped");
        }
    }
}
