using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class CoreweaverAbility : MonoBehaviour, IUnitAbility
{
    private UnitAI unitAI;

    [Header("Passive: Stationary Mana Generator")]
    [Tooltip("Mana regenerated per second")]
    public float[] baseManaPerSecond = { 5f, 6f, 12f };

    [Tooltip("How much attack speed converts to mana regen (0.5 AS = 1 mana/sec)")]
    public float attackSpeedToManaConversion = 2f;

    [Header("Active: Meteor & Tornado Storm")]
    [Tooltip("Storm duration at 1/2/3 stars")]
    public float[] stormDuration = { 4f, 6f, 60f };

    [Tooltip("Meteor damage at 1/2/3 stars")]
    public float[] meteorDamage = { 250f, 600f, 9999f };

    [Tooltip("Tornado damage at 1/2/3 stars")]
    public float[] tornadoDamage = { 100f, 200f, 2000f };

    [Tooltip("Tornado stun duration")]
    public float tornadoStunDuration = 1f;

    [Tooltip("How often meteors spawn (seconds)")]
    public float meteorSpawnInterval = 0.3f;

    [Tooltip("How many tornados to spawn simultaneously")]
    public int tornadoCount = 4;

    [Tooltip("How fast tornados orbit around the center")]
    public float tornadoOrbitSpeed = 45f;

    [Tooltip("Radius of tornado orbit (in hex tiles)")]
    public float tornadoOrbitRadius = 14f;

    [Tooltip("Height offset for tornados above ground")]
    public float tornadoHeightOffset = 1.5f;


    [Header("AOE Damage Radius")]
    [Tooltip("AOE radius for meteor impact damage")]
    public float meteorAOERadius = 3.5f;

    [Tooltip("AOE radius for tornado continuous damage")]
    public float tornadoAOERadius = 3f;

    [Header("Meteor Spread Settings")]
    [Tooltip("Initial meteor spawn radius at start of storm")]
    public float meteorStartRadius = 1f;

    [Tooltip("Final meteor spawn radius at end of storm (in hex tiles)")]
    public float meteorEndRadius = 24f;

    [Tooltip("Number of meteors to spawn per wave")]
    public int meteorsPerWave = 2;

    [Header("VFX")]
    public GameObject meteorVFX;
    public GameObject tornadoVFX;
    public GameObject castVFX;

    [Header("Audio")]
    public AudioClip castSound;
    public AudioClip meteorImpactSound;
    public AudioClip tornadoSound;
    [Range(0f, 1f)]
    public float audioVolume = 0.7f;

    private AudioSource audioSource;
    private Coroutine stormCoroutine;
    private Coroutine manaRegenCoroutine;
    private bool isStormActive = false;
    private List<GameObject> activeTornados = new List<GameObject>();
    private List<GameObject> activeMeteors = new List<GameObject>();

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

    private void OnEnable()
    {
        if (unitAI != null)
        {
            unitAI.canMove = false;
            unitAI.canAttack = false;
            StartManaRegeneration();
        }
    }

    private void Start()
    {
        if (unitAI != null)
        {
            unitAI.canMove = false;
            unitAI.canAttack = false;
            StartManaRegeneration();
        }
    }

    private void StartManaRegeneration()
    {
        if (manaRegenCoroutine != null)
        {
            StopCoroutine(manaRegenCoroutine);
        }
        manaRegenCoroutine = StartCoroutine(PassiveManaGeneration());
        Debug.Log($"🔋 {unitAI.unitName} mana regeneration started");
    }

    private IEnumerator PassiveManaGeneration()
    {
        while (unitAI != null && unitAI.isAlive)
        {
            if (unitAI.currentState == UnitState.Combat)
            {
                int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, 2);
                float baseMana = baseManaPerSecond[starIndex];
                float manaPerSecond = baseMana + (unitAI.attackSpeed * attackSpeedToManaConversion);
                float manaGained = manaPerSecond * Time.deltaTime;
                unitAI.GainMana(manaGained);
            }
            yield return null;
        }
    }

    public void Cast(UnitAI target)
    {
        if (!unitAI.isAlive || isStormActive) return;

        unitAI.currentMana = 0f;
        if (unitAI.ui != null)
        {
            unitAI.ui.UpdateMana(unitAI.currentMana);
        }

        PlaySound(castSound);

        if (castVFX != null)
        {
            GameObject vfx = Instantiate(castVFX, transform.position + Vector3.up * 2f, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        if (unitAI.animator)
        {
            unitAI.animator.SetTrigger("AbilityTrigger");
        }

        int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, 2);
        float duration = stormDuration[starIndex];

        stormCoroutine = StartCoroutine(MeteorTornadoStorm(duration));
    }

    public void OnRoundEnd()
    {
        if (stormCoroutine != null)
        {
            StopCoroutine(stormCoroutine);
            stormCoroutine = null;
        }

        CleanupAllVFX();
        isStormActive = false;

        if (unitAI != null)
        {
            unitAI.isCastingAbility = false;
        }

        StartManaRegeneration();

        Debug.Log($"🔋 {unitAI?.unitName} ability reset for new round");
    }


    private void CleanupAllVFX()
    {
        foreach (GameObject tornado in activeTornados)
        {
            if (tornado != null)
            {
                Destroy(tornado);
            }
        }
        activeTornados.Clear();

        foreach (GameObject meteor in activeMeteors)
        {
            if (meteor != null)
            {
                Destroy(meteor);
            }
        }
        activeMeteors.Clear();
    }


    private IEnumerator MeteorTornadoStorm(float duration)
    {
        isStormActive = true;
        float elapsed = 0f;
        float nextMeteor = 0f;

        Debug.Log($"⚡ {unitAI.unitName} unleashes a storm for {duration} seconds covering 16 hex radius!");

        Vector3 initialBoardCenter = GetBoardCenter();
        SpawnOrbitingTornados(initialBoardCenter, duration);

        while (elapsed < duration && unitAI != null && unitAI.isAlive && unitAI.currentState == UnitState.Combat)
        {
            elapsed += Time.deltaTime;
            nextMeteor -= Time.deltaTime;

            if (nextMeteor <= 0f)
            {
                float spreadProgress = elapsed / duration;
                Vector3 currentCenter = GetBoardCenter();

                for (int i = 0; i < meteorsPerWave; i++)
                {
                    SpawnMeteor(currentCenter, spreadProgress);
                }

                nextMeteor = meteorSpawnInterval;
            }

            yield return null;
        }

        CleanupAllVFX();
        isStormActive = false;
        unitAI.isCastingAbility = false;

        if (unitAI.currentState != UnitState.Combat)
        {
            Debug.Log($"⚡ {unitAI.unitName}'s storm cancelled - round ended.");
        }
        else
        {
            Debug.Log($"⚡ {unitAI.unitName}'s storm has ended.");
        }
    }


    private Vector3 GetBoardCenter()
    {
        if (unitAI.currentTarget != null)
        {
            UnitAI targetUnit = unitAI.currentTarget.GetComponent<UnitAI>();
            if (targetUnit != null && targetUnit.isAlive)
            {
                return unitAI.currentTarget.position;
            }
        }

        if (BoardManager.Instance == null) return Vector3.zero;

        List<HexTile> enemyTiles = BoardManager.Instance.GetEnemyTiles();
        if (enemyTiles.Count == 0) return Vector3.zero;

        List<HexTile> tilesWithEnemies = new List<HexTile>();
        foreach (HexTile tile in enemyTiles)
        {
            if (tile.occupyingUnit != null && tile.occupyingUnit.isAlive && tile.occupyingUnit.teamID != unitAI.teamID)
            {
                tilesWithEnemies.Add(tile);
            }
        }

        if (tilesWithEnemies.Count > 0)
        {
            Vector3 center = Vector3.zero;
            foreach (HexTile tile in tilesWithEnemies)
            {
                center += tile.transform.position;
            }
            center /= tilesWithEnemies.Count;
            return center;
        }

        return Vector3.zero;
    }


    private void SpawnOrbitingTornados(Vector3 center, float duration)
    {
        if (tornadoVFX == null) return;

        PlaySound(tornadoSound);

        for (int i = 0; i < tornadoCount; i++)
        {
            float angleOffset = (360f / tornadoCount) * i;
            StartCoroutine(OrbitingTornado(center, angleOffset, duration));
        }
    }

    private IEnumerator OrbitingTornado(Vector3 center, float startAngle, float duration)
    {
        GameObject tornado = null;
        if (tornadoVFX != null)
        {
            tornado = Instantiate(tornadoVFX, center, Quaternion.identity);
            tornado.transform.localScale = Vector3.one;
            activeTornados.Add(tornado);
        }

        float elapsed = 0f;
        float currentAngle = startAngle;

        int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, 2);
        float damage = tornadoDamage[starIndex];

        float damageTickInterval = 0.5f;
        float nextDamageTick = 0f;

        while (elapsed < duration && unitAI != null && unitAI.isAlive)
        {
            elapsed += Time.deltaTime;
            currentAngle += tornadoOrbitSpeed * Time.deltaTime;

            Vector3 updatedCenter = GetBoardCenter();

            float angleRad = currentAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(angleRad) * tornadoOrbitRadius,
                tornadoHeightOffset,
                Mathf.Sin(angleRad) * tornadoOrbitRadius
            );

            Vector3 tornadoPos = updatedCenter + offset;

            if (tornado != null)
            {
                tornado.transform.position = tornadoPos;
                tornado.transform.localScale = Vector3.one;
            }

            nextDamageTick -= Time.deltaTime;
            if (nextDamageTick <= 0f)
            {
                ApplyTornadoDamageAOE(tornadoPos, damage);
                nextDamageTick = damageTickInterval;
            }

            yield return null;
        }

        if (tornado != null)
        {
            activeTornados.Remove(tornado);
            Destroy(tornado);
        }
    }

    private void ApplyTornadoDamageAOE(Vector3 position, float damage)
    {
        Collider[] hits = Physics.OverlapSphere(position, tornadoAOERadius);
        HashSet<UnitAI> hitEnemies = new HashSet<UnitAI>();

        foreach (Collider col in hits)
        {
            UnitAI enemy = col.GetComponent<UnitAI>();
            if (enemy != null && enemy.teamID != unitAI.teamID && enemy.isAlive && !hitEnemies.Contains(enemy))
            {
                unitAI.DealMagicDamageWithAP(enemy, damage, 1.5f); StartCoroutine(StunEnemy(enemy, tornadoStunDuration));
                hitEnemies.Add(enemy);
            }
        }
    }

    private void SpawnMeteor(Vector3 center, float spreadProgress)
    {
        if (BoardManager.Instance == null) return;

        List<HexTile> enemyTiles = BoardManager.Instance.GetEnemyTiles();
        if (enemyTiles.Count == 0) return;

        float currentRadius = Mathf.Lerp(meteorStartRadius, meteorEndRadius, spreadProgress);

        Vector3 randomOffset = Random.insideUnitSphere * currentRadius;
        randomOffset.y = 0f;

        Vector3 targetPos = center + randomOffset;

        HexTile closestTile = BoardManager.Instance.GetTileFromWorld(targetPos);
        if (closestTile != null && closestTile.owner == TileOwner.Enemy)
        {
            targetPos = closestTile.transform.position;
        }

        Vector3 spawnPos = targetPos + Vector3.up * 15f;
        StartCoroutine(MeteorStrike(spawnPos, targetPos));
    }

    private IEnumerator MeteorStrike(Vector3 startPos, Vector3 targetPos)
    {
        GameObject meteor = null;
        if (meteorVFX != null)
        {
            meteor = Instantiate(meteorVFX, startPos, Quaternion.identity);
            activeMeteors.Add(meteor);
        }

        float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration && unitAI != null && unitAI.isAlive && unitAI.currentState == UnitState.Combat)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (meteor != null)
            {
                meteor.transform.position = Vector3.Lerp(startPos, targetPos + Vector3.up * 0.5f, t);
            }

            yield return null;
        }

        if (unitAI == null || !unitAI.isAlive || unitAI.currentState != UnitState.Combat)
        {
            if (meteor != null)
            {
                activeMeteors.Remove(meteor);
                Destroy(meteor);
            }
            yield break;
        }

        PlaySound(meteorImpactSound);

        if (meteor != null)
        {
            activeMeteors.Remove(meteor);
            Destroy(meteor, 2f);
        }

        ApplyMeteorDamageAOE(targetPos);
    }


    private void ApplyMeteorDamageAOE(Vector3 impactPos)
    {
        int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, 2);
        float damage = meteorDamage[starIndex];

        Collider[] hits = Physics.OverlapSphere(impactPos, meteorAOERadius);
        HashSet<UnitAI> hitEnemies = new HashSet<UnitAI>();

        int hitCount = 0;
        foreach (Collider col in hits)
        {
            UnitAI enemy = col.GetComponent<UnitAI>();
            if (enemy != null && enemy.teamID != unitAI.teamID && enemy.isAlive && !hitEnemies.Contains(enemy))
            {
                unitAI.DealMagicDamageWithAP(enemy, damage, 1.5f);
                hitEnemies.Add(enemy);
                hitCount++;
            }
        }

        if (hitCount > 0)
        {
            Debug.Log($"☄️ Meteor dealt {damage} AOE damage to {hitCount} enemies in {meteorAOERadius} radius!");
        }
    }

    private IEnumerator StunEnemy(UnitAI enemy, float duration)
    {
        bool wasAbleToMove = enemy.canMove;
        bool wasAbleToAttack = enemy.canAttack;

        enemy.canMove = false;
        enemy.canAttack = false;

        if (enemy.animator)
        {
            enemy.animator.SetBool("IsRunning", false);
        }

        yield return new WaitForSeconds(duration);

        if (enemy != null && enemy.isAlive)
        {
            enemy.canMove = wasAbleToMove;
            enemy.canAttack = wasAbleToAttack;
        }
    }

    private void OnDisable()
    {
        OnRoundEnd();

        if (manaRegenCoroutine != null)
        {
            StopCoroutine(manaRegenCoroutine);
            manaRegenCoroutine = null;
        }
    }
}
