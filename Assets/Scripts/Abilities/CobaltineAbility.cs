using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class CobaltineAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;
    private CyberneticVFX vfxManager;
    private Coroutine cloudRoutine;

    [Header("Passive: All Healing → Damage Conversion")]
    [Tooltip("Percentage of ALL healing received converted to next auto attack bonus damage (per star level)")]
    public float[] healToDamageConversion = { 0.5f, 0.75f, 1f };

    private float totalHealingReceived = 0f;

    [Header("Active: Armor Drain Cloud")]
    [Tooltip("Armor drained per enemy per second at 1/2/3 stars")]
    public float[] armorDrainPerSecond = { 3f, 5f, 100f };

    [Tooltip("Healing per second at 1/2/3 stars")]
    public float[] healingPerSecond = { 30f, 90f, 500f };

    [Tooltip("Duration of the cloud at 1/2/3 stars")]
    public float[] cloudDuration = { 4f, 5f, 30f };

    [Tooltip("Cloud radius in hex tiles")]
    public int cloudRadius = 2;

    [Header("VFX")]
    [Tooltip("VFX to play on body when Cobaltine receives healing")]
    public GameObject healVFX;

    [Tooltip("Cloud VFX spawned above the target area")]
    public GameObject cloudVFX;

    [Tooltip("VFX to play on hand when passive damage is ready")]
    public GameObject passiveReadyVFX;

    [Header("Audio")]
    public AudioClip abilityStartSound;
    public AudioClip cloudTickSound;
    public AudioClip passiveProcSound;
    [Range(0f, 1f)]
    public float audioVolume = 0.7f;

    private AudioSource audioSource;
    private GameObject activeCloudVFX;
    private GameObject passiveVFXInstance;
    private List<ArmorDebuff> activeDebuffs = new List<ArmorDebuff>();

    private class ArmorDebuff
    {
        public UnitAI target;
        public float armorReduction;

        public ArmorDebuff(UnitAI target, float armorReduction)
        {
            this.target = target;
            this.armorReduction = armorReduction;
        }
    }

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        vfxManager = GetComponent<CyberneticVFX>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.8f;
        }
    }

    private void Start()
    {
        unitAI.OnAttackEvent += OnAutoAttack;
        unitAI.OnHealReceived += OnHealReceived;
    }

    private void OnDestroy()
    {
        if (unitAI != null)
        {
            unitAI.OnAttackEvent -= OnAutoAttack;
            unitAI.OnHealReceived -= OnHealReceived;
        }

        if (cloudRoutine != null)
        {
            StopCoroutine(cloudRoutine);
        }

        CleanupActiveDebuffs();

        if (activeCloudVFX != null)
        {
            Destroy(activeCloudVFX);
        }

        if (passiveVFXInstance != null)
        {
            Destroy(passiveVFXInstance);
        }
    }

    public void Cast(UnitAI target)
    {
        if (!unitAI.isAlive) return;

        if (unitAI.animator != null)
        {
            unitAI.animator.SetTrigger("AbilityTrigger");
        }

        PlayAbilityAudio(abilityStartSound);

        Vector3 cloudPosition = transform.position + Vector3.up * 3f;

        if (cloudVFX != null)
        {
            activeCloudVFX = Instantiate(cloudVFX, cloudPosition, Quaternion.identity);
            activeCloudVFX.transform.localScale = Vector3.one * cloudRadius * 2f;
        }

        cloudRoutine = StartCoroutine(CloudEffectRoutine());
    }

    private IEnumerator CloudEffectRoutine()
    {
        float duration = cloudDuration[Mathf.Clamp(unitAI.starLevel - 1, 0, cloudDuration.Length - 1)];
        float elapsed = 0f;
        float tickRate = 1f;
        float nextTick = 0f;

        Debug.Log($"☁️ {unitAI.unitName} cloud active for {duration}s!");

        while (elapsed < duration && unitAI != null && unitAI.isAlive)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextTick)
            {
                nextTick += tickRate;
                ProcessCloudTick();
            }

            yield return null;
        }

        CleanupActiveDebuffs();

        if (activeCloudVFX != null)
        {
            Destroy(activeCloudVFX);
        }

        unitAI.currentMana = 0f;
        if (unitAI.ui != null)
        {
            unitAI.ui.UpdateMana(unitAI.currentMana);
        }

        Debug.Log($"☁️ {unitAI.unitName} cloud expired! Total healing during duration: {totalHealingReceived}");
    }

    private void ProcessCloudTick()
    {
        PlayAbilityAudio(cloudTickSound);

        List<UnitAI> enemiesInCloud = FindEnemiesInCloud();
        float armorDrain = armorDrainPerSecond[Mathf.Clamp(unitAI.starLevel - 1, 0, armorDrainPerSecond.Length - 1)];

        foreach (UnitAI enemy in enemiesInCloud)
        {
            enemy.armor = Mathf.Max(0, enemy.armor - armorDrain);

            ArmorDebuff existingDebuff = activeDebuffs.Find(d => d.target == enemy);
            if (existingDebuff != null)
            {
                existingDebuff.armorReduction += armorDrain;
            }
            else
            {
                activeDebuffs.Add(new ArmorDebuff(enemy, armorDrain));
            }

            Debug.Log($"☁️ {unitAI.unitName} drained {armorDrain} armor from {enemy.unitName} (now: {enemy.armor})");
        }

        float healing = healingPerSecond[Mathf.Clamp(unitAI.starLevel - 1, 0, healingPerSecond.Length - 1)];
        HealSelf(healing);
    }

    private List<UnitAI> FindEnemiesInCloud()
    {
        List<UnitAI> enemies = new List<UnitAI>();

        if (BoardManager.Instance == null || unitAI.currentTile == null)
        {
            return enemies;
        }

        HexTile centerTile = unitAI.currentTile;
        HashSet<HexTile> tilesInRadius = GetTilesInHexRadius(centerTile, cloudRadius);

        foreach (HexTile tile in tilesInRadius)
        {
            if (tile.occupyingUnit != null)
            {
                UnitAI unit = tile.occupyingUnit;
                if (unit != unitAI && unit.isAlive && unit.team != unitAI.team && unit.currentState != UnitState.Bench)
                {
                    enemies.Add(unit);
                }
            }
        }

        return enemies;
    }

    private HashSet<HexTile> GetTilesInHexRadius(HexTile centerTile, int radius)
    {
        HashSet<HexTile> tiles = new HashSet<HexTile>();
        if (centerTile == null || BoardManager.Instance == null) return tiles;

        Queue<HexTile> toProcess = new Queue<HexTile>();
        Dictionary<HexTile, int> distances = new Dictionary<HexTile, int>();

        tiles.Add(centerTile);
        toProcess.Enqueue(centerTile);
        distances[centerTile] = 0;

        while (toProcess.Count > 0)
        {
            HexTile current = toProcess.Dequeue();
            int currentDist = distances[current];

            if (currentDist >= radius)
                continue;

            List<HexTile> neighbors = BoardManager.Instance.GetNeighbors(current);
            foreach (var neighbor in neighbors)
            {
                if (!tiles.Contains(neighbor) && neighbor.tileType == TileType.Board)
                {
                    tiles.Add(neighbor);
                    toProcess.Enqueue(neighbor);
                    distances[neighbor] = currentDist + 1;
                }
            }
        }

        return tiles;
    }

    public void HealSelf(float healAmount)
    {
        if (!unitAI.isAlive) return;

        float actualHealing = Mathf.Min(healAmount, unitAI.maxHealth - unitAI.currentHealth);

        if (actualHealing <= 0) return;

        unitAI.currentHealth = Mathf.Min(unitAI.maxHealth, unitAI.currentHealth + actualHealing);

        if (unitAI.ui != null)
        {
            unitAI.ui.UpdateHealth(unitAI.currentHealth);
        }

        if (healVFX != null)
        {
            GameObject healEffect = Instantiate(healVFX, transform.position + Vector3.up * 1f, Quaternion.identity);
            Destroy(healEffect, 1.5f);
        }

        unitAI.RaiseHealReceivedEvent(actualHealing);

        Debug.Log($"💚 {unitAI.unitName} healed {actualHealing} HP!");
    }

    private void OnHealReceived(float healAmount)
    {
        totalHealingReceived += healAmount;

        UpdatePassiveVFX();

        Debug.Log($"💚 {unitAI.unitName} tracked {healAmount} healing! Total: {totalHealingReceived}");
    }

    private void OnAutoAttack(UnitAI target)
    {
        if (totalHealingReceived > 0 && target != null && target.isAlive)
        {
            PlayAbilityAudio(passiveProcSound);

            float conversionRate = healToDamageConversion[Mathf.Clamp(unitAI.starLevel - 1, 0, healToDamageConversion.Length - 1)];
            float bonusDamage = totalHealingReceived * conversionRate;

            unitAI.DealMagicDamageWithAP(target, bonusDamage, 0.5f);

            Debug.Log($"⚡ {unitAI.unitName} dealt {bonusDamage} bonus passive damage ({totalHealingReceived} healing × {conversionRate * 100}%) to {target.unitName}!");

            totalHealingReceived = 0f;
            UpdatePassiveVFX();
        }
    }

    private void UpdatePassiveVFX()
    {
        if (passiveReadyVFX == null) return;

        if (totalHealingReceived > 0 && passiveVFXInstance == null)
        {
            Transform handPoint = unitAI.firePoint != null ? unitAI.firePoint : transform;
            passiveVFXInstance = Instantiate(passiveReadyVFX, handPoint.position, Quaternion.identity, handPoint);
        }
        else if (totalHealingReceived <= 0 && passiveVFXInstance != null)
        {
            Destroy(passiveVFXInstance);
            passiveVFXInstance = null;
        }
    }

    private void CleanupActiveDebuffs()
    {
        foreach (ArmorDebuff debuff in activeDebuffs)
        {
            if (debuff.target != null && debuff.target.isAlive)
            {
                debuff.target.armor += debuff.armorReduction;
                Debug.Log($"🔄 Restored {debuff.armorReduction} armor to {debuff.target.unitName}");
            }
        }
        activeDebuffs.Clear();
    }

    private void PlayAbilityAudio(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, audioVolume);
        }
    }

    public AudioClip GetAbilityAudio()
    {
        return abilityStartSound;
    }

    public void OnRoundEnd()
    {
        if (cloudRoutine != null)
        {
            StopCoroutine(cloudRoutine);
            cloudRoutine = null;
        }

        CleanupActiveDebuffs();

        if (activeCloudVFX != null)
        {
            Destroy(activeCloudVFX);
        }

        totalHealingReceived = 0f;

        if (passiveVFXInstance != null)
        {
            Destroy(passiveVFXInstance);
            passiveVFXInstance = null;
        }
    }
}
