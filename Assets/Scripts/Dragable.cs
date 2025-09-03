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

            float distance = Vector3.Distance(transform.position, tile.transform.position);
            if (distance < closestDistance)
            {
                targetTile = tile;
                closestDistance = distance;
            }
        }

        if (targetTile != null)
        {
            // Check occupancy BEFORE snapping
            if (targetTile.occupyingUnit != null)
            {
                Debug.Log($"Tile {targetTile.name} is already occupied by {targetTile.occupyingUnit.unitName}");
                transform.position = oldPosition; // snap back
                return;
            }

            // ✅ Free old tile if needed
            if (unitAI.currentTile != null && unitAI.currentTile.occupyingUnit == unitAI)
                unitAI.currentTile.occupyingUnit = null;

            // ✅ Now safe to snap
            transform.position = new Vector3(
                targetTile.transform.position.x,
                transform.position.y,
                targetTile.transform.position.z);

            // Claim tile
            targetTile.occupyingUnit = unitAI;
            unitAI.currentTile = targetTile;

            // State management
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

            return;
        }


        // No tile found → snap back
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
