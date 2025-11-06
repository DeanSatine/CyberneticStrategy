using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class BackSlashAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;

    [Header("Slash Settings")]
    [Tooltip("Slash damage per star level")]
    public float[] slashDamage = { 150f, 250f, 400f };

    [Tooltip("Distance of slash in hexes")]
    public float slashDistance = 3f;

    [Tooltip("Width of slash area")]
    public float slashWidth = 2f;

    [Header("Slam Settings (Every 3rd Cast)")]
    [Tooltip("Slam damage per star level")]
    public float[] slamDamage = { 300f, 500f, 800f };

    [Tooltip("Slam radius in hexes")]
    public float slamRadius = 6f;

    [Header("VFX")]
    public GameObject slashVFX;
    public GameObject slamVFX;

    [Header("Audio")]
    public AudioClip slashSound;
    public AudioClip slamSound;
    [Range(0f, 1f)]
    public float audioVolume = 0.8f;

    private AudioSource audioSource;
    private int castCount = 0;
    private bool waitingForAnimationEvent = false;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        SetupAudio();
    }

    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.5f;
        audioSource.volume = audioVolume;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, audioVolume);
        }
    }

    public void Cast(UnitAI target)
    {
        if (!unitAI.isAlive || waitingForAnimationEvent) return;

        unitAI.currentMana = 0f;
        if (unitAI.ui != null)
        {
            unitAI.ui.UpdateMana(unitAI.currentMana);
        }

        castCount++;

        if (castCount % 3 == 0)
        {
            StartSlamAnimation();
        }
        else
        {
            StartSlashAnimation();
        }
    }

    private void StartSlashAnimation()
    {
        if (unitAI.animator)
        {
            unitAI.animator.SetBool("IsSlam", false);
            unitAI.animator.SetTrigger("AbilityTrigger");
        }
        else
        {
            OnSlashAnimationEvent();
        }
    }

    private void StartSlamAnimation()
    {
        waitingForAnimationEvent = true;

        if (unitAI.animator)
        {
            unitAI.animator.SetBool("IsSlam", true);
            unitAI.animator.SetTrigger("HeavyAbilityTrigger");
        }
        else
        {
            OnSlamAnimationEvent();
        }
    }


    public void OnSlashAnimationEvent()
    {
        PlaySound(slashSound);

        Vector3 slashDirection = transform.forward;
        Vector3 slashCenter = transform.position + slashDirection * (slashDistance * 0.5f);

        if (slashVFX != null)
        {
            GameObject vfx = Instantiate(slashVFX, slashCenter, Quaternion.LookRotation(slashDirection));
            Destroy(vfx, 2f);
        }

        int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, slashDamage.Length - 1);
        float damage = slashDamage[starIndex];

        List<UnitAI> enemies = FindEnemiesInFront();

        foreach (UnitAI enemy in enemies)
        {
            unitAI.DealMagicDamage(enemy, damage);
        }

        Debug.Log($"⚔️ {unitAI.unitName} slashed, hitting {enemies.Count} enemies for {damage} magic damage! (Cast {castCount})");
    }

    public void OnSlamAnimationEvent()
    {
        PlaySound(slamSound);

        if (slamVFX != null)
        {
            GameObject vfx = Instantiate(slamVFX, transform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, slamDamage.Length - 1);
        float damage = slamDamage[starIndex];

        Collider[] hits = Physics.OverlapSphere(transform.position, slamRadius);
        HashSet<UnitAI> hitEnemies = new HashSet<UnitAI>();

        foreach (Collider col in hits)
        {
            UnitAI enemy = col.GetComponent<UnitAI>();
            if (enemy != null && enemy.teamID != unitAI.teamID && enemy.isAlive && !hitEnemies.Contains(enemy))
            {
                unitAI.DealMagicDamage(enemy, damage);
                hitEnemies.Add(enemy);
            }
        }

        Debug.Log($"💥 {unitAI.unitName} SLAMMED, hitting {hitEnemies.Count} enemies for {damage} magic damage! (3rd cast)");

        waitingForAnimationEvent = false;
    }

    private List<UnitAI> FindEnemiesInFront()
    {
        List<UnitAI> enemiesInFront = new List<UnitAI>();

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        Collider[] hits = Physics.OverlapBox(
            transform.position + forward * (slashDistance * 0.5f),
            new Vector3(slashWidth * 0.5f, 2f, slashDistance * 0.5f),
            Quaternion.LookRotation(forward)
        );

        foreach (Collider col in hits)
        {
            UnitAI enemy = col.GetComponent<UnitAI>();
            if (enemy != null && enemy.teamID != unitAI.teamID && enemy.isAlive)
            {
                Vector3 toEnemy = enemy.transform.position - transform.position;
                toEnemy.y = 0;

                float dotProduct = Vector3.Dot(forward, toEnemy.normalized);

                if (dotProduct > 0.5f)
                {
                    enemiesInFront.Add(enemy);
                }
            }
        }

        return enemiesInFront;
    }

    public void OnRoundEnd()
    {
        castCount = 0;
        waitingForAnimationEvent = false;

        if (unitAI != null && unitAI.animator != null)
        {
            unitAI.animator.SetBool("IsSlam", false);
        }
    }

    private void OnDisable()
    {
        OnRoundEnd();
    }
}
