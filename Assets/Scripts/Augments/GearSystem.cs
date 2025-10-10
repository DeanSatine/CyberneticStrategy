// /Assets/Scripts/Augments/GearSystem.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GearSystem : MonoBehaviour
{
    private SupportTheRevolutionAugment augment;
    private UnitAI unitAI;
    private List<GameObject> gears = new List<GameObject>();
    private int maxGears;
    private float orbitRadius;
    private float orbitSpeed;
    private float gearRefreshTimer;

    [Header("Gear Visual Settings")]
    public GameObject gearPrefab;
    public float gearScale = 0.5f;
    public float flySpeed = 8f;
    public float flyHeight = 2f;

    private bool hasSubscribedToAttack = false;

    public void Initialize(SupportTheRevolutionAugment augment, int gearCount, float radius, float speed)
    {
        this.augment = augment;
        this.maxGears = gearCount;
        this.orbitRadius = radius;
        this.orbitSpeed = speed;

        unitAI = GetComponent<UnitAI>();

        // Load gear prefab if not assigned
        if (gearPrefab == null)
        {
            gearPrefab = CreateDefaultGear();
        }

        // Subscribe to attack events
        SubscribeToAttackEvents();

        // Create initial gears
        CreateGears();

        Debug.Log($"⚙️ GearSystem initialized for {unitAI.unitName} with {gearCount} gears");
    }

    private GameObject CreateDefaultGear()
    {
        // Create a simple gear prefab if none provided
        GameObject gear = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        gear.transform.localScale = Vector3.one * gearScale;

        // Make it look like a gear
        Renderer renderer = gear.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = augment.augmentColor;
            renderer.material.SetFloat("_Metallic", 0.8f);
        }

        // Remove collider
        Collider collider = gear.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }

        return gear;
    }

    private void SubscribeToAttackEvents()
    {
        if (unitAI != null && !hasSubscribedToAttack)
        {
            unitAI.OnAttackEvent += OnUnitAttack;
            hasSubscribedToAttack = true;
            Debug.Log($"⚙️ Subscribed to attack events for {unitAI.unitName}");
        }
    }

    private void OnUnitAttack(UnitAI target)
    {
        if (gears.Count > 0)
        {
            SendGearToHeal();
        }
    }

    private void CreateGears()
    {
        // Clear existing gears
        ClearGears();

        // Create new gears
        for (int i = 0; i < maxGears; i++)
        {
            GameObject gear = Instantiate(gearPrefab);
            gear.transform.SetParent(transform);
            gear.name = $"Gear_{i}";
            gears.Add(gear);
        }

        Debug.Log($"⚙️ Created {gears.Count} gears for {unitAI.unitName}");
    }

    private void Update()
    {
        UpdateGearPositions();
        UpdateGearRefreshTimer();
    }

    private void UpdateGearPositions()
    {
        if (gears.Count == 0) return;

        float angleStep = 360f / gears.Count;
        float currentTime = Time.time * orbitSpeed;

        for (int i = 0; i < gears.Count; i++)
        {
            if (gears[i] != null)
            {
                float angle = (angleStep * i + currentTime) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * orbitRadius,
                    1.5f, // Height above unit
                    Mathf.Sin(angle) * orbitRadius
                );

                gears[i].transform.position = transform.position + offset;
                gears[i].transform.Rotate(Vector3.up, orbitSpeed * Time.deltaTime);
            }
        }
    }

    private void UpdateGearRefreshTimer()
    {
        if (gears.Count < maxGears)
        {
            gearRefreshTimer += Time.deltaTime;

            if (gearRefreshTimer >= augment.gearRefreshTime)
            {
                RefreshGears();
                gearRefreshTimer = 0f;
            }
        }
    }

    private void SendGearToHeal()
    {
        if (gears.Count == 0) return;

        UnitAI target = augment.FindLowestHealthAlly(unitAI);
        if (target == null) return;

        GameObject gearToSend = gears[0];
        gears.RemoveAt(0);

        StartCoroutine(FlyGearToTarget(gearToSend, target));
    }

    private IEnumerator FlyGearToTarget(GameObject gear, UnitAI target)
    {
        if (gear == null || target == null) yield break;

        Vector3 startPos = gear.transform.position;
        Vector3 targetPos = target.transform.position + Vector3.up * 1.5f;
        Vector3 midPoint = Vector3.Lerp(startPos, targetPos, 0.5f) + Vector3.up * flyHeight;

        float journeyTime = Vector3.Distance(startPos, targetPos) / flySpeed;
        float t = 0f;

        while (t < 1f && gear != null && target != null && target.isAlive)
        {
            t += Time.deltaTime / journeyTime;

            // Bezier curve for smooth arc
            Vector3 currentPos = CalculateBezierPoint(t, startPos, midPoint, targetPos);
            gear.transform.position = currentPos;

            // Spin the gear while flying
            gear.transform.Rotate(Vector3.up, 360f * Time.deltaTime);

            yield return null;
        }

        // Apply healing when gear reaches target
        if (target != null && target.isAlive)
        {
            float healAmount = augment.GetCurrentHealAmount();
            augment.OnGearHeal(target, healAmount);
        }

        // Destroy the gear
        if (gear != null)
        {
            Destroy(gear);
        }
    }

    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 point = uu * p0;
        point += 2 * u * t * p1;
        point += tt * p2;

        return point;
    }

    public void RefreshGears()
    {
        CreateGears();
        gearRefreshTimer = 0f;
        Debug.Log($"⚙️ Refreshed gears for {unitAI.unitName}");
    }

    public void ResetForNextRound()
    {
        RefreshGears();
        gearRefreshTimer = 0f;
    }

    private void ClearGears()
    {
        foreach (GameObject gear in gears)
        {
            if (gear != null)
            {
                Destroy(gear);
            }
        }
        gears.Clear();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (unitAI != null && hasSubscribedToAttack)
        {
            unitAI.OnAttackEvent -= OnUnitAttack;
        }

        ClearGears();
    }
}
