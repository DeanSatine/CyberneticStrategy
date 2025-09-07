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

    private void Update()
    {
        if (isDragging)
        {
            Vector3 mousePos = GetMouseWorldPos();
            transform.position = new Vector3(mousePos.x, transform.position.y, mousePos.z);
        }
    }

    // ✅ Check if player can place units on this tile
    private bool CanPlayerPlaceOnTile(HexTile tile)
    {
        // Allow placement on bench tiles
        if (tile.tileType == TileType.Bench)
            return true;

        // For board tiles, check ownership and row restrictions
        if (tile.tileType == TileType.Board)
        {
            // Only allow player units on player-owned tiles
            if (tile.owner != TileOwner.Player)
            {
                Debug.Log($"❌ Cannot place unit on enemy territory at {tile.gridPosition}");
                return false;
            }

            // ✅ RESTRICTION: Only allow placement on rows 0-4 (first 5 rows)
            if (tile.gridPosition.y > 4)
            {
                Debug.Log($"❌ Cannot place unit beyond row 4. Tile {tile.gridPosition} is too far from player side.");
                return false;
            }

            Debug.Log($"✅ Valid placement at {tile.gridPosition} (player territory, row 0-4)");
            return true;
        }

        return false;
    }

    private void SnapToClosestObject()
    {
        TraitManager.Instance.EvaluateTraits(GameManager.Instance.playerUnits);

        Collider[] colliders = Physics.OverlapCapsule(
            transform.position - Vector3.up * 0.5f,
            transform.position + Vector3.up * 0.5f,
            0.5f);

        HexTile targetTile = null;
        float closestDistance = float.MaxValue;

        foreach (var collider in colliders)
        {
            if (collider.gameObject == gameObject) continue;

            // 🔑 Always search upwards to see if this collider belongs to a HexTile
            HexTile tile = collider.GetComponentInParent<HexTile>();
            if (tile == null) continue;

            // ✅ Check if player can place on this tile (ownership + row restriction)
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
            // Ask the tile to claim itself atomically
            if (!targetTile.TryClaim(unitAI))
            {
                Debug.Log($"Tile {targetTile.name} already occupied. Placement rejected.");
                transform.position = oldPosition;
                return;
            }

            // ✅ Free old tile first (important: do AFTER successful claim, so we don't lose both)
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
