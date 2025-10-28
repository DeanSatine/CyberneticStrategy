using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
public class ClobbertronJumpBehaviour : MonoBehaviour
{
    [HideInInspector] public float lowHealthThreshold = 0.1f;
    [HideInInspector] public Color augmentColor = Color.blue;

    [Header("Jump Settings")]
    public float jumpHeight = 3f;
    public float jumpDuration = 0.5f;
    public GameObject jumpVFXPrefab;
    [HideInInspector] public ItsClobberingTimeAugment augment;
    [HideInInspector] public float shockwaveDamageMultiplier = 1.5f;
    [HideInInspector] public int shockwaveHexRadius = 2;
    private HexTile reservedLandingTile = null;

    private UnitAI unitAI;
    private ClobbertronTrait clobbertronTrait;
    private bool hasLowHealthJumped = false;  // 10% HP jump
    private bool hasMidHealthJumped = false;  // 50% HP jump (NEW)
    private bool isJumping = false;
    private bool hasCombatStartJumped = false;
    private UnitAI.UnitState lastKnownState = UnitAI.UnitState.BoardIdle;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        clobbertronTrait = GetComponent<ClobbertronTrait>();

        if (unitAI == null)
        {
            Debug.LogError($"❌ ClobbertronJumpBehaviour on {gameObject.name} has no UnitAI component!");
        }
    }

    private void Update()
    {
        if (unitAI == null || !unitAI.isAlive || isJumping) return;

        // FIX 1: Check if Clobbertron trait is active
        if (!IsTraitActive()) return;

        // Detect when unit enters combat state for the first time
        if (lastKnownState != UnitAI.UnitState.Combat && unitAI.currentState == UnitAI.UnitState.Combat)
        {
            Debug.Log($"🔨 {unitAI.unitName} detected combat start - preparing to jump");
            lastKnownState = UnitAI.UnitState.Combat;

            if (!hasCombatStartJumped)
            {
                StartCoroutine(DelayedCombatStartJump());
            }
        }

        // Update last known state
        lastKnownState = unitAI.currentState;

        // Only check for health-based jumps during combat
        if (unitAI.currentState != UnitAI.UnitState.Combat) return;

        float healthPercent = (float)unitAI.currentHealth / unitAI.maxHealth;

        // FIX 2: Check for 50% HP jump first (coordinate with ClobbertronTrait crash)
        if (!hasMidHealthJumped && healthPercent <= 0.5f && healthPercent > lowHealthThreshold)
        {
            Debug.Log($"🔨 {unitAI.unitName} is at {healthPercent:P1} health - executing 50% health jump!");
            hasMidHealthJumped = true;
            JumpToTarget();
        }
        // Check for 10% HP jump (low health threshold)
        else if (!hasLowHealthJumped && healthPercent <= lowHealthThreshold)
        {
            Debug.Log($"🔨 {unitAI.unitName} is at {healthPercent:P1} health - executing low health jump!");
            hasLowHealthJumped = true;
            JumpToTarget();
        }
    }

    // FIX 1: Check if Clobbertron trait is actually active
    private bool IsTraitActive()
    {
        return unitAI.traits.Contains(Trait.Clobbertron);
    }

    private IEnumerator DelayedCombatStartJump()
    {
        // Wait a short moment for combat to properly initialize and targets to be assigned
        yield return new WaitForSeconds(0.2f);

        if (unitAI != null && unitAI.isAlive && unitAI.currentState == UnitAI.UnitState.Combat)
        {
            Debug.Log($"🔨 {unitAI.unitName} executing combat start jump");
            JumpToTarget();
            hasCombatStartJumped = true;
        }
    }

    public void JumpToTarget()
    {
        if (isJumping)
        {
            Debug.Log($"🔨 {unitAI.unitName} is already jumping - skipping");
            return;
        }

        UnitAI target = GetValidTarget();

        if (target != null)
        {
            // FIX 3: Get smart landing position near target
            Vector3 landingPosition = GetSmartLandingPosition(target);
            Debug.Log($"🔨 {unitAI.unitName} jumping to {target.unitName} (landing at {landingPosition})");
            StartCoroutine(JumpToPosition(landingPosition));
        }
        else
        {
            Debug.LogWarning($"⚠️ {unitAI.unitName} has no valid target to jump to!");
        }
    }

    // FIX 3: Smart landing position that stays on board and avoids same hex
    private Vector3 GetSmartLandingPosition(UnitAI target)
    {
        Vector3 targetPos = target.transform.position;
        Vector3 myPos = transform.position;

        // Calculate direction from me to target
        Vector3 direction = (targetPos - myPos).normalized;

        // Try different landing positions around the target
        Vector3[] candidateOffsets = {
            direction * 1.5f,                    // In front of target
            direction * 2f,                      // Further in front
            Vector3.Cross(direction, Vector3.up) * 1.5f,  // To the side
            -Vector3.Cross(direction, Vector3.up) * 1.5f, // Other side
            -direction * 1f,                     // Behind target
        };

        foreach (Vector3 offset in candidateOffsets)
        {
            Vector3 candidatePos = targetPos + offset;

            // Check if this position is valid
            if (IsValidLandingPosition(candidatePos))
            {
                return candidatePos;
            }
        }

        // Fallback: Use closest valid board position
        return GetClosestValidBoardPosition(targetPos);
    }

    private bool IsValidLandingPosition(Vector3 position)
    {
        HexTile nearestTile = GetNearestBoardTile(position);
        if (nearestTile == null) return false;

        // FIX BUG 2: Check if tile is reserved by another jumping unit
        if (augment != null && augment.IsTileReserved(nearestTile)) return false;

        // Check if tile is occupied by another unit (not me)
        if (nearestTile.occupyingUnit != null && nearestTile.occupyingUnit != unitAI) return false;

        // Check if it's not the same tile I'm currently on
        if (unitAI.currentTile != null && nearestTile == unitAI.currentTile) return false;

        float distanceFromTileCenter = Vector3.Distance(position, nearestTile.transform.position);
        return distanceFromTileCenter <= 1f;
    }

    private HexTile GetNearestBoardTile(Vector3 position)
    {
        if (BoardManager.Instance == null) return null;

        var allTiles = BoardManager.Instance.GetAllTiles();
        var boardTiles = allTiles.Where(t => t.tileType == TileType.Board).ToArray();

        HexTile nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var tile in boardTiles)
        {
            float distance = Vector3.Distance(position, tile.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = tile;
            }
        }

        return nearest;
    }

    private Vector3 GetClosestValidBoardPosition(Vector3 targetPosition)
    {
        if (BoardManager.Instance == null) return targetPosition;

        var allTiles = BoardManager.Instance.GetAllTiles();
        var boardTiles = allTiles.Where(t => t.tileType == TileType.Board &&
                                            (t.occupyingUnit == null || t.occupyingUnit == unitAI) &&
                                            t != unitAI.currentTile).ToArray();

        if (boardTiles.Length == 0)
        {
            Debug.LogWarning($"⚠️ No valid board tiles found for {unitAI.unitName}!");
            return targetPosition; // Fallback to original target position
        }

        // Find closest free board tile to target
        HexTile bestTile = boardTiles.OrderBy(t => Vector3.Distance(t.transform.position, targetPosition)).First();
        return bestTile.transform.position;
    }

    // Better target validation and acquisition
    private UnitAI GetValidTarget()
    {
        UnitAI target = null;

        // Method 1: Use unitAI.GetCurrentTarget() (proper API)
        target = unitAI.GetCurrentTarget();
        if (target != null && target.isAlive)
        {
            Debug.Log($"✅ Using current target: {target.unitName}");
            return target;
        }

        // Method 2: Get UnitAI from currentTarget Transform (fallback)
        if (unitAI.currentTarget != null)
        {
            target = unitAI.currentTarget.GetComponent<UnitAI>();
            if (target != null && target.isAlive)
            {
                Debug.Log($"✅ Using target from Transform: {target.unitName}");
                return target;
            }
        }

        // Method 3: Find closest enemy manually (last resort)
        target = FindClosestEnemy();
        if (target != null)
        {
            Debug.Log($"✅ Using closest enemy: {target.unitName}");
            return target;
        }

        return null;
    }

    private UnitAI FindClosestEnemy()
    {
        UnitAI[] enemies = FindObjectsOfType<UnitAI>();
        UnitAI closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (var enemy in enemies)
        {
            // Must be alive, different team, and in combat
            if (enemy.team != unitAI.team && enemy.isAlive && enemy.currentState == UnitAI.UnitState.Combat)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }
        }

        return closestEnemy;
    }

    private IEnumerator JumpToPosition(Vector3 targetPosition)
    {
        if (isJumping) yield break;

        isJumping = true;
        HexTile landingTile = GetNearestBoardTile(targetPosition);
        if (landingTile != null && augment != null)
        {
            if (!augment.TryReserveTile(landingTile))
            {
                Debug.LogWarning($"⚠️ {unitAI.unitName} landing tile is reserved, finding alternative");
                targetPosition = GetClosestValidBoardPosition(targetPosition);
                landingTile = GetNearestBoardTile(targetPosition);
                if (landingTile != null)
                {
                    augment.TryReserveTile(landingTile);
                }
            }
            reservedLandingTile = landingTile;
        }

        Vector3 startPos = transform.position;
        float groundHeight = startPos.y;
        
        Vector3 endPos = targetPosition;
        endPos.y = groundHeight;
        
        Vector3 jumpPeakPos = Vector3.Lerp(startPos, endPos, 0.5f) + Vector3.up * jumpHeight;

        Debug.Log($"🔨 Jump heights - Start Y: {startPos.y}, End Y: {endPos.y}, Peak Y: {jumpPeakPos.y}");

        ClearMovementState();

        // Update tile occupancy before jumping
        if (unitAI.currentTile != null)
        {
            unitAI.currentTile.Free(unitAI);
        }

        // Play jump VFX at start position (feet level)
        if (jumpVFXPrefab != null)
        {
            Vector3 feetPosition = startPos;
            feetPosition.y = 0.1f;
            
            GameObject startVFX = Instantiate(jumpVFXPrefab, feetPosition, Quaternion.identity);
            Destroy(startVFX, 2f);
        }

        Debug.Log($"🔨 {unitAI.unitName} starting jump animation from {startPos} to {endPos}");

        // Disable unit movement during jump
        bool originalCanMove = unitAI.canMove;
        unitAI.canMove = false;

        // Force unit to stop any current movement
        if (unitAI.GetComponent<Rigidbody>() != null)
        {
            unitAI.GetComponent<Rigidbody>().velocity = Vector3.zero;
        }

        // Jump up to peak (first half of jump)
        float halfJumpTime = jumpDuration * 0.5f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / halfJumpTime;
            Vector3 currentPos = Vector3.Lerp(startPos, jumpPeakPos, t);
            transform.position = currentPos;

            // Rotate towards target during jump
            Vector3 directionToTarget = (endPos - startPos).normalized;
            if (directionToTarget != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 3f);
            }

            yield return null;
        }

        // Jump down to target (second half of jump)
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / halfJumpTime;
            Vector3 currentPos = Vector3.Lerp(jumpPeakPos, endPos, t);
            transform.position = currentPos;
            yield return null;
        }

        transform.position = endPos;
        
        Debug.Log($"🔨 {unitAI.unitName} final landing Y position: {transform.position.y} (expected: {endPos.y})");

        ResetMovementStateAfterLanding(endPos);

        // Play landing VFX at FEET (ground level)
        if (jumpVFXPrefab != null)
        {
            Vector3 feetPosition = endPos;
            feetPosition.y = endPos.y + 0.1f;
            
            GameObject landVFX = Instantiate(jumpVFXPrefab, feetPosition, Quaternion.identity);

            ParticleSystem particles = landVFX.GetComponent<ParticleSystem>();
            if (particles != null)
            {
                var main = particles.main;
            }

            Destroy(landVFX, 2f);
            Debug.Log($"💥 Spawned landing VFX at {feetPosition}");
        }
        else
        {
            Debug.LogWarning($"⚠️ {unitAI.unitName} has no jumpVFXPrefab assigned!");
        }

        // Do landing impact damage in small radius
        DoLandingImpact(endPos);

        // Re-enable movement
        unitAI.canMove = originalCanMove;
        isJumping = false;
        if (reservedLandingTile != null && augment != null)
        {
            augment.UnreserveTile(reservedLandingTile);
            reservedLandingTile = null;
        }
        Debug.Log($"🔨 {unitAI.unitName} completed jump landing at {endPos}!");
    }

    // NEW: Clear movement state before jumping
    private void ClearMovementState()
    {
        // Use reflection or try to access private fields if possible
        // Alternative: Add public methods to UnitAI to reset movement state
        if (unitAI != null)
        {
            // Force stop any ongoing movement
            if (unitAI.GetComponent<Rigidbody>() != null)
            {
                unitAI.GetComponent<Rigidbody>().velocity = Vector3.zero;
            }

            // Clear any ongoing movement animations
            if (unitAI.animator != null)
            {
                unitAI.animator.SetBool("IsRunning", false);
            }
        }
    }

    // NEW: Comprehensive state reset after landing
    private void ResetMovementStateAfterLanding(Vector3 landingPosition)
    {
        HexTile landingTile = GetNearestBoardTile(landingPosition);
        if (landingTile != null && landingTile.TryClaim(unitAI))
        {
            Debug.Log($"🔨 {unitAI.unitName} successfully claimed landing tile");
        }

        Vector3 correctedPosition = transform.position;
        correctedPosition.y = landingPosition.y;
        transform.position = correctedPosition;
        
        Debug.Log($"🔨 {unitAI.unitName} position corrected to Y: {correctedPosition.y}");

        StartCoroutine(DelayedMovementReset());
    }
    private System.Collections.IEnumerator DelayedMovementReset()
    {
        yield return null; // Wait one frame

        if (unitAI != null)
        {
            // Use the new public methods to reset movement state
            unitAI.ForceResetMovementState();
            unitAI.ResetPathfinding();

            Debug.Log($"🔨 {unitAI.unitName} movement state comprehensively reset after landing");
        }
    }



    private void DoLandingImpact(Vector3 impactPosition)
    {
        if (BoardManager.Instance == null)
        {
            Debug.LogWarning("⚠️ BoardManager.Instance is null - cannot perform hex-based shockwave");
            return;
        }

        HexTile centerTile = GetNearestBoardTile(impactPosition);
        if (centerTile == null)
        {
            Debug.LogWarning("⚠️ No center tile found for shockwave impact");
            return;
        }

        float shockwaveDamage = unitAI.attackDamage * shockwaveDamageMultiplier;
        HashSet<HexTile> affectedTiles = GetTilesInHexRadius(centerTile, shockwaveHexRadius);
        int hitCount = 0;

        Debug.Log($"💥 {unitAI.unitName} shockwave impact at {centerTile.gridPosition} affecting {affectedTiles.Count} tiles in {shockwaveHexRadius} hex radius");

        foreach (var tile in affectedTiles)
        {
            if (tile.occupyingUnit != null)
            {
                UnitAI enemy = tile.occupyingUnit;
                if (enemy.team != unitAI.team && enemy.isAlive)
                {
                    enemy.TakeDamage(shockwaveDamage);
                    hitCount++;
                    Debug.Log($"💥 {unitAI.unitName} shockwave damaged {enemy.unitName} for {shockwaveDamage} damage!");
                }
            }
        }

        if (hitCount > 0)
        {
            Debug.Log($"💥 {unitAI.unitName} shockwave hit {hitCount} enemies!");
            StartCoroutine(CameraShake(0.3f, 0.2f));
        }
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

    private IEnumerator CameraShake(float intensity, float duration)
    {
        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.Shake(intensity, duration);
        }
        yield break;
    }

    // Reset for new combat
    public void ResetJumpBehavior()
    {
        hasLowHealthJumped = false;
        hasMidHealthJumped = false;  // NEW: Reset 50% health jump
        hasCombatStartJumped = false;
        isJumping = false;
        lastKnownState = unitAI.currentState;
        Debug.Log($"🔨 {unitAI.unitName} jump behavior reset for new combat");
    }

    private void OnDrawGizmosSelected()
    {
        // Draw jump arc preview in editor
        if (unitAI != null)
        {
            Gizmos.color = augmentColor;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            UnitAI target = GetValidTarget();
            if (target != null)
            {
                Vector3 landingPos = GetSmartLandingPosition(target);
                Gizmos.DrawLine(transform.position, landingPos);
                Gizmos.DrawWireSphere(landingPos, 1f);
            }
        }
    }

    // DEBUG METHODS
    [ContextMenu("Force Jump Now")]
    public void ForceJumpNow()
    {
        Debug.Log($"🔨 Force jumping {unitAI.unitName}");
        JumpToTarget();
    }

    [ContextMenu("Debug Jump State")]
    public void DebugJumpState()
    {
        UnitAI target = GetValidTarget();
        Debug.Log($"🔨 {unitAI.unitName} Jump State:");
        Debug.Log($"   - Trait Active: {IsTraitActive()}");
        Debug.Log($"   - Is Jumping: {isJumping}");
        Debug.Log($"   - Has Combat Start Jumped: {hasCombatStartJumped}");
        Debug.Log($"   - Has Mid Health Jumped (50%): {hasMidHealthJumped}");
        Debug.Log($"   - Has Low Health Jumped (10%): {hasLowHealthJumped}");
        Debug.Log($"   - Current Health: {unitAI.currentHealth}/{unitAI.maxHealth} ({(float)unitAI.currentHealth / unitAI.maxHealth:P1})");
        Debug.Log($"   - Current State: {unitAI.currentState}");
        Debug.Log($"   - Last Known State: {lastKnownState}");
        Debug.Log($"   - Current Target: {(target != null ? target.unitName : "None")}");
        Debug.Log($"   - Can Move: {unitAI.canMove}");
        Debug.Log($"   - Is Alive: {unitAI.isAlive}");
    }
}
