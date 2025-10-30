using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Team
{
    Player,
    Enemy,
    Neutral // optional if you want non-fighting dummies
}

public class UnitAI : MonoBehaviour
{
    [Header("Unit Info")]
    public string unitName;
    public int starLevel = 1;
    public Team team = Team.Player;

    [Header("Stats")]
    public float maxHealth = 100;
    public float currentHealth;
    public float attackDamage = 10;
    public float attackSpeed = 1.0f; // attacks per second
    public float armor = 0;
    public float attackRange = 1.5f;
    public float mana = 0f;
    public float maxMana = 50f;
    [HideInInspector] public float currentMana = 0f;
    [Header("Traits")]
    public List<Trait> traits = new List<Trait>();

    [Header("AI Settings")]
    public bool canMove = true;
    public bool canAttack = true;
    public bool isAlive = true;
    [HideInInspector] public bool isCastingAbility = false;
    public delegate void AttackEvent(UnitAI target);
    public event System.Action<UnitAI> OnAttackEvent;
    public event System.Action<float> OnHealReceived;

    [Header("Combat VFX")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 15f;

    [Tooltip("Define custom forward direction for this unit. Leave empty for default forward.")]
    public Transform facingReference;

    [Header("UI")]
    public GameObject unitUIPrefab;
    public UnitUI ui;
    private Queue<HexTile> currentPath = new Queue<HexTile>();

    private float attackCooldown = 0f;
    [HideInInspector] public Animator animator;
    public Transform currentTarget;

    [Header("Collision Settings")]
    [SerializeField] private float unitRadius = 0.6f; // Detection radius
    [SerializeField] private LayerMask unitLayerMask = -1; // What layers to check for units

    [Header("Pathfinding")]
    [SerializeField] private float pathRecalculateTime = 2f; // How often to recalculate if stuck
    [SerializeField] private float stuckThreshold = 0.1f; // How little movement means "stuck"
    [SerializeField] private int maxPathfindingAttempts = 5; // Max alternative paths to try
                                                             // Pathfinding state tracking
    private Vector3 lastPosition;
    private float timeAtLastPosition;
    private float lastPathRecalculation;
    private bool isUsingAlternativePath = false;

    [Header("Team Settings")]
    public int teamID = 0;   // 0 = Player team, 1 = Enemy team (expand later)
    public static event System.Action<UnitAI> OnAnyUnitDeath;
    public event System.Action<UnitState> OnStateChanged;
    private UnitState _currentState = UnitState.Bench;
    [HideInInspector] public HexTile currentTile;

    [Header("Movement")]
    [SerializeField] private float movementSpeed = 3f;
    [SerializeField] private float stoppingDistance = 1.4f; // How close to get before attacking
    [SerializeField] private float obstacleAvoidance = 2f; // Radius for avoiding other units

    [Tooltip("Random micro-offset so units don't perfectly stack on tile centers.")]
    [SerializeField] private float moveOffsetRange = 0.2f;

    [Header("Animation Settings")]
    [SerializeField] private float deathAnimLength = 1.2f; // match clip length
    private bool isBeingRestored = false; // ✅ NEW: Flag to prevent death interference

    private Vector3 moveOffset;
    private Vector3 currentDestination;
    private bool hasReachedDestination = true;
    [HideInInspector] public HexTile startingTile; // where the unit was before combat

    [Header("Star Upgrade")]
    [Tooltip("Multiplier applied to stats on 2-star (e.g. 1.8)")]

    public float twoStarMultiplier = 1.8f;

    [Tooltip("Multiplier applied to stats on 3-star (e.g. 2.5)")]

    public float threeStarMultiplier = 2.5f;

    [Tooltip("Scale at 2-star (relative to base)")]

    public float twoStarScale = 1.10f;
    [HideInInspector] public float traitBonusMaxHealth = 0f; // Persistent trait bonuses

    [Tooltip("Scale at 3-star (relative to base)")]
    // runtime baseline + modifiers so buffs persist
    [HideInInspector] public float baseMaxHealth;
    [HideInInspector] public float bonusMaxHealth;

    public float threeStarScale = 1.25f;
    private List<GameObject> activeProjectiles = new List<GameObject>();

    private Vector3 baseScale;
    public UnitState currentState
    {
        get => _currentState;
        set
        {
            if (_currentState == value) return;   // only fire on change
            _currentState = value;
            OnStateChanged?.Invoke(_currentState);
        }
    }

    public enum UnitState
    {
        Bench,
        BoardIdle,
        Combat
    }
    public void SetState(UnitState newState)
    {
        if (currentState != newState)
        {
            UnitState oldState = currentState;
            currentState = newState;
            OnStateChanged?.Invoke(newState);

            // ✅ FIX: Ensure proper state transition for movement/attack
            if (newState == UnitState.BoardIdle)
            {
                // Reset movement and attack capabilities when placed on board
                canMove = true;
                canAttack = true;
                attackCooldown = 0f;

                Debug.Log($"✅ {unitName} transitioned {oldState} → {newState} - movement and attack enabled");
            }
            else if (newState == UnitState.Combat)
            {
                // Force fresh target and reset cooldowns at combat start
                currentTarget = FindNearestEnemy();
                attackCooldown = 0f;
                canMove = true;
                canAttack = true;

                Debug.Log($"⚔️ {unitName} entered combat - fresh targeting and abilities enabled");
            }
            else if (newState == UnitState.Bench)
            {
                // Clear target and stop actions when benched
                currentTarget = null;
                canMove = false;
                canAttack = false;

                if (animator)
                {
                    animator.SetBool("IsRunning", false);
                }
            }
        }
    }
    public static void ResetStaticEvents()
    {
        Debug.Log("🧹 Resetting static events in UnitAI");
        OnAnyUnitDeath = null; // Clear all subscribers
    }

    // Also add this to help debug event issues
    public static int GetEventSubscriberCount()
    {
        return OnAnyUnitDeath?.GetInvocationList()?.Length ?? 0;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();

        // Ensure uniform scaling baseline
        baseScale = Vector3.one;
    }


    private void Start()
    {
        float offsetRange = 0.2f; // tweak this for spacing
        moveOffset = new Vector3(Random.Range(-offsetRange, offsetRange), 0, Random.Range(-offsetRange, offsetRange));

        // initialize base/buff values so runtime buffs persist
        baseMaxHealth = maxHealth;
        bonusMaxHealth = 0f;
        RecalculateMaxHealth();

        currentHealth = maxHealth;
        currentMana = 0f;

        if (unitUIPrefab != null)
        {
            GameObject uiObj = Instantiate(unitUIPrefab, transform);
            ui = uiObj.GetComponent<UnitUI>();
            ui.Init(transform, maxHealth, maxMana);
        }
        SetupUnitCollision();
    }

    private void Update()
    {
        if (!isAlive || currentState != UnitState.Combat || isCastingAbility) return;

        if (animator) animator.SetFloat("AttackSpeed", attackSpeed);

        if (currentTarget != null)
            FaceTarget(currentTarget.position);

        attackCooldown -= Time.deltaTime;

        // Retarget if invalid
        if (currentTarget == null
            || !currentTarget.GetComponent<UnitAI>().isAlive
            || currentTarget.GetComponent<UnitAI>().currentState == UnitState.Bench)
        {
            currentTarget = FindNearestEnemy();
            currentPath.Clear();
            if (currentTarget != null) attackCooldown = 0f;
        }

        if (currentTarget != null && canAttack)
        {
            float dist = Vector3.Distance(transform.position, currentTarget.position);

            // ✅ FIX: Stop movement when in attack range
            if (dist <= attackRange)
            {
                // Stop all movement immediately
                if (animator) animator.SetBool("IsRunning", false);
                hasReachedDestination = true;
                currentPath.Clear();

                // Attack on cooldown
                if (attackCooldown <= 0f)
                {
                    Attack(currentTarget);
                    attackCooldown = 1f / attackSpeed;
                }
            }
            // ✅ FIX: Only move if we're NOT in attack range
            else if (canMove)
            {
                MoveTowards(currentTarget.position);
            }
        }
    }

    private void Attack(Transform target)
    {
        if (target == null) return;

        FaceTarget(target.position);

        if (animator) animator.SetTrigger("AttackTrigger");
    }

    private void SpawnProjectile(UnitAI target)
    {
        if (target == null || !target.isAlive) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f;
        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        activeProjectiles.Add(proj); // ✅ track projectile
        StartCoroutine(MoveProjectile(proj, target));
    }

    private void SetupUnitCollision()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;                 // ✅ don’t let physics move the unit
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll; // fully under AI control
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;  // ✅ no pushing, only detection
        }
    }

    private IEnumerator MoveProjectile(GameObject proj, UnitAI target)
    {
        while (proj != null && target != null && target.isAlive)
        {
            if (target.currentState == UnitAI.UnitState.Bench)
            {
                activeProjectiles.Remove(proj);
                Destroy(proj);
                yield break;
            }

            Vector3 dir = (target.transform.position + Vector3.up * 1.2f) - proj.transform.position;
            proj.transform.position += dir.normalized * projectileSpeed * Time.deltaTime;

            if (dir.magnitude < 0.2f)
            {
                target.TakeDamage(attackDamage);
                activeProjectiles.Remove(proj);
                Destroy(proj);
                yield break;
            }

            yield return null;
        }

        if (proj != null)
        {
            activeProjectiles.Remove(proj);
            Destroy(proj);
        }
    }
    private void CleanupProjectiles()
    {
        foreach (var proj in activeProjectiles)
        {
            if (proj != null) Destroy(proj);
        }
        activeProjectiles.Clear();
    }
    public void AssignToTile(HexTile tile)
    {
        if (tile == null) return;

        // clear previous
        ClearTile();

        currentTile = tile;
        tile.occupyingUnit = this;

        // snap to tile center (keep current Y)
        Vector3 p = tile.transform.position;
        p.y = transform.position.y;
        transform.position = p;

        // ✅ remember this as "starting tile" for reset between rounds
        if (currentState != UnitState.Combat)
            startingTile = tile;
    }

    private void FaceTarget(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);

            // If there's a facing reference, apply the offset
            if (facingReference != null)
            {
                // Calculate the offset between the unit's forward and the reference's forward
                Quaternion offset = Quaternion.Inverse(transform.rotation) * facingReference.rotation;
                lookRotation = lookRotation * Quaternion.Inverse(offset);
            }

            // Instant snap for melee, smooth for ranged
            if (attackRange <= 2f)
                transform.rotation = lookRotation;  // Instant
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);  // Smooth
        }
    }

    public void RaiseHealReceivedEvent(float healAmount)
    {
        OnHealReceived?.Invoke(healAmount);
    }

    public void DealAutoAttackDamage()
    {
        Debug.Log($"🎯 [ATTACK EVENT] {unitName} (⭐{starLevel}) FIRING auto attack event! Current target: {(currentTarget != null ? currentTarget.name : "NULL")}");

        if (currentTarget != null && currentTarget.TryGetComponent(out UnitAI enemy))
        {
            if (enemy.currentState == UnitState.Bench)
            {
                Debug.Log($"❌ [ATTACK EVENT] {unitName}: Target {enemy.unitName} is benched, ignoring.");
                return;
            }

            Debug.Log($"🎯 [ATTACK EVENT] {unitName} (⭐{starLevel}) attacking {enemy.unitName} (⭐{enemy.starLevel}) with {attackDamage} damage");

            CyberneticVFX vfx = GetComponent<CyberneticVFX>();

            if (projectilePrefab != null && vfx == null)
            {
                Debug.Log($"🚀 [{unitName}] Using UnitAI projectile system");
                SpawnProjectile(enemy);
            }
            else
            {
                Debug.Log($"⚡ [{unitName}] Dealing IMMEDIATE damage: {attackDamage} to {enemy.unitName} (⭐{enemy.starLevel})");
                Debug.Log($"💥 [BEFORE] {enemy.unitName} health: {enemy.currentHealth}/{enemy.maxHealth}");

                enemy.TakeDamage(attackDamage);

                Debug.Log($"💗 [AFTER] {enemy.unitName} health: {enemy.currentHealth}/{enemy.maxHealth}");
            }

            GainMana(10);
            OnAttackEvent?.Invoke(enemy);
        }
        else
        {
            Debug.Log($"❌ [ATTACK EVENT] {unitName} (⭐{starLevel}) has NO VALID TARGET! currentTarget={currentTarget}");
        }
    }


    public void TakeDamage(float damage)
    {
        Debug.Log($"💥 [TAKE DAMAGE] {unitName} (⭐{starLevel}) receiving {damage} damage (current state: {currentState})");

        // ✅ Benched units cannot take damage
        if (currentState == UnitState.Bench)
        {
            Debug.Log($"🚫 {unitName} is benched and ignored damage.");
            return;
        }

        float reducedDamage = damage * (1f - (armor * 0.005f));
        currentHealth -= reducedDamage;
        GainMana(1);

        Debug.Log($"💗 {unitName} took {reducedDamage} damage. HP: {currentHealth}/{maxHealth}");

        if (ui != null)
            ui.UpdateHealth(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void ClearTile()
    {
        if (currentTile != null)
        {
            if (currentTile.occupyingUnit == this)
                currentTile.occupyingUnit = null;
            currentTile = null;
        }
    }

    private IEnumerator FadeAndDestroy(float fadeDuration = 1.5f)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        List<Material> materials = new List<Material>();

        // Create unique instances of all materials so we don’t modify shared ones
        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                materials.Add(mat);
            }
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            foreach (var mat in materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
            }

            yield return null;
        }

        Destroy(gameObject); // fully gone after fade
    }

    private void Die()
    {
        Debug.Log($"💀 {unitName} attempting to die - isAlive: {isAlive}, isBeingRestored: {isBeingRestored}");
        Debug.Log($"🔍 OnAnyUnitDeath subscribers: {GetEventSubscriberCount()}");

        // ✅ Enhanced guards: prevent multiple death calls and restoration interference
        if (!isAlive || isBeingRestored)
        {
            if (isBeingRestored) Debug.Log($"🚫 {unitName} death blocked - unit is being restored");
            else Debug.Log($"🚫 {unitName} death blocked - already dead");
            return;
        }

        CleanupProjectiles();
        isAlive = false;
        OnAnyUnitDeath?.Invoke(this);

        // ✅ FIXED: Ensure event actually fires
        if (OnAnyUnitDeath != null)
        {
            Debug.Log($"🔥 Firing OnAnyUnitDeath event for {unitName} to {GetEventSubscriberCount()} subscribers");
            OnAnyUnitDeath.Invoke(this);
        }
        else
        {
            Debug.LogError($"❌ OnAnyUnitDeath is null! Cannot fire death event for {unitName}");
        }

        // ✅ TFT-STYLE: Don't destroy or unregister player units during combat
        // They will be restored from snapshots after combat ends
        if (team == Team.Player && StageManager.Instance?.currentPhase == StageManager.GamePhase.Combat)
        {
            Debug.Log($"💀 {unitName} died in combat but will be restored after round ends");

            // ✅ FIX: Play death animation FIRST, then hide after animation completes
            // Clear from tile but keep registered for restoration
            ClearTile();

            // ✅ NEW: Start death sequence but don't hide immediately
            StartCoroutine(HandleCombatDeath());

            return; // ✅ CRITICAL: Don't destroy player units during combat
        }

        // ✅ Normal death for enemies or non-combat scenarios
        GameManager.Instance.UnregisterUnit(this);

        // ✅ Snap to ground (align with tile or y = 0)
        Vector3 pos = transform.position;
        pos.y = currentTile != null ? currentTile.transform.position.y : 0f;
        transform.position = pos;

        // ✅ Reset move offset so unit doesn't "die in the air"
        moveOffset = Vector3.zero;

        if (animator)
        {
            animator.SetTrigger("DieTrigger");
            StartCoroutine(FadeAndDestroy(deathAnimLength));
        }
        else
        {
            StartCoroutine(FadeAndDestroy(1.5f));
        }
    }

    // ✅ ENHANCED: Handle death animation during combat without immediate hiding
    private IEnumerator HandleCombatDeath()
    {
        // ✅ Snap to ground (align with tile or y = 0)
        Vector3 pos = transform.position;
        pos.y = currentTile != null ? currentTile.transform.position.y : 0f;
        transform.position = pos;

        // ✅ Reset move offset so unit doesn't "die in the air"
        moveOffset = Vector3.zero;

        // ✅ Play death animation immediately
        if (animator)
        {
            animator.SetTrigger("DieTrigger");
            Debug.Log($"💀 Playing death animation for {unitName}");

            // Wait for death animation to complete, but check for restoration
            float elapsedTime = 0f;
            while (elapsedTime < deathAnimLength)
            {
                // ✅ NEW: Exit early if unit is being restored
                if (isBeingRestored)
                {
                    Debug.Log($"🔄 Death animation interrupted by restoration for {unitName}");
                    yield break; // Stop the coroutine
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            // No animation, just wait a brief moment but still check for restoration
            float elapsedTime = 0f;
            while (elapsedTime < 0.5f)
            {
                if (isBeingRestored)
                {
                    Debug.Log($"🔄 Death delay interrupted by restoration for {unitName}");
                    yield break;
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        // ✅ Only hide if NOT being restored
        if (!isBeingRestored)
        {
            gameObject.SetActive(false);
            Debug.Log($"👻 {unitName} hidden after death animation completed");
        }
    }
    // ✅ NEW: Public method to handle restoration from combat manager
    public void RestoreFromCombat()
    {
        Debug.Log($"🔄 Starting restoration for {unitName}");

        // ✅ Stop any ongoing death processes
        isBeingRestored = true;
        StopAllCoroutines(); // Stop death animations and other coroutines

        // ✅ Ensure unit is active and alive
        gameObject.SetActive(true);
        isAlive = true;

        // ✅ Reset animation state
        if (animator)
        {
            animator.SetBool("isDead", false);
            animator.ResetTrigger("DieTrigger");
            animator.Rebind();
            animator.Update(0f);
        }

        // ✅ Clear restoration flag after a brief delay
        StartCoroutine(ClearRestorationFlag());
    }

    // ✅ NEW: Clear restoration flag after restoration is complete
    private IEnumerator ClearRestorationFlag()
    {
        yield return new WaitForSeconds(0.1f); // Brief delay to ensure restoration is complete
        isBeingRestored = false;
        Debug.Log($"✅ Restoration completed for {unitName}");
    }

    private IEnumerator DeathSequence(float animLength)
    {
        yield return new WaitForSeconds(animLength);

        if (team == Team.Player)
        {
            // 🔹 Player units are only KO’d, not destroyed
            gameObject.SetActive(false); // hide until prep reset
        }
        else
        {
            // 🔹 Enemies can be fully removed
            StartCoroutine(FadeAndDestroy(2.5f));
        }
    }

    private void GainMana(float amount)
    {
        currentMana += amount;

        if (ui != null)
            ui.UpdateMana(currentMana);   // ✅ refresh bar

        Debug.Log($"[{unitName}] gained {amount} mana → {currentMana}/{maxMana}");

        if (currentMana >= maxMana)
        {
            Debug.Log($"[{unitName}] Mana full! Casting ability...");
            CastAbility();
            currentMana = 0;
            if (ui != null)
                ui.UpdateMana(currentMana);   // reset bar
        }
    }


    private void CastAbility()
    {
        if (isCastingAbility) return; // already casting
        isCastingAbility = true;

        if (currentTarget != null)
            FaceTarget(currentTarget.transform.position);

        if (animator) animator.SetTrigger("AbilityTrigger");

        foreach (var ability in GetComponents<MonoBehaviour>())
        {
            if (ability is IUnitAbility unitAbility)
            {
                UnitAI targetAI = currentTarget?.GetComponent<UnitAI>();

                // ✅ Skip casting on benched or invalid targets
                if (targetAI == null || !targetAI.isAlive || targetAI.currentState == UnitState.Bench)
                {
                    Debug.Log($"🚫 {unitName} tried to cast ability on invalid/benched target.");
                    continue;
                }

                unitAbility.Cast(targetAI);
                break;
            }
        }

        // Optional: reset after a fixed time if ability doesn’t reset itself
        StartCoroutine(EndAbilityAfterDelay(1.5f));
    }

    private IEnumerator EndAbilityAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isCastingAbility = false;
        StopAllCoroutines(); 
    }


    // ✅ Only one interface definition now
    public interface IUnitAbility
    {
        void Cast(UnitAI target);
        void OnRoundEnd();
    }
    public void ResetAbilityState()
    {
        foreach (var ability in GetComponents<MonoBehaviour>())
        {
            if (ability is IUnitAbility unitAbility)
            {
                unitAbility.OnRoundEnd();
            }
        }
    }

    private Transform FindNearestEnemy()
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        Transform bestTarget = null;
        float bestScore = Mathf.Infinity;

        foreach (var unit in allUnits)
        {
            if (unit == this || !unit.isAlive) continue;
            if (unit.team == this.team) continue;
            if (unit.currentState != UnitState.Combat) continue; // ✅ skip benched

            float dist = Vector3.Distance(transform.position, unit.transform.position);

            // ✅ Add targeting bias
            float bias = 0f;

            // Prefer melee units
            if (unit.unitName == "Bop" || unit.unitName == "KillSwitch" || unit.unitName == "Haymaker")
                bias -= 3f; // lower score = more likely to be picked

            float score = dist + bias;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = unit.transform;
            }
        }

        return bestTarget;
    }


    public UnitAI GetCurrentTarget()
    {
        return currentTarget != null ? currentTarget.GetComponent<UnitAI>() : null;
    }

    private Vector3 CalculateMovementDirection(Vector3 destination)
    {
        Vector3 baseDirection = (destination - transform.position).normalized;
        Vector3 avoidanceDirection = CalculateObstacleAvoidance();

        // Combine base movement with obstacle avoidance
        Vector3 finalDirection = (baseDirection + avoidanceDirection * 0.5f).normalized;

        // ✅ FIX: Constrain movement to horizontal plane only (no Y-axis movement)
        finalDirection.y = 0;
        finalDirection = finalDirection.normalized;

        return finalDirection;
    }

    private Vector3 CalculateObstacleAvoidance()
    {
        Vector3 avoidanceVector = Vector3.zero;

        // Find nearby units to avoid
        Collider[] nearbyUnits = Physics.OverlapSphere(transform.position, obstacleAvoidance, unitLayerMask);

        foreach (var unit in nearbyUnits)
        {
            if (unit.transform == transform) continue; // Skip self

            UnitAI otherUnit = unit.GetComponent<UnitAI>();
            if (otherUnit == null || !otherUnit.isAlive) continue;

            // Don't avoid our target (we want to get close to attack)
            if (currentTarget != null && unit.transform == currentTarget) continue;

            Vector3 directionAway = transform.position - unit.transform.position;

            // ✅ FIX: Constrain avoidance to horizontal plane only
            directionAway.y = 0;

            float distance = directionAway.magnitude;

            if (distance < obstacleAvoidance && distance > 0.1f)
            {
                // Stronger avoidance for closer units
                float avoidanceStrength = (obstacleAvoidance - distance) / obstacleAvoidance;
                avoidanceVector += directionAway.normalized * avoidanceStrength;
            }
        }

        // ✅ FIX: Ensure avoidance vector is horizontal only
        avoidanceVector.y = 0;
        return avoidanceVector.normalized;
    }

    private Vector3 CalculateIdealDestination(Vector3 targetPos)
    {
        if (currentTarget == null) return targetPos;

        // For melee units: get close but maintain stopping distance
        if (attackRange <= 1.6f)
        {
            Vector3 dirToTarget = (targetPos - transform.position).normalized;
            // ✅ FIX: Constrain direction to horizontal plane
            dirToTarget.y = 0;
            dirToTarget = dirToTarget.normalized;

            Vector3 destination = targetPos - dirToTarget * stoppingDistance;
            // ✅ FIX: Keep same Y position as current unit
            destination.y = transform.position.y;
            return destination;
        }
        else
        {
            // For ranged units: maintain attack range distance
            Vector3 dirToTarget = (targetPos - transform.position).normalized;
            // ✅ FIX: Constrain direction to horizontal plane
            dirToTarget.y = 0;
            dirToTarget = dirToTarget.normalized;

            Vector3 destination = targetPos - dirToTarget * (attackRange * 0.8f);
            // ✅ FIX: Keep same Y position as current unit
            destination.y = transform.position.y;
            return destination;
        }
    }


    private void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.001f)
        {
            direction.y = 0f; // Keep upright
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }
    }
    public void ResetAfterCombat()
    {
        gameObject.SetActive(true);
        isAlive = true;
        // ✅ FIX: Only reset temporary bonuses, preserve trait bonuses
        float tempBonuses = bonusMaxHealth - traitBonusMaxHealth;
        bonusMaxHealth = traitBonusMaxHealth; // Keep only trait bonuses

        currentHealth = maxHealth; // Full heal
        currentMana = 0f;

        SetState(UnitState.BoardIdle);
        currentTarget = null;
        attackCooldown = 0f;
        isCastingAbility = false;

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.ResetTrigger("AttackTrigger");
            animator.ResetTrigger("AbilityTrigger");
        }

        if (ui != null)
        {
            ui.UpdateHealth(currentHealth);
            ui.UpdateMana(currentMana);
            ui.gameObject.SetActive(true);
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        this.enabled = true;

        Debug.Log($"✅ {unitName} reset for new round - HP: {currentHealth}/{maxHealth}, Mana: {currentMana}/{maxMana}");
    }


    public void FullResetToPrep(HexTile tile)
    {
        // ✅ FIX: Also clear bonuses in full reset
        bonusMaxHealth = 0f;
        RecalculateMaxHealth();

        currentHealth = maxHealth;
        currentMana = 0;
        isAlive = true;
        isCastingAbility = false;

        AssignToTile(tile);
        SetState(UnitState.BoardIdle);
        currentTarget = null;

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.ResetTrigger("AttackTrigger");
            animator.ResetTrigger("AbilityTrigger");
            animator.Rebind();
            animator.Update(0f);
        }

        if (ui != null)
        {
            ui.UpdateHealth(currentHealth);
            ui.UpdateMana(currentMana);
            ui.gameObject.SetActive(true);
        }

        this.enabled = true;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
        CleanupProjectiles();
    }


    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1)) // right click
        {
            Debug.Log($"Right-clicked {unitName}");
            UnitInfoPanelManager.Instance.ShowPanel(this);
        }
    }


    private void UpdateCurrentTile()
    {
        // Find the closest hex tile for game logic purposes
        HexTile closestTile = BoardManager.Instance.GetTileFromWorld(transform.position);

        if (closestTile != null && closestTile != currentTile)
        {
            // Clear previous tile
            if (currentTile != null && currentTile.occupyingUnit == this)
                currentTile.occupyingUnit = null;

            // Only claim the new tile if it's free or we're just passing through
            if (closestTile.occupyingUnit == null)
            {
                currentTile = closestTile;
                closestTile.occupyingUnit = this;
            }
            // If tile is occupied, we're just passing through - don't claim it
        }
    }
    public string GetAbilityDescription()
    {
        return UnitAbilityDescriptions.GetDescription(this);
    }

    public void ForceResetMovementState()
    {
        currentPath.Clear();
        hasReachedDestination = true;
        currentDestination = transform.position;

        if (animator)
        {
            animator.SetBool("IsRunning", false);
        }

        // Clear any movement velocity
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
        }

        Debug.Log($"🔄 {unitName} movement state forcefully reset");
    }

    public void ResetPathfinding()
    {
        currentPath.Clear();
        hasReachedDestination = false;
        lastPathRecalculation = 0f;
        isUsingAlternativePath = false;

        Debug.Log($"🗺️ {unitName} pathfinding system reset");
    }

    private void MoveTowards(Vector3 targetPos)
    {
        if (currentTile == null) return;

        // Calculate ideal destination (near target, not necessarily on grid)
        Vector3 idealDestination = CalculateIdealDestination(targetPos);

        // Only recalculate path if destination changed significantly
        if (Vector3.Distance(idealDestination, currentDestination) > 1.0f)
        {
            currentDestination = idealDestination;
            hasReachedDestination = false;
        }

        if (!hasReachedDestination)
        {
            // Smooth movement toward destination with obstacle avoidance
            Vector3 moveDirection = CalculateMovementDirection(currentDestination);

            if (moveDirection != Vector3.zero)
            {
                // ✅ Check for collisions before moving
                Vector3 newPosition = transform.position + moveDirection * movementSpeed * Time.deltaTime;

                // ✅ Simple collision check - don't move if too close to other units
                bool canMove = true;
                Collider[] nearbyUnits = Physics.OverlapSphere(newPosition, unitRadius, unitLayerMask);

                foreach (var col in nearbyUnits)
                {
                    UnitAI otherUnit = col.GetComponent<UnitAI>();
                    if (otherUnit != null && otherUnit != this && otherUnit.isAlive)
                    {
                        // Don't move if it would put us too close to another unit
                        if (currentTarget == null || col.transform != currentTarget)
                        {
                            canMove = false;
                            break;
                        }
                    }
                }

                if (canMove)
                {
                    // Move smoothly in world space
                    transform.position = newPosition;

                    // Face movement direction
                    FaceDirection(moveDirection);

                    // Update which hex tile we're closest to (for game logic)
                    UpdateCurrentTile();

                    if (animator) animator.SetBool("IsRunning", true);
                }
                else
                {
                    // ✅ Can't move directly - try alternative direction
                    Vector3 alternativeDir = FindAlternativeDirection(moveDirection);
                    if (alternativeDir != Vector3.zero)
                    {
                        Vector3 altPosition = transform.position + alternativeDir * movementSpeed * Time.deltaTime;
                        transform.position = altPosition;
                        FaceDirection(alternativeDir);
                        UpdateCurrentTile();
                        if (animator) animator.SetBool("IsRunning", true);
                    }
                    else
                    {
                        if (animator) animator.SetBool("IsRunning", false);
                    }
                }
            }

            // Check if we've reached our destination
            if (Vector3.Distance(transform.position, currentDestination) < 0.3f)
            {
                hasReachedDestination = true;
                if (animator) animator.SetBool("IsRunning", false);
            }
        }
        else
        {
            // ✅ Try full pathfinding as last resort
            if (currentTile != null && currentTarget != null)
            {
                HexTile targetTile = BoardManager.Instance.GetTileFromWorld(currentTarget.position);
                if (targetTile != null)
                {
                    List<HexTile> path = BoardManager.Instance.FindPath(currentTile, targetTile);
                    if (path != null && path.Count > 1)
                    {
                        Vector3 nextStep = path[1].transform.position;
                        nextStep.y = transform.position.y;

                        Vector3 pathDir = (nextStep - transform.position).normalized;
                        transform.position += pathDir * movementSpeed * Time.deltaTime;

                        FaceDirection(pathDir);
                        UpdateCurrentTile();

                        if (animator) animator.SetBool("IsRunning", true);
                    }
                }
            }
        }
    }
    public void RecalculateMaxHealth()
    {
        // effective max
        maxHealth = baseMaxHealth + bonusMaxHealth;

        // ✅ REMOVED: Don't cap current health to allow overheal
        // ❌ REMOVED: if (currentHealth > maxHealth) currentHealth = maxHealth;

        // immediately sync unit UI (if present)
        if (ui != null)
        {
            ui.SetMaxHealth(maxHealth);
            ui.UpdateHealth(currentHealth);
        }
    }

    // ⭐ Upgrade Star Level (called from GameManager when merging)
    public void UpgradeStarLevel()
    {
        if (starLevel >= 3)
        {
            Debug.Log($"[Upgrade] {unitName} already at max star ({starLevel})");
            return;
        }

        int oldStar = starLevel;
        starLevel++;

        float multiplier = (starLevel == 2) ? twoStarMultiplier : threeStarMultiplier;

        // scale base and existing bonus so relative bonuses remain consistent
        baseMaxHealth = Mathf.Round(baseMaxHealth * multiplier);
        bonusMaxHealth = Mathf.Round(bonusMaxHealth * multiplier);

        attackDamage = Mathf.Round(attackDamage * multiplier);

        // recompute effective max and sync UI
        RecalculateMaxHealth();

        currentHealth = maxHealth;


        // 📏 Force scaling rules by unit name
        // 📏 Force scaling rules by unit name
        if (starLevel == 2)
        {
            Vector3 desiredWorldScale;
            switch (unitName)
            {
                case "Needlebot":
                    desiredWorldScale = new Vector3(0.8f, 0.85f, 0.8f);
                    break;
                case "BOP":
                    desiredWorldScale = new Vector3(1.3f, 1.05f, 1.3f);
                    break;
                case "ManaDrive":
                    desiredWorldScale = new Vector3(0.75f, 0.60f, 0.75f);
                    break;
                case "KillSwitch":
                    desiredWorldScale = new Vector3(1.3f, 0.90f, 1.3f);
                    break;
                case "Haymaker":
                    desiredWorldScale = new Vector3(1.3f, 1.05f, 1.3f);
                    break;
                default:
                    desiredWorldScale = baseScale * 1.1f; // fallback for other units
                    break;
            }

            // ✅ Account for parent scaling (CharacterTile has very small Y scale)
            if (transform.parent != null)
            {
                Vector3 parentScale = transform.parent.lossyScale;
                transform.localScale = new Vector3(
                    desiredWorldScale.x / parentScale.x,
                    desiredWorldScale.y / parentScale.y,
                    desiredWorldScale.z / parentScale.z
                );
            }
            else
            {
                transform.localScale = desiredWorldScale;
            }
        }
        else if (starLevel == 3)
        {
            Vector3 desiredWorldScale;
            switch (unitName)
            {
                case "Needlebot":
                    desiredWorldScale = new Vector3(1f, 1f, 1f);
                    break;
                case "Bop":
                    desiredWorldScale = new Vector3(1.25f, 1.25f, 1.25f);
                    break;
                case "ManaDrive":
                    desiredWorldScale = new Vector3(1f, 1f, 1f);
                    break;
                case "KillSwitch":
                    desiredWorldScale = new Vector3(1.3f, 1.3f, 1.3f);
                    break;
                case "Haymaker":
                    desiredWorldScale = new Vector3(1.40f, 1.40f, 1.40f);
                    break;
                default:
                    desiredWorldScale = baseScale * 1.25f; // fallback for other units
                    break;
            }

            // ✅ Account for parent scaling (CharacterTile has very small Y scale)
            if (transform.parent != null)
            {
                Vector3 parentScale = transform.parent.lossyScale;
                transform.localScale = new Vector3(
                    desiredWorldScale.x / parentScale.x,
                    desiredWorldScale.y / parentScale.y,
                    desiredWorldScale.z / parentScale.z
                );
            }
            else
            {
                transform.localScale = desiredWorldScale;
            }
        }


        // Spawn VFX if prefab assigned
        if (GameManager.Instance != null && GameManager.Instance.starUpVFXPrefab != null)
        {
            var vfx = Instantiate(GameManager.Instance.starUpVFXPrefab, transform.position + Vector3.up * 1.2f, Quaternion.identity);
            Destroy(vfx, 0.8f);
        }

        // after this line:
        currentHealth = maxHealth;

        // ✅ Fix: make sure the UnitUI knows about the new max health
        if (ui != null)
        {
            // Reset both stored max and current values
            ui.SetMaxHealth(maxHealth);
            ui.UpdateHealth(currentHealth);
        }

        Debug.Log($"[Upgrade] {unitName} {oldStar}★ -> {starLevel}★. HP: {maxHealth}, AD: {attackDamage}");

        // ✅ FIX: Notify HaymakerAbility about star upgrade
        var haymakerAbility = GetComponent<HaymakerAbility>();
        if (haymakerAbility != null)
        {
            haymakerAbility.OnStarLevelUpgraded();
        }

        Debug.Log($"[Upgrade] {unitName} {oldStar}★ -> {starLevel}★. HP: {maxHealth}, AD: {attackDamage}");
    }


    // ✅ Helper method for alternative movement
    private Vector3 FindAlternativeDirection(Vector3 blockedDirection)
    {
        // Try perpendicular directions
        Vector3[] alternatives = {
        Vector3.Cross(blockedDirection, Vector3.up).normalized,
        Vector3.Cross(Vector3.up, blockedDirection).normalized
    };

        foreach (var dir in alternatives)
        {
            Vector3 testPos = transform.position + dir * unitRadius;
            Collider[] check = Physics.OverlapSphere(testPos, unitRadius * 0.8f, unitLayerMask);

            bool isClear = true;
            foreach (var col in check)
            {
                UnitAI otherUnit = col.GetComponent<UnitAI>();
                if (otherUnit != null && otherUnit != this && otherUnit.isAlive)
                {
                    isClear = false;
                    break;
                }
            }

            if (isClear) return dir;
        }

        return Vector3.zero;
    }
}
