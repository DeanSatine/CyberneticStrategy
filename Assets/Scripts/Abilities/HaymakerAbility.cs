using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class HaymakerAbility : MonoBehaviour, IUnitAbility
{
    public UnitAI unitAI;
    private int soulCount = 0;

    [Header("Ability Stats")]
    public float[] slashDamage = { 30f, 40f, 500f };
    public float[] slamDamage = { 125f, 250f, 999f };
    public float[] temporaryArmor = { 160f, 180f, 190f }; // 80/90/95% damage reduction

    [Header("Ability Mechanics")]
    public float slashDuration = 3f;                    // Total slashing time
    public float slashesPerAttackSpeed = 10f;           // 1 slash per 0.10 attack speed (10 = 1.0 AS)
    public float dashSpeed = 8f;                        // Speed of dash movement
    public float slashAnimationSpeedMultiplier = 5f;    // Much faster slashing

    [Header("Passive Clone")]
    public GameObject clonePrefab;
    private GameObject cloneInstance;
    private bool shouldHaveClone = false;

    [Header("VFX")]
    public GameObject slashVFX;        // VFX for each slash
    public GameObject slamVFX;         // VFX for clone slam
    public GameObject dashStartVFX;    // VFX when starting dash
    public GameObject dashEndVFX;      // VFX when arriving at target

    // Ability state tracking
    private bool isPerformingAbility = false;
    private Vector3 originalPosition;
    private float originalArmor;
    private bool isInitialized = false;

    [Header("Audio")]
    public AudioClip autoAttackSound;
    public AudioClip slashSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    private AudioSource audioSource;

    // Simplified clone tracking - only use one system
    private static Dictionary<Team, GameObject> globalActiveClones = new Dictionary<Team, GameObject>();
    private static object cloneLock = new object();

    // Performance optimization - cache expensive operations
    private static HaymakerClone[] cachedClones;
    private static float lastCloneCacheTime;
    private const float CLONE_CACHE_DURATION = 0.1f;

    private void Start()
    {
        isInitialized = true;
        ValidateExistingClones();

        if (unitAI != null)
        {
            unitAI.OnAttackEvent += OnAutoAttack;
        }
    }

    private void OnAutoAttack(UnitAI target)
    {
        PlaySound(autoAttackSound);
    }

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        unitAI.OnStateChanged += HandleStateChanged;
        UnitAI.OnAnyUnitDeath += OnUnitDeath;

        // Store original armor
        originalArmor = unitAI.armor;

        // Audio source setup
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.8f; // 3D spatial sound
        }
    }

    private void OnDestroy()
    {
        unitAI.OnStateChanged -= HandleStateChanged;
        UnitAI.OnAnyUnitDeath -= OnUnitDeath;

        if (unitAI != null)
        {
            unitAI.OnAttackEvent -= OnAutoAttack;
        }

        // Clean up global tracking
        lock (cloneLock)
        {
            if (globalActiveClones.ContainsKey(unitAI.team) && globalActiveClones[unitAI.team] == cloneInstance)
            {
                globalActiveClones.Remove(unitAI.team);
            }
        }

        if (cloneInstance != null)
        {
            DestroyClone();
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
        Debug.Log($"[HaymakerAbility] Round ended, cleaning up for {unitAI.unitName}");

        // Stop any ongoing abilities
        if (isPerformingAbility)
        {
            StopAllCoroutines();
            ResetAbilityState();
        }

        // Reset soul count for new round (optional - remove if you want souls to persist)
        soulCount = 0;

        // Ensure clone state is properly managed
        if (unitAI.currentState == UnitState.Bench)
        {
            shouldHaveClone = false;
            if (cloneInstance != null)
            {
                DestroyClone();
            }
        }
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Safe clone health monitoring with comprehensive null checks
        if (cloneInstance != null)
        {
            var cloneAI = cloneInstance.GetComponent<UnitAI>();
            if (cloneAI == null)
            {
                Debug.LogWarning("[HaymakerAbility] Clone missing UnitAI component, cleaning up");
                cloneInstance = null;
                return;
            }

            if (!cloneAI.isAlive)
            {
                Debug.Log("[HaymakerAbility] Clone died, cleaning up reference");
                cloneInstance = null;

                // Respawn clone if we should still have one
                if (shouldHaveClone && unitAI.currentState == UnitState.BoardIdle)
                {
                    Debug.Log("[HaymakerAbility] Respawning dead clone...");
                    SpawnClone();
                }
            }
        }
        // Check if clone went missing during upgrades
        else if (shouldHaveClone && unitAI.currentState == UnitState.BoardIdle && cloneInstance == null)
        {
            // Clone went missing, try to find it or respawn
            if (HasLostClone())
            {
                Debug.Log("[HaymakerAbility] Clone missing during board state, respawning...");
                SpawnClone();
            }
        }
    }

    // Performance optimized clone cache
    private HaymakerClone[] GetCachedClones()
    {
        if (cachedClones == null || Time.time - lastCloneCacheTime > CLONE_CACHE_DURATION)
        {
            cachedClones = FindObjectsOfType<HaymakerClone>();
            lastCloneCacheTime = Time.time;
        }
        return cachedClones;
    }

    // Simplified clone validation
    private void ValidateExistingClones()
    {
        HaymakerClone[] existingClones = GetCachedClones();

        foreach (var clone in existingClones)
        {
            var existingCloneAI = clone.GetComponent<UnitAI>();
            if (existingCloneAI != null && existingCloneAI.team == unitAI.team)
            {
                // Check if this clone belongs to us based on position proximity
                if (Vector3.Distance(clone.transform.position, transform.position) < 10f)
                {
                    Debug.Log($"[HaymakerAbility] Found existing clone, adopting it: {clone.name}");
                    cloneInstance = clone.gameObject;
                    shouldHaveClone = true;

                    lock (cloneLock)
                    {
                        globalActiveClones[unitAI.team] = cloneInstance;
                    }
                    return;
                }
            }
        }

        Debug.Log($"[HaymakerAbility] No existing clone found for {unitAI.unitName}");
    }

    private bool ShouldSpawnClone()
    {
        lock (cloneLock)
        {
            // Only one clone per team, ever
            if (globalActiveClones.ContainsKey(unitAI.team))
            {
                var existingClone = globalActiveClones[unitAI.team];
                if (existingClone != null)
                {
                    var existingCloneAI = existingClone.GetComponent<UnitAI>();
                    if (existingCloneAI != null && existingCloneAI.isAlive)
                    {
                        Debug.Log($"[HaymakerAbility] Team {unitAI.team} already has living clone: {existingClone.name}");
                        return false;
                    }
                    else
                    {
                        // Clone is dead, remove from tracking
                        globalActiveClones.Remove(unitAI.team);
                    }
                }
                else
                {
                    // Reference is null, remove from tracking
                    globalActiveClones.Remove(unitAI.team);
                }
            }

            // Check if we already have a clone reference
            if (shouldHaveClone && cloneInstance != null)
            {
                var cloneAI = cloneInstance.GetComponent<UnitAI>();
                if (cloneAI != null && cloneAI.isAlive)
                {
                    Debug.Log("[HaymakerAbility] Already have living clone, not spawning");
                    return false;
                }
            }

            Debug.Log("[HaymakerAbility] All checks passed, can spawn clone");
            return true;
        }
    }

    private void HandleStateChanged(UnitState state)
    {
        if (!isInitialized) return;

        Debug.Log($"[HaymakerAbility] {unitAI.unitName} state → {state}");

        if (state == UnitState.BoardIdle)
        {
            // Only spawn clone if we don't have one AND we should have one
            if (!shouldHaveClone && ShouldSpawnClone())
            {
                shouldHaveClone = true;
                SpawnClone();
            }
            else if (shouldHaveClone && cloneInstance == null)
            {
                // Try to recover lost clone
                SpawnClone();
            }
        }
        else if (state == UnitState.Bench || !unitAI.isAlive)
        {
            shouldHaveClone = false;
            if (cloneInstance != null)
                DestroyClone();

            if (isPerformingAbility)
            {
                StopAllCoroutines();
                ResetAbilityState();
            }
        }
    }

    private bool HasLostClone()
    {
        if (cloneInstance == null && shouldHaveClone)
        {
            // Try to find our clone by name or proximity
            HaymakerClone[] allClones = GetCachedClones();
            foreach (var clone in allClones)
            {
                var lostCloneAI = clone.GetComponent<UnitAI>();
                if (lostCloneAI != null && lostCloneAI.team == unitAI.team)
                {
                    if (Vector3.Distance(clone.transform.position, transform.position) < 15f)
                    {
                        Debug.Log($"[HaymakerAbility] Found lost clone, re-adopting: {clone.name}");
                        cloneInstance = clone.gameObject;
                        return false;
                    }
                }
            }
            return true; // Truly lost
        }
        return false;
    }

    private void ResetAbilityState()
    {
        isPerformingAbility = false;

        // Stop any playing audio when ability is reset/interrupted
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Reset animation speed
        if (unitAI.animator)
        {
            unitAI.animator.speed = 1f;
        }

        // Reset armor
        unitAI.armor = originalArmor;

        Debug.Log("[HaymakerAbility] Ability state reset");
    }

    private void SpawnClone()
    {
        lock (cloneLock)
        {
            // Check for existing clones across the entire scene
            HaymakerClone[] existingClones = GetCachedClones();
            foreach (var existingClone in existingClones)
            {
                var existingCloneAI = existingClone.GetComponent<UnitAI>();
                if (existingCloneAI != null && existingCloneAI.team == unitAI.team && existingCloneAI.isAlive)
                {
                    Debug.LogWarning($"🚫 Prevented duplicate clone spawn - {existingClone.name} already exists for team {unitAI.team}");
                    shouldHaveClone = false;
                    return;
                }
            }

            // Double-check we don't already have a clone
            if (cloneInstance != null)
            {
                Debug.LogWarning("[HaymakerAbility] Tried to spawn clone but one already exists!");
                return;
            }

            HexTile targetTile = FindClosestEmptyHexTile();
            if (targetTile == null)
            {
                Debug.LogWarning("[HaymakerAbility] No empty hex tile found for clone!");
                shouldHaveClone = false;
                return;
            }

            Vector3 spawnPos = targetTile.transform.position;
            spawnPos.y = 0.6f;

            cloneInstance = Instantiate(clonePrefab, spawnPos, Quaternion.identity);
            cloneInstance.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            cloneInstance.name = $"{unitAI.unitName} Clone {System.Guid.NewGuid().ToString().Substring(0, 8)}";

            var cloneAI = cloneInstance.GetComponent<UnitAI>();
            if (cloneAI == null)
            {
                Debug.LogError("[HaymakerAbility] Clone prefab missing UnitAI component!");
                Destroy(cloneInstance);
                cloneInstance = null;
                return;
            }

            cloneAI.AssignToTile(targetTile);

            // Scale stats
            cloneAI.maxHealth = unitAI.maxHealth * 0.25f;
            cloneAI.currentHealth = cloneAI.maxHealth;
            cloneAI.attackDamage = unitAI.attackDamage * 0.25f;
            cloneAI.starLevel = 1; // Always lock clones to 1 star
            cloneAI.team = unitAI.team;
            cloneAI.teamID = unitAI.teamID;
            cloneAI.currentMana = 0f;
            cloneAI.isAlive = true;
            cloneAI.unitName = "Haymaker Clone";

            cloneAI.SetState(UnitState.BoardIdle);

            // Remove the original HaymakerAbility to prevent infinite cloning
            var selfAbility = cloneInstance.GetComponent<HaymakerAbility>();
            if (selfAbility) Destroy(selfAbility);

            // Add star lock component to prevent upgrades
            var starLock = cloneInstance.GetComponent<HaymakerCloneStarLock>();
            if (starLock == null)
            {
                starLock = cloneInstance.AddComponent<HaymakerCloneStarLock>();
            }

            // Add HaymakerClone component for soul tracking and description
            var cloneComponent = cloneInstance.GetComponent<HaymakerClone>();
            if (cloneComponent == null)
            {
                cloneComponent = cloneInstance.AddComponent<HaymakerClone>();
            }

            cloneComponent.originalUnitName = unitAI.unitName;
            cloneComponent.canBeSold = false;
            cloneComponent.showInUI = true;
            cloneComponent.masterHaymaker = this;

            // Disable traits that could cause issues
            foreach (var mb in cloneInstance.GetComponents<MonoBehaviour>())
            {
                if (mb.GetType().Name.Contains("Trait") && !(mb is HaymakerClone))
                    mb.enabled = false;
            }

            // Update global tracking
            globalActiveClones[unitAI.team] = cloneInstance;

            GameManager.Instance.RegisterUnit(cloneAI, cloneAI.team == Team.Player);
            ApplySoulBonusesToClone(cloneAI);

            Debug.Log($"[HaymakerAbility] Clone spawned with star lock at {targetTile.gridPosition}");
        }
    }

    public static void CleanupAllDuplicateClones()
    {
        lock (cloneLock)
        {
            // Find all clones in scene
            HaymakerClone[] allClones = FindObjectsOfType<HaymakerClone>();
            Dictionary<Team, List<GameObject>> clonesByTeam = new Dictionary<Team, List<GameObject>>();

            // Group clones by team
            foreach (var clone in allClones)
            {
                var cloneAI = clone.GetComponent<UnitAI>();
                if (cloneAI != null && cloneAI.isAlive)
                {
                    if (!clonesByTeam.ContainsKey(cloneAI.team))
                        clonesByTeam[cloneAI.team] = new List<GameObject>();

                    clonesByTeam[cloneAI.team].Add(clone.gameObject);
                }
            }

            // Destroy duplicates (keep only first clone per team)
            foreach (var teamClones in clonesByTeam)
            {
                if (teamClones.Value.Count > 1)
                {
                    Debug.LogWarning($"Found {teamClones.Value.Count} clones for team {teamClones.Key}, removing duplicates!");

                    // Keep first, destroy rest
                    for (int i = 1; i < teamClones.Value.Count; i++)
                    {
                        var cloneAI = teamClones.Value[i].GetComponent<UnitAI>();
                        if (cloneAI != null)
                        {
                            cloneAI.ClearTile();
                            GameManager.Instance.UnregisterUnit(cloneAI);
                        }
                        Destroy(teamClones.Value[i]);
                    }

                    // Update global tracking with remaining clone
                    globalActiveClones[teamClones.Key] = teamClones.Value[0];
                }
            }
        }
    }

    public static void CleanupAllClonesForNewRound()
    {
        lock (cloneLock)
        {
            Debug.Log("[HaymakerAbility] Cleaning up all clones for new round");

            // Clear global tracking
            globalActiveClones.Clear();

            // Find and properly clean up all existing clones
            HaymakerClone[] existingClones = FindObjectsOfType<HaymakerClone>();
            foreach (var clone in existingClones)
            {
                var cloneAI = clone.GetComponent<UnitAI>();
                if (cloneAI != null)
                {
                    cloneAI.ClearTile();
                    GameManager.Instance.UnregisterUnit(cloneAI);
                }
                Destroy(clone.gameObject);
            }

            Debug.Log($"[HaymakerAbility] Cleaned up {existingClones.Length} clones");
        }
    }

    private void ApplySoulBonusesToClone(UnitAI cloneAI)
    {
        if (soulCount > 0)
        {
            // Apply all accumulated bonuses (5 souls = 1% bonus each)
            int bonusApplications = soulCount / 5;
            if (bonusApplications > 0)
            {
                float healthMultiplier = Mathf.Pow(1.01f, bonusApplications);
                float damageMultiplier = Mathf.Pow(1.01f, bonusApplications);

                cloneAI.maxHealth *= healthMultiplier;
                cloneAI.currentHealth = cloneAI.maxHealth; // Full heal with new max
                cloneAI.attackDamage *= damageMultiplier;

                Debug.Log($"💀 Applied {bonusApplications} soul bonuses to new clone (x{healthMultiplier:F3} stats)");
            }
        }
    }

    private void DestroyClone()
    {
        if (cloneInstance != null)
        {
            var cloneAI = cloneInstance.GetComponent<UnitAI>();

            if (cloneAI != null)
            {
                cloneAI.ClearTile();
                GameManager.Instance.UnregisterUnit(cloneAI);
            }

            // Clean up global tracking before destroying
            lock (cloneLock)
            {
                if (globalActiveClones.ContainsKey(unitAI.team) && globalActiveClones[unitAI.team] == cloneInstance)
                {
                    globalActiveClones.Remove(unitAI.team);
                }
            }

            Destroy(cloneInstance);
            cloneInstance = null;

            Debug.Log($"[HaymakerAbility] Clone destroyed");
        }
    }

    private HexTile FindClosestEmptyHexTile()
    {
        if (BoardManager.Instance == null) return null;

        List<HexTile> playerTiles = BoardManager.Instance.GetPlayerTiles();
        if (playerTiles == null || playerTiles.Count == 0) return null;

        List<HexTile> emptyTiles = new List<HexTile>();
        foreach (var tile in playerTiles)
        {
            if (tile.occupyingUnit == null)
                emptyTiles.Add(tile);
        }

        if (emptyTiles.Count == 0) return null;

        emptyTiles.Sort((a, b) =>
            Vector3.Distance(transform.position, a.transform.position)
            .CompareTo(Vector3.Distance(transform.position, b.transform.position))
        );

        return emptyTiles[0];
    }

    public int SoulCount => soulCount;

    private void OnUnitDeath(UnitAI deadUnit)
    {
        if (!unitAI.isAlive) return;
        if (deadUnit.team == unitAI.team) return;

        soulCount++;

        Debug.Log($"💀 Haymaker gained soul! Total souls: {soulCount}");

        // Apply bonus every 5 souls
        if (soulCount % 5 == 0 && cloneInstance != null)
        {
            var cloneAI = cloneInstance.GetComponent<UnitAI>();
            if (cloneAI != null && cloneAI.isAlive)
            {
                float oldHealth = cloneAI.maxHealth;
                float oldDamage = cloneAI.attackDamage;

                cloneAI.maxHealth *= 1.01f;
                cloneAI.attackDamage *= 1.01f;

                // Update UI immediately
                if (cloneAI.ui != null)
                {
                    cloneAI.ui.UpdateHealth(cloneAI.currentHealth);
                }

                Debug.Log($"💀 Clone empowered! Souls: {soulCount} | Health: {oldHealth:F1} → {cloneAI.maxHealth:F1} | Damage: {oldDamage:F1} → {cloneAI.attackDamage:F1}");
            }
        }
    }

    public string GetSoulCountDisplay()
    {
        return $"Souls: {soulCount}";
    }

    // Active ability
    public void Cast(UnitAI target)
    {
        // Check if we're in the right phase and state
        if (isPerformingAbility)
        {
            Debug.Log("[HaymakerAbility] Already performing ability, ignoring cast");
            return;
        }

        // Only allow ability during combat phase
        if (StageManager.Instance != null && StageManager.Instance.currentPhase != StageManager.GamePhase.Combat)
        {
            Debug.Log("[HaymakerAbility] Cannot cast ability outside of combat phase");
            return;
        }

        // Only allow ability when in combat state
        if (unitAI.currentState != UnitState.Combat && unitAI.currentState != UnitState.BoardIdle)
        {
            Debug.Log($"[HaymakerAbility] Cannot cast ability in state: {unitAI.currentState}");
            return;
        }

        // Trigger ability animation at start of cast
        if (unitAI.animator)
        {
            unitAI.animator.SetTrigger("AbilityTrigger");
            Debug.Log("[HaymakerAbility] Triggered ability animation");
        }

        StartCoroutine(PerformFuryOfSlashes());
    }

    private IEnumerator PerformFuryOfSlashes()
    {
        isPerformingAbility = true;
        originalPosition = transform.position;

        Debug.Log($"⚡ [HaymakerAbility] Starting Fury of Slashes!");

        // PHASE 1: Dash to enemy clump
        Vector3 targetClumpPosition = FindBestClumpPosition(6f);
        if (targetClumpPosition == Vector3.zero)
        {
            // No enemies found, end ability
            Debug.Log("[HaymakerAbility] No enemies found, ending ability");
            isPerformingAbility = false;
            yield break;
        }

        // Spawn dash start VFX
        if (dashStartVFX != null)
        {
            var startVFX = Instantiate(dashStartVFX, transform.position, Quaternion.identity);
            Destroy(startVFX, 2f);
        }

        Debug.Log($"🏃 Phase 1: Dashing to clump at {targetClumpPosition}");

        // Dash to target position
        yield return StartCoroutine(DashToPosition(targetClumpPosition));

        // Spawn dash end VFX
        if (dashEndVFX != null)
        {
            var endVFX = Instantiate(dashEndVFX, transform.position, Quaternion.identity);
            Destroy(endVFX, 2f);
        }

        // PHASE 2: Fury of Slashes (3 seconds)
        Debug.Log("⚔️ Phase 2: Unleashing Fury of Slashes!");
        if (unitAI.animator)
        {
            unitAI.animator.SetTrigger("AbilityTrigger");
            Debug.Log("[HaymakerAbility] Triggered ability animation for slashing phase");
        }

        // Apply massive armor for damage reduction
        int starIndex = Mathf.Clamp(unitAI.starLevel - 1, 0, temporaryArmor.Length - 1);
        unitAI.armor = temporaryArmor[starIndex];
        Debug.Log($"🛡️ Temporary armor active: {temporaryArmor[starIndex]} ({80 + (starIndex * 10)}% damage reduction)");

        // Special 3-star behavior - Massive slashes that sweep across the entire board
        if (unitAI.starLevel == 3)
        {
            Debug.Log("🌟 3-STAR HAYMAKER: Unleashing massive board-wide slashing waves!");

            float slashDmg = slashDamage[Mathf.Clamp(unitAI.starLevel - 1, 0, slashDamage.Length - 1)];

            // Much faster slashing animation for 3-star
            if (unitAI.animator)
            {
                unitAI.animator.speed = slashAnimationSpeedMultiplier * 1.5f;
            }

            // Create 10 massive slashes that sweep across the entire board
            int totalSlashes = 10;
            float boardRadius = 3f; // Cover the entire board

            for (int i = 0; i < totalSlashes; i++)
            {
                // Check if ability should be interrupted
                if (!isPerformingAbility || !unitAI.isAlive)
                {
                    Debug.Log("[HaymakerAbility] 3-star ability interrupted, stopping slashes");
                    break;
                }

                // Check phase during slashing too
                if (StageManager.Instance != null && StageManager.Instance.currentPhase != StageManager.GamePhase.Combat)
                {
                    Debug.Log("[HaymakerAbility] Phase changed during 3-star slashing, stopping ability");
                    break;
                }

                // Calculate sweeping angle (36 degrees apart = 360/10)
                float angle = i * 36f;
                Vector3 slashDirection = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    0f,
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

                // Create multiple slash positions along a line from center to edge of board
                List<Vector3> slashPositions = new List<Vector3>();

                // Create 5 slash points along the line from Haymaker to edge of board
                for (int j = 0; j < 5; j++)
                {
                    float distance = (j + 1) * (boardRadius / 5f);
                    Vector3 slashPos = transform.position + slashDirection * distance;
                    slashPositions.Add(slashPos);
                }

                // Trigger attack animation every few slashes
                if (i % 3 == 0 && unitAI.animator)
                {
                    unitAI.animator.SetTrigger("AbilityTrigger");
                    PlaySound(slashSound);
                }

                // Apply damage along the entire slash line
                foreach (Vector3 slashPos in slashPositions)
                {
                    // Large damage radius for each slash point (covers more area)
                    List<UnitAI> slashTargets = FindEnemiesInRadius(2.5f, slashPos);

                    foreach (UnitAI slashTarget in slashTargets)
                    {
                        slashTarget.TakeDamage(slashDmg);
                        Debug.Log($"💥 Board Slash {i + 1}/{totalSlashes}: {slashDmg} damage to {slashTarget.unitName}");
                    }

                    // Spawn slash VFX at each position along the line
                    if (slashVFX != null)
                    {
                        Vector3 vfxPos = slashPos + Vector3.up * 1f;
                        Quaternion vfxRotation = Quaternion.LookRotation(slashDirection);

                        var slashEffect = Instantiate(slashVFX, vfxPos, vfxRotation);

                        // Make the slash VFX bigger for board-wide effect
                        slashEffect.transform.localScale *= 2.5f;

                        Destroy(slashEffect, 2f);
                    }
                }

                Debug.Log($"⚔️ Board Slash {i + 1}/{totalSlashes}: Swept across {slashPositions.Count} positions");

                // Slightly longer delay between slashes to see the sweeping effect
                yield return new WaitForSeconds(0.15f);
            }

            Debug.Log("🏆 3-STAR HAYMAKER: Board-wide devastation complete!");
        }
        else
        {
            // Normal behavior for 1-star and 2-star Haymakers
            // Calculate number of slashes based on attack speed
            float attackSpeed = unitAI.attackSpeed;
            int totalSlashes = Mathf.RoundToInt(attackSpeed * slashesPerAttackSpeed * slashDuration);
            float timeBetweenSlashes = slashDuration / totalSlashes;

            Debug.Log($"🗡️ Will perform {totalSlashes} slashes over {slashDuration} seconds (1 every {timeBetweenSlashes:F2}s)");

            // Much faster slashing animation
            if (unitAI.animator)
            {
                unitAI.animator.speed = slashAnimationSpeedMultiplier;
            }

            // Perform slashes (original behavior)
            for (int i = 0; i < totalSlashes; i++)
            {
                // Check if ability should be interrupted
                if (!isPerformingAbility || !unitAI.isAlive)
                {
                    Debug.Log("[HaymakerAbility] Ability interrupted, stopping slashes");
                    break;
                }

                // Check phase during slashing too
                if (StageManager.Instance != null && StageManager.Instance.currentPhase != StageManager.GamePhase.Combat)
                {
                    Debug.Log("[HaymakerAbility] Phase changed during slashing, stopping ability");
                    break;
                }

                // Find enemies within 3 hex radius (approximately 3 units)
                List<UnitAI> slashTargets = FindEnemiesInRadius(3f, transform.position);

                if (slashTargets.Count > 0)
                {
                    // Only trigger attack animation every few slashes to avoid animation conflicts
                    if (i % 3 == 0 && unitAI.animator) // Every 3rd slash
                    {
                        unitAI.animator.SetTrigger("AbilityTrigger");
                        PlaySound(slashSound);
                    }

                    // Apply damage to ALL enemies within 3 hex radius
                    float slashDmg = slashDamage[Mathf.Clamp(unitAI.starLevel - 1, 0, slashDamage.Length - 1)];

                    foreach (UnitAI slashTarget in slashTargets)
                    {
                        slashTarget.TakeDamage(slashDmg);
                        Debug.Log($"💥 Slash {i + 1}/{totalSlashes}: {slashDmg} damage to {slashTarget.unitName}");
                    }

                    // Spawn slash VFX around Haymaker's position
                    if (slashVFX != null)
                    {
                        // Create VFX in a circle around Haymaker
                        float angle = (float)i / totalSlashes * 360f; // Distribute around circle
                        Vector3 offset = new Vector3(
                            Mathf.Cos(angle * Mathf.Deg2Rad) * 1.5f, // Slightly larger radius for VFX
                            1f, // Chest level
                            Mathf.Sin(angle * Mathf.Deg2Rad) * 1.5f
                        );
                        Vector3 vfxPos = transform.position + offset;

                        var slashEffect = Instantiate(slashVFX, vfxPos, Quaternion.LookRotation(offset));
                        Destroy(slashEffect, 1f);
                    }

                    Debug.Log($"⚔️ Slash {i + 1}: Hit {slashTargets.Count} enemies within 3 hex radius");
                }
                else
                {
                    Debug.Log($"⚔️ Slash {i + 1}: No enemies within 3 hex radius");
                }

                yield return new WaitForSeconds(timeBetweenSlashes);
            }
        }

        // Reset animation speed and armor (same for both cases)
        if (unitAI.animator)
        {
            unitAI.animator.speed = 1f;
        }
        unitAI.armor = originalArmor;

        // PHASE 3: Clone slam (with clone death check)
        Debug.Log("💥 Phase 3: Clone slam!");

        // Check if we're still in combat and clone is alive before slam
        if (StageManager.Instance != null && StageManager.Instance.currentPhase == StageManager.GamePhase.Combat)
        {
            // Check if clone is still alive before slam
            if (cloneInstance != null)
            {
                var cloneAI = cloneInstance.GetComponent<UnitAI>();
                if (cloneAI != null && cloneAI.isAlive)
                {
                    yield return StartCoroutine(PerformCloneSlamWithRetargeting());
                }
                else
                {
                    Debug.Log("[HaymakerAbility] Clone is dead or missing, skipping slam");
                }
            }
            else
            {
                Debug.Log("[HaymakerAbility] Clone is missing, skipping slam");
            }
        }
        else
        {
            Debug.Log("[HaymakerAbility] Phase changed, skipping clone slam");
        }

        // PHASE 4: Dash back to original position (ONLY if still in combat)
        if (StageManager.Instance != null && StageManager.Instance.currentPhase == StageManager.GamePhase.Combat &&
            unitAI.isAlive && isPerformingAbility)
        {
            Debug.Log("🏃 Phase 4: Dashing back to original position");
            yield return StartCoroutine(DashToPosition(originalPosition));
        }
        else
        {
            Debug.Log("[HaymakerAbility] Skipping return dash - phase changed or unit state invalid");
        }

        isPerformingAbility = false;

        // Stop any playing audio when ability finishes
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        Debug.Log("✅ Fury of Slashes complete!");
    }

    public void OnStarLevelUpgraded()
    {
        Debug.Log($"[HaymakerAbility] {unitAI.unitName} upgraded to {unitAI.starLevel} stars");

        // If we're on the board and should have a clone, ensure clone is maintained
        if (unitAI.currentState == UnitState.BoardIdle && shouldHaveClone)
        {
            // Validate our clone is still good
            if (cloneInstance != null)
            {
                var cloneAI = cloneInstance.GetComponent<UnitAI>();
                if (cloneAI != null && cloneAI.isAlive)
                {
                    // Update clone's HaymakerClone component to point to this ability
                    var cloneComponent = cloneInstance.GetComponent<HaymakerClone>();
                    if (cloneComponent != null)
                    {
                        cloneComponent.masterHaymaker = this;
                    }

                    Debug.Log($"✅ Clone maintained for {unitAI.starLevel}★ {unitAI.unitName} with {soulCount} souls");
                    return;
                }
            }

            // If we got here, we need to respawn the clone
            Debug.Log($"🔄 Respawning clone after star upgrade for {unitAI.unitName}");
            SpawnClone();
        }
    }

    private IEnumerator PerformCloneSlamWithRetargeting()
    {
        // Initial clone death check
        if (cloneInstance == null)
        {
            Debug.Log("[HaymakerAbility] No clone available for slam");
            yield break;
        }

        var cloneAI = cloneInstance.GetComponent<UnitAI>();
        if (cloneAI == null || !cloneAI.isAlive)
        {
            Debug.Log("[HaymakerAbility] Clone is dead, cannot perform slam");
            yield break;
        }

        // Find best target for slam (retarget if needed)
        UnitAI slamTarget = FindClosestEnemy(5f); // Larger search radius for slam

        if (slamTarget == null)
        {
            Debug.Log("[HaymakerAbility] No valid target found for clone slam");
            yield break;
        }

        Debug.Log($"🎯 Clone targeting {slamTarget.unitName} for slam");

        Vector3 cloneStartPos = cloneInstance.transform.position;
        Vector3 slamPosition = slamTarget.transform.position;

        // Clone jumps up and slams down
        float slamDuration = 0.8f;
        float jumpHeight = 3f;

        float elapsed = 0f;
        while (elapsed < slamDuration)
        {
            // Continuous clone death check during slam animation
            if (cloneInstance == null || cloneAI == null || !cloneAI.isAlive)
            {
                Debug.Log("[HaymakerAbility] Clone died during slam animation, aborting slam");
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / slamDuration;

            // Check if target is still valid during slam
            if (slamTarget == null || !slamTarget.isAlive)
            {
                // Retarget mid-slam if needed
                UnitAI newTarget = FindClosestEnemy(5f);
                if (newTarget != null)
                {
                    slamTarget = newTarget;
                    slamPosition = slamTarget.transform.position;
                    Debug.Log($"🔄 Clone retargeted to {slamTarget.unitName} mid-slam");
                }
            }

            // Arc movement for clone
            Vector3 pos = Vector3.Lerp(cloneStartPos, slamPosition, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;
            cloneInstance.transform.position = pos;

            yield return null;
        }

        // Final clone death check before damage application
        if (cloneInstance == null || cloneAI == null || !cloneAI.isAlive)
        {
            Debug.Log("[HaymakerAbility] Clone died before damage application, no slam damage");
            yield break;
        }

        // Final target validation before damage
        if (slamTarget != null && slamTarget.isAlive)
        {
            // Apply slam damage
            float slamDmg = slamDamage[Mathf.Clamp(unitAI.starLevel - 1, 0, slamDamage.Length - 1)];
            slamTarget.TakeDamage(slamDmg);

            // Spawn slam VFX at chest level
            if (slamVFX != null)
            {
                Vector3 slamVFXPosition = slamPosition;
                slamVFXPosition.y += 1.2f; // Chest level

                var slamEffect = Instantiate(slamVFX, slamVFXPosition, Quaternion.identity);
                Destroy(slamEffect, 2f);

                Debug.Log($"💥 Slam VFX spawned at chest level: {slamVFXPosition}");
            }

            Debug.Log($"🌪️ Clone slam: {slamDmg} damage to {slamTarget.unitName}");
        }
        else
        {
            Debug.Log("[HaymakerAbility] Clone slam target became invalid, no damage applied");

            // Still spawn VFX at slam position for visual feedback
            if (slamVFX != null)
            {
                Vector3 slamVFXPosition = slamPosition;
                slamVFXPosition.y += 1.2f; // Chest level even for missed slams

                var slamEffect = Instantiate(slamVFX, slamVFXPosition, Quaternion.identity);
                Destroy(slamEffect, 2f);
            }
        }

        // Final clone check before returning to position
        if (cloneInstance != null && cloneAI != null && cloneAI.isAlive)
        {
            // Return clone to original position
            yield return StartCoroutine(MoveCloneToPosition(cloneStartPos));
        }
        else
        {
            Debug.Log("[HaymakerAbility] Clone died, skipping return movement");
        }
    }

    private IEnumerator DashToPosition(Vector3 targetPosition)
    {
        Vector3 startPos = transform.position;
        float distance = Vector3.Distance(startPos, targetPosition);
        float dashTime = distance / dashSpeed;

        float elapsed = 0f;
        while (elapsed < dashTime)
        {
            // Enhanced interruption checks
            if (!isPerformingAbility || !unitAI.isAlive)
            {
                Debug.Log("[HaymakerAbility] Dash interrupted - ability stopped or unit died");
                yield break;
            }

            // Check phase during dash
            if (StageManager.Instance != null && StageManager.Instance.currentPhase != StageManager.GamePhase.Combat)
            {
                Debug.Log("[HaymakerAbility] Dash interrupted - phase changed to non-combat");
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / dashTime;

            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
    }

    private IEnumerator MoveCloneToPosition(Vector3 targetPos)
    {
        if (cloneInstance == null) yield break;

        Vector3 startPos = cloneInstance.transform.position;
        float moveTime = 0.5f;

        float elapsed = 0f;
        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveTime;

            cloneInstance.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        cloneInstance.transform.position = targetPos;
    }

    private Vector3 FindBestClumpPosition(float searchRadius)
    {
        List<UnitAI> enemies = FindEnemiesInRadius(searchRadius);
        if (enemies.Count == 0) return Vector3.zero;

        // Find enemy with most neighbors (clump center)
        UnitAI bestCenter = enemies[0];
        int bestCount = 0;

        foreach (var enemy in enemies)
        {
            int nearbyCount = 0;
            foreach (var other in enemies)
            {
                if (other == enemy) continue;
                if (Vector3.Distance(enemy.transform.position, other.transform.position) <= 2.5f)
                    nearbyCount++;
            }

            if (nearbyCount > bestCount)
            {
                bestCount = nearbyCount;
                bestCenter = enemy;
            }
        }

        // Find adjacent position instead of clump center
        Vector3 clumpCenter = bestCenter.transform.position;
        Vector3 haymakerPos = transform.position;

        // Calculate direction from Haymaker to clump center
        Vector3 directionToClump = (clumpCenter - haymakerPos).normalized;

        // Find a position 1.5 units away from clump center (approximately 1 hex distance)
        Vector3 targetPosition = clumpCenter - (directionToClump * 1.5f);

        // Make sure the position is valid (not on top of another unit)
        targetPosition = FindNearestValidPosition(targetPosition, clumpCenter, 1.5f);

        Debug.Log($"🎯 Clump center at {clumpCenter}, Haymaker will dash to {targetPosition}");
        return targetPosition;
    }

    private Vector3 FindNearestValidPosition(Vector3 preferredPos, Vector3 clumpCenter, float minDistance)
    {
        // Try the preferred position first
        if (IsPositionValid(preferredPos, minDistance))
        {
            return preferredPos;
        }

        // If preferred position is blocked, try positions in a circle around clump center
        float[] angles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

        foreach (float angle in angles)
        {
            Vector3 offset = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * minDistance,
                0f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * minDistance
            );

            Vector3 testPosition = clumpCenter + offset;

            if (IsPositionValid(testPosition, 1f)) // Allow closer check for alternative positions
            {
                return testPosition;
            }
        }

        // Fallback: return preferred position even if not ideal
        return preferredPos;
    }

    private bool IsPositionValid(Vector3 position, float minDistanceToUnits)
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();

        foreach (var unit in allUnits)
        {
            if (unit == unitAI || !unit.isAlive || unit.currentState == UnitState.Bench)
                continue;

            float distance = Vector3.Distance(position, unit.transform.position);
            if (distance < minDistanceToUnits)
            {
                return false; // Too close to another unit
            }
        }

        return true; // Position is valid
    }

    private UnitAI FindClosestEnemy(float maxDistance)
    {
        List<UnitAI> enemies = FindEnemiesInRadius(maxDistance);
        if (enemies.Count == 0) return null;

        UnitAI closest = enemies[0];
        float minDistance = Vector3.Distance(transform.position, closest.transform.position);

        foreach (var enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = enemy;
            }
        }

        return closest;
    }

    private List<UnitAI> FindEnemiesInRadius(float radius, Vector3? center = null)
    {
        if (center == null) center = transform.position;

        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        List<UnitAI> enemies = new List<UnitAI>();

        foreach (var unit in allUnits)
        {
            if (unit == unitAI || !unit.isAlive || unit.team == unitAI.team || unit.currentState == UnitState.Bench)
                continue;

            if (Vector3.Distance(center.Value, unit.transform.position) <= radius)
                enemies.Add(unit);
        }

        return enemies;
    }
}
