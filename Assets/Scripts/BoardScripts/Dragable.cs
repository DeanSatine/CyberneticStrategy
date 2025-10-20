using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class Draggable : MonoBehaviour
{
    public bool isDragging = false;
    private Vector3 oldPosition;
    private UnitAI unitAI;
    public static Draggable currentlyDragging;
    private HexTile originalTile;
    private List<HexTile> highlightedTiles = new List<HexTile>();

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
    }

    private void OnMouseDown()
    {
        // ✅ BLOCK INTERACTION DURING COMBAT
        if (!CanInteractWithUnit())
        {
            Debug.Log($"❌ Cannot interact with {unitAI.unitName} during combat!");
            return;
        }

        if (unitAI.currentTile != null && unitAI.currentTile.occupyingUnit != unitAI)
        {
            Debug.LogWarning("Tried to pick up a unit that shares a tile with another. Ignored.");
            return;
        }

        // ✅ Prevent picking up another unit if one is already being dragged
        if (!isDragging)
        {
            if (currentlyDragging != null && currentlyDragging != this)
            {
                Debug.Log($"🚫 Already dragging {currentlyDragging.unitAI.unitName}, can’t pick up another.");
                return;
            }

            // Pick up the unit
            isDragging = true;
            currentlyDragging = this;
            oldPosition = transform.position;

            // ✅ STORE original tile before clearing it
            originalTile = unitAI.currentTile;

            // Free its previously occupied tile immediately
            if (unitAI != null && unitAI.currentTile != null)
            {
                if (unitAI.currentTile.occupyingUnit == unitAI)
                    unitAI.currentTile.occupyingUnit = null;

                unitAI.currentTile = null;
            }
            HighlightValidTiles();
        }
        else
        {
            // Drop the unit
            isDragging = false;
            currentlyDragging = null;
            ClearTileHighlights();
            SnapToClosestObject();
        }
    }

    private void HighlightValidTiles()
    {
        ClearTileHighlights();

        if (BoardManager.Instance == null) return;

        List<HexTile> allTiles = BoardManager.Instance.GetAllTiles();

        foreach (HexTile tile in allTiles)
        {
            if (tile == null || tile.occupyingUnit != null) continue;

            if (unitAI.team == Team.Player && CanPlayerPlaceOnTile(tile))
            {
                tile.HighlightAsValid();
                highlightedTiles.Add(tile);
            }
        }
    }

    private void ClearTileHighlights()
    {
        foreach (HexTile tile in highlightedTiles)
        {
            if (tile != null)
            {
                tile.ClearHighlight();
            }
        }
        highlightedTiles.Clear();
    }

    private bool CanInteractWithUnit()
    {
        // ✅ Allow interaction with benched units even during combat
        if (unitAI != null && unitAI.currentState == UnitState.Bench)
        {
            Debug.Log($"✅ {unitAI.unitName} is benched and can be moved during combat");
            return true;
        }

        // ✅ Check 1: No interaction during combat phase (except for benched units)
        if (StageManager.Instance != null && StageManager.Instance.currentPhase == StageManager.GamePhase.Combat)
        {
            Debug.Log("🚫 Units on the board cannot be moved during combat phase!");
            return false;
        }

        // ✅ Check 2: No interaction if unit is in combat state
        if (unitAI != null && unitAI.currentState == UnitState.Combat)
        {
            Debug.Log($"🚫 {unitAI.unitName} is in combat and cannot be moved!");
            return false;
        }

        // ✅ Check 3: No interaction if unit is dead
        if (unitAI != null && !unitAI.isAlive)
        {
            Debug.Log($"🚫 {unitAI.unitName} is dead and cannot be moved!");
            return false;
        }

        // ✅ Check 4: Only allow player units to be dragged (prevent enemy unit interaction)
        if (unitAI != null && unitAI.team != Team.Player)
        {
            Debug.Log($"🚫 Cannot interact with enemy unit {unitAI.unitName}!");
            return false;
        }

        return true;
    }


    private void Update()
    {
        if (isDragging)
        {
            if (!CanInteractWithUnit())
            {
                Debug.Log("❌ Combat started while dragging! Dropping unit.");
                isDragging = false;
                currentlyDragging = null;
                ClearTileHighlights();
                SnapToClosestObject();
                return;
            }

            Vector3 mousePos = GetMouseWorldPos();
            transform.position = new Vector3(mousePos.x, transform.position.y, mousePos.z);
        }
    }

    private bool CanPlayerPlaceOnTile(HexTile tile)
    {
        // ✅ CRITICAL: Block ALL board placement during combat
        if (tile.tileType == TileType.Board)
        {
            if (StageManager.Instance != null && StageManager.Instance.currentPhase == StageManager.GamePhase.Combat)
            {
                Debug.Log($"🚫 COMBAT ACTIVE: Cannot place any units on the board during combat!");
                return false;
            }

            // Normal row restrictions during prep phase
            if (tile.gridPosition.x > 3)
            {
                Debug.Log($"❌ Cannot place unit beyond row 3. Tile {tile.gridPosition} is too far forward.");
                return false;
            }

            Debug.Log($"✅ Valid board placement at {tile.gridPosition} (prep phase, rows 0-3)");
            return true;
        }

        // ✅ Always allow bench placement
        if (tile.tileType == TileType.Bench)
        {
            Debug.Log($"✅ Valid bench placement at {tile.gridPosition}");
            return true;
        }

        Debug.Log($"❌ Invalid tile type for placement: {tile.tileType}");
        return false;
    }

    private void SnapToClosestObject()
    {
        Collider[] colliders = Physics.OverlapCapsule(
            transform.position - Vector3.up * 0.5f,
            transform.position + Vector3.up * 0.5f,
            0.5f);

        HexTile targetTile = null;
        float closestDistance = float.MaxValue;

        foreach (var collider in colliders)
        {
            if (collider.gameObject == gameObject) continue;

            HexTile tile = collider.GetComponentInParent<HexTile>();
            if (tile == null) continue;

            if (unitAI.team == Team.Player && !CanPlayerPlaceOnTile(tile))
            {
                continue; // Skip invalid tiles for player units
            }

            float distance = Vector3.Distance(transform.position, tile.transform.position);
            if (distance < closestDistance)
            {
                targetTile = tile;
                closestDistance = distance;
            }
        }

        if (targetTile != null)
        {
            if (!targetTile.TryClaim(unitAI))
            {
                Debug.Log($"Tile {targetTile.name} already occupied. Placement rejected.");
                transform.position = oldPosition;
                return;
            }

            if (unitAI.currentTile != null && unitAI.currentTile != targetTile)
            {
                unitAI.currentTile.Free(unitAI);
            }

            // Snap to this tile
            transform.position = new Vector3(
                targetTile.transform.position.x,
                transform.position.y,
                targetTile.transform.position.z);

            // ✅ CRITICAL: Update state FIRST, then evaluate traits
            if (targetTile.tileType == TileType.Board)
            {
                unitAI.currentState = UnitState.BoardIdle;
                GameManager.Instance.RegisterUnit(unitAI, unitAI.team == Team.Player);
            }
            else if (targetTile.tileType == TileType.Bench)
            {
                unitAI.currentState = UnitState.Bench;
                GameManager.Instance.UnregisterUnit(unitAI);
            }

            // ✅ NOW evaluate traits AFTER unit state is properly updated
            TraitManager.Instance.EvaluateTraits(GameManager.Instance.playerUnits);
            TraitManager.Instance.ApplyTraits(GameManager.Instance.playerUnits);

            // ✅ Check for merging after moving a unit
            if (unitAI.team == Team.Player)
            {
                GameManager.Instance.TryMergeUnits(unitAI);
            }

            // ✅ Update fight button after state changes
            if (unitAI.team == Team.Player && UIManager.Instance != null)
            {
                UIManager.Instance.UpdateFightButtonVisibility();
            }

            Debug.Log($"✅ {unitAI.unitName} placed successfully at {targetTile.gridPosition}");
            return;
        }
        // No valid tile found → snap back
        Debug.Log($"❌ No valid placement found for {unitAI.unitName}. Returning to original position.");
        transform.position = oldPosition;

        // ✅ CRITICAL: Restore original tile reference to fix movement bug
        if (originalTile != null)
        {
            Debug.Log($"🔄 Restoring {unitAI.unitName} to original tile: {originalTile.gridPosition}");

            // Reclaim the original tile
            if (originalTile.TryClaim(unitAI))
            {
                // Restore proper state based on tile type
                if (originalTile.tileType == TileType.Board)
                {
                    unitAI.currentState = UnitState.BoardIdle;
                    GameManager.Instance.RegisterUnit(unitAI, unitAI.team == Team.Player);
                }
                else if (originalTile.tileType == TileType.Bench)
                {
                    unitAI.currentState = UnitState.Bench;
                    GameManager.Instance.UnregisterUnit(unitAI);
                }

                Debug.Log($"✅ {unitAI.unitName} successfully restored to {originalTile.gridPosition} with state {unitAI.currentState}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Could not reclaim original tile {originalTile.gridPosition}, unit may be in invalid state!");
            }
        }

        // ✅ Clear the stored reference
        originalTile = null;
        return;
    }

    private Vector3 GetMouseWorldPos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            return hit.point;
        }

        return Vector3.zero;
    }
}
