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

    [Header("Combat VFX")]
    public GameObject projectilePrefab; // assign for ranged units
    public Transform firePoint;         // optional: empty child object for projectile spawn
    public float projectileSpeed = 15f;

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

    [Tooltip("Scale at 3-star (relative to base)")]

    public float threeStarScale = 1.25f;

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
            currentState = newState;
            OnStateChanged?.Invoke(newState);
        }
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
        if (!isAlive) return;
        if (currentState != UnitState.Combat) return;
        if (!isAlive || currentState != UnitState.Combat || isCastingAbility) return;

        // ✅ Keep animator synced with attack speed
        if (animator) animator.SetFloat("AttackSpeed", attackSpeed);

        if (currentTarget != null)
            FaceTarget(currentTarget.position);

        attackCooldown -= Time.deltaTime;

        if (currentTarget == null || !currentTarget.GetComponent<UnitAI>().isAlive)
        {
            currentTarget = FindNearestEnemy();
            currentPath.Clear(); // ✅ recalc path on new target
        }

        if (currentTarget != null && canAttack)
        {
            float dist = Vector3.Distance(transform.position, currentTarget.position);

            // Use actual distance instead of hex-based logic
            if (dist <= attackRange)
            {
                if (attackCooldown <= 0f)
                {
                    Attack(currentTarget);
                    attackCooldown = 1f / attackSpeed;
                }
                if (animator) animator.SetBool("IsRunning", false);
                hasReachedDestination = true; // Stop moving when in attack range
            }
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


    public void PerformAttack()
    {
        if (currentTarget == null) return;

        if (currentTarget.TryGetComponent<UnitAI>(out UnitAI enemy) && enemy.isAlive)
        {
            // Deal damage
            enemy.TakeDamage(attackDamage);

            // Notify traits
            OnAttackEvent?.Invoke(enemy);

            Debug.Log($"{unitName} attacked {enemy.unitName} for {attackDamage} dmg.");
        }
    }
    private void SpawnProjectile(UnitAI target)
    {
        if (target == null || !target.isAlive) return;

        // Pick spawn point
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f;
        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        // Make it move toward target
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
            Vector3 dir = (target.transform.position + Vector3.up * 1.2f) - proj.transform.position;
            proj.transform.position += dir.normalized * projectileSpeed * Time.deltaTime;

            if (dir.magnitude < 0.2f) // impact
            {
                target.TakeDamage(attackDamage);

                // Optional: spawn hit VFX
                // GameObject impact = Instantiate(impactVFX, target.transform.position, Quaternion.identity);
                // Destroy(impact, 1f);

                Destroy(proj);
                yield break;
            }

            yield return null;
        }

        if (proj != null) Destroy(proj);
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
        direction.y = 0f; // keep upright

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }
    }

    // 🔹 Call this from the auto attack animation (Animation Event)
    public void DealAutoAttackDamage()
    {
        if (currentTarget != null && currentTarget.TryGetComponent(out UnitAI enemy))
        {
            if (projectilePrefab != null)
            {
                SpawnProjectile(enemy);
            }
            else
            {
                enemy.TakeDamage(attackDamage); // melee units hit instantly
            }
            GainMana(10); // Only gain mana once, here

            // notify traits / listeners
            OnAttackEvent?.Invoke(enemy);

            var ks = GetComponent<KillSwitchAbility>();
            if (ks != null) ks.OnAttack(enemy);
        }
    }

    public void TakeDamage(float damage)
    {
        float reducedDamage = damage * (1f - (armor * 0.005f));
        currentHealth -= reducedDamage;
        GainMana(1);

        Debug.Log($"{unitName} took {reducedDamage} damage. HP: {currentHealth}/{maxHealth}");
        if (ui != null)
            ui.UpdateHealth(currentHealth);   // ✅ refresh bar

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
        // ✅ Guard: prevent multiple death calls
        if (!isAlive) return;

        isAlive = false;
        OnAnyUnitDeath?.Invoke(this);
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

            // Get death animation length safely
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            float animLength = state.length > 0 ? state.length : 1.0f; // fallback to 1s

            StartCoroutine(DeathSequence(animLength));
        }
        else
        {
            // No animator → just fade out
            StartCoroutine(FadeAndDestroy(2.5f));
        }

        // Disable collider so others can move onto this tile
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        ClearTile();

        if (ui != null)
            ui.gameObject.SetActive(false);

        // Stop Update loop
        this.enabled = false;
    }

    private IEnumerator DeathSequence(float animLength)
    {
        // ✅ wait for death animation
        yield return new WaitForSeconds(animLength);

        // Then fade and destroy
        StartCoroutine(FadeAndDestroy(2.5f));
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
                unitAbility.Cast(currentTarget?.GetComponent<UnitAI>());
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
    }


    // ✅ Only one interface definition now
    public interface IUnitAbility
    {
        void Cast(UnitAI target);
    }

    private Transform FindNearestEnemy()
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        Transform nearest = null;
        float minDist = Mathf.Infinity;

        foreach (var unit in allUnits)
        {
            if (unit == this || !unit.isAlive) continue;
            if (unit.team == this.team) continue; // ✅ don’t target allies

            float dist = Vector3.Distance(transform.position, unit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = unit.transform;
            }
        }
        return nearest;
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
    public void FullResetToPrep(HexTile tile)
    {
        // restore stats
        currentHealth = maxHealth;
        currentMana = 0;
        isAlive = true;
        isCastingAbility = false;

        // snap back to tile
        AssignToTile(tile);

        // reset state/animation
        SetState(UnitState.BoardIdle);
        currentTarget = null;

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.ResetTrigger("AttackTrigger");
            animator.ResetTrigger("AbilityTrigger");
        }

        // reset UI bars
        if (ui != null)
        {
            ui.UpdateHealth(currentHealth);
            ui.UpdateMana(currentMana);
            ui.gameObject.SetActive(true);
        }

        // re-enable this script
        this.enabled = true;

        // re-enable collider
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }

    public void ResetAfterCombat()
    {
        // ✅ RESTORE HEALTH AND MANA FOR NEW ROUND
        currentHealth = maxHealth;
        currentMana = 0f;

        // Stop all combat-related activity
        SetState(UnitState.BoardIdle);

        currentTarget = null;
        attackCooldown = 0f;
        isCastingAbility = false;

        // Reset animator
        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.ResetTrigger("AttackTrigger");
            animator.ResetTrigger("AbilityTrigger");
        }

        // ✅ UPDATE UI BARS TO SHOW FULL HEALTH/ZERO MANA
        if (ui != null)
        {
            ui.UpdateHealth(currentHealth);
            ui.UpdateMana(currentMana);
        }

        Debug.Log($"✅ {unitName} reset for new round - HP: {currentHealth}/{maxHealth}, Mana: {currentMana}/{maxMana}");
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


    private void MoveTowards(Vector3 targetPos)
    {
        if (currentTile == null) return;

        // Calculate ideal destination (near target, not necessarily on grid)
        Vector3 idealDestination = CalculateIdealDestination(targetPos);

        // Only recalculate path if destination changed significantly
        if (Vector3.Distance(idealDestination, currentDestination) > 0.5f)
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
            if (animator) animator.SetBool("IsRunning", false);
        }
    }
    // ⭐ Upgrade Star Level (called from GameManager when merging)
    // ⭐ Upgrade Star Level (called from GameManager when merging)
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
        maxHealth = Mathf.Round(maxHealth * multiplier);
        attackDamage = Mathf.Round(attackDamage * multiplier);
        currentHealth = maxHealth;

        // 📏 Force scaling rules by unit name
        if (starLevel == 2)
        {
            switch (unitName)
            {
                case "Needlebot":
                    transform.localScale = new Vector3(1, 70, 1);
                    break;
                case "Bop":
                    transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
                    break;
                case "ManaDrive":
                    transform.localScale = new Vector3(115, 115, 115);
                    break;
                case "KillSwitch":
                    transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);
                    break;
                case "Haymaker":
                    transform.localScale = new Vector3(1.1f, 120, 1.1f);
                    break;
                default:
                    transform.localScale = baseScale * 1.1f; // fallback for other units
                    break;
            }
        }
        else if (starLevel == 3)
        {
            switch (unitName)
            {
                case "Needlebot":
                    transform.localScale = new Vector3(125, 125, 125);
                    break;
                case "Bop":
                    transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);
                    break;
                case "ManaDrive":
                    transform.localScale = new Vector3(130, 130, 130);
                    break;
                case "KillSwitch":
                    transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                    break;
                case "Haymaker":
                    transform.localScale = new Vector3(140, 140, 140);
                    break;
                default:
                    transform.localScale = baseScale * 1.25f; // fallback for other units
                    break;
            }
        }

        // Spawn VFX if prefab assigned
        if (GameManager.Instance != null && GameManager.Instance.starUpVFXPrefab != null)
        {
            var vfx = Instantiate(GameManager.Instance.starUpVFXPrefab, transform.position + Vector3.up * 1.2f, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        // Update UI
        if (ui != null)
            ui.UpdateHealth(currentHealth);

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
