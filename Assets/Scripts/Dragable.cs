using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class Draggable : MonoBehaviour
{
    public bool isDragging = false;
    private Vector3 oldPosition;
    private UnitAI unitAI;

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
        if (!isDragging)
        {
            // Pick up the unit
            isDragging = true;
            oldPosition = transform.position;

            // Free its previously occupied tile immediately (so other units may be placed there while dragging)
            if (unitAI != null && unitAI.currentTile != null)
            {
                if (unitAI.currentTile.occupyingUnit == unitAI)
                    unitAI.currentTile.occupyingUnit = null;

                unitAI.currentTile = null;
            }
        }
        else
        {
            // Drop the unit
            isDragging = false;
            SnapToClosestObject();
        }
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
            // ✅ Additional safety check during dragging
            if (!CanInteractWithUnit())
            {
                Debug.Log("❌ Combat started while dragging! Dropping unit.");
                isDragging = false;
                SnapToClosestObject();
                return;
            }

            Vector3 mousePos = GetMouseWorldPos();
            transform.position = new Vector3(mousePos.x, transform.position.y, mousePos.z);
        }
    }

    private bool CanPlayerPlaceOnTile(HexTile tile)
    {
        // Allow placement on bench tiles
        if (tile.tileType == TileType.Bench)
            return true;

        // For board tiles, check only row restrictions
        if (tile.tileType == TileType.Board)
        {
            // Change from Y restriction to X restriction
            if (tile.gridPosition.x > 3)
            {
                Debug.Log($"❌ Cannot place unit beyond row 3. Tile {tile.gridPosition} is too far forward.");
                return false;
            }

            Debug.Log($"✅ Valid placement at {tile.gridPosition} (bottom half, rows 0-3)");
            return true;
        }

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

            // State logic
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

            // ✅ NOW evaluate traits AFTER unit is properly placed and registered
            TraitManager.Instance.EvaluateTraits(GameManager.Instance.playerUnits);
            TraitManager.Instance.ApplyTraits(GameManager.Instance.playerUnits);

            Debug.Log($"✅ {unitAI.unitName} placed successfully at {targetTile.gridPosition}");
            return;
        }

        // No valid tile found → snap back
        Debug.Log($"❌ No valid placement found for {unitAI.unitName}. Returning to original position.");
        transform.position = oldPosition;
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
