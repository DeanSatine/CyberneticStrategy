using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static UnitAI;

public class TracerCoreAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;

    [Header("Ability Stats")]
    [Tooltip("Damage per bolt at each star level")]
    public float[] damagePerStar = { 400f, 900f, 1500f };

    [Header("VFX")]
    public GameObject aimLinePrefab;
    public GameObject boltPrefab;
    public GameObject hitVFX;
    public Color aimLineColor = Color.cyan;
    public float aimLineWidth = 0.1f;

    [Header("Audio")]
    public AudioClip aimSound;
    public AudioClip fireSound;
    public AudioClip hitSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    private AudioSource audioSource;
    private List<UnitAI> targets = new List<UnitAI>();
    private List<LineRenderer> aimLines = new List<LineRenderer>();

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.8f;
        }
    }

    public void Cast(UnitAI target)
    {
        if (unitAI.currentState != UnitState.Combat && unitAI.currentState != UnitState.BoardIdle)
        {
            Debug.Log($"[TracerCoreAbility] Cannot cast in state: {unitAI.currentState}");
            return;
        }

        if (unitAI.animator != null)
        {
            unitAI.animator.SetTrigger("AbilityTrigger");
        }

        Debug.Log($"⚡ {unitAI.unitName} activates TracerCore ability!");
    }

    public void OnAimTargets()
    {
        ClearAimLines();
        targets.Clear();

        PlaySound(aimSound);

        targets = FindTwoFarthestEnemies();

        if (targets.Count == 0)
        {
            Debug.Log($"⚠️ {unitAI.unitName} found no valid targets for TracerCore!");
            return;
        }

        Vector3 firePoint = unitAI.firePoint != null ? unitAI.firePoint.position : transform.position + Vector3.up * 1.5f;

        foreach (var target in targets)
        {
            if (target != null && target.isAlive)
            {
                LineRenderer aimLine = CreateAimLine(firePoint, target.transform.position + Vector3.up * 1.2f);
                aimLines.Add(aimLine);
            }
        }

        Debug.Log($"🎯 {unitAI.unitName} aims at {targets.Count} targets!");
    }

    public void OnFireBolts()
    {
        ClearAimLines();

        PlaySound(fireSound);

        Vector3 firePoint = unitAI.firePoint != null ? unitAI.firePoint.position : transform.position + Vector3.up * 1.5f;

        int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, damagePerStar.Length - 1);
        float damage = damagePerStar[starIndex];

        foreach (var target in targets)
        {
            if (target != null && target.isAlive && target.currentState != UnitState.Bench)
            {
                StartCoroutine(FireBolt(firePoint, target, damage));
            }
        }

        Debug.Log($"⚡ {unitAI.unitName} fires {targets.Count} bolts for {damage} damage each!");
    }

    private List<UnitAI> FindTwoFarthestEnemies()
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        List<UnitAI> validEnemies = new List<UnitAI>();

        foreach (var unit in allUnits)
        {
            if (unit == this || unit == unitAI || !unit.isAlive) continue;
            if (unit.team == unitAI.team) continue;
            if (unit.currentState == UnitState.Bench) continue;

            validEnemies.Add(unit);
        }

        validEnemies.Sort((a, b) =>
        {
            float distA = Vector3.Distance(transform.position, a.transform.position);
            float distB = Vector3.Distance(transform.position, b.transform.position);
            return distB.CompareTo(distA);
        });

        List<UnitAI> farthestTwo = new List<UnitAI>();
        for (int i = 0; i < Mathf.Min(2, validEnemies.Count); i++)
        {
            farthestTwo.Add(validEnemies[i]);
        }

        return farthestTwo;
    }

    private LineRenderer CreateAimLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("TracerCore_AimLine");
        lineObj.transform.SetParent(transform);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        lr.startWidth = aimLineWidth;
        lr.endWidth = aimLineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = aimLineColor;
        lr.endColor = aimLineColor;

        lr.sortingOrder = 100;

        return lr;
    }

    private void ClearAimLines()
    {
        foreach (var line in aimLines)
        {
            if (line != null)
            {
                Destroy(line.gameObject);
            }
        }
        aimLines.Clear();
    }

    private IEnumerator FireBolt(Vector3 startPos, UnitAI target, float damage)
    {
        GameObject bolt = null;

        if (boltPrefab != null)
        {
            bolt = Instantiate(boltPrefab, startPos, Quaternion.identity);
        }
        else if (unitAI.projectilePrefab != null)
        {
            bolt = Instantiate(unitAI.projectilePrefab, startPos, Quaternion.identity);
        }

        if (bolt == null)
        {
            Debug.LogWarning($"No bolt prefab found for {unitAI.unitName}");
            yield break;
        }

        float speed = 25f;
        bool hasHitTarget = false;

        while (bolt != null && !hasHitTarget)
        {
            if (target == null || !target.isAlive || target.currentState == UnitState.Bench)
            {
                Destroy(bolt);
                yield break;
            }

            Vector3 targetPos = target.transform.position + Vector3.up * 1.2f;
            Vector3 direction = (targetPos - bolt.transform.position).normalized;

            bolt.transform.position += direction * speed * Time.deltaTime;
            bolt.transform.rotation = Quaternion.LookRotation(direction);

            if (Vector3.Distance(bolt.transform.position, targetPos) < 0.3f)
            {
                unitAI.DealMagicDamageWithAP(target, damage, 1.0f);

                if (hitVFX != null)
                {
                    GameObject vfx = Instantiate(hitVFX, targetPos, Quaternion.identity);
                    Destroy(vfx, 2f);
                }

                PlaySound(hitSound);

                Debug.Log($"⚡ TracerCore bolt hit {target.unitName} for {damage} damage!");

                hasHitTarget = true;
                Destroy(bolt);
                yield break;
            }

            yield return null;
        }

        if (bolt != null)
        {
            Destroy(bolt);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    public void OnRoundEnd()
    {
        ClearAimLines();
        targets.Clear();
        StopAllCoroutines();

        Debug.Log($"[TracerCoreAbility] Round ended for {unitAI.unitName}");
    }
}
