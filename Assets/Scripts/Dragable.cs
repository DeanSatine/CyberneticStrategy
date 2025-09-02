using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitAI;

public class Draggable : MonoBehaviour
{
    public bool isDragging = false;
    private Vector3 oldPosition;
    private int friendlyUnitLayer = 9;

    private void OnMouseDown()
    {
        if (!isDragging)
        {
            // Pick up the unit
            isDragging = true;
            oldPosition = transform.position;
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

        Transform closestObject = null;
        float closestDistance = float.MaxValue;

        foreach (var collider in colliders)
        {
            if (collider.gameObject == gameObject) continue;

            float distance = Vector3.Distance(transform.position, collider.transform.position);
            if (distance < closestDistance)
            {
                closestObject = collider.transform;
                closestDistance = distance;
            }
        }

        if (closestObject != null)
        {
            transform.position = new Vector3(
                closestObject.position.x,
                transform.position.y,
                closestObject.position.z);

            var unitAI = GetComponent<UnitAI>();
            var tile = closestObject.GetComponent<HexTile>();

            if (tile != null)
            {
                if (tile.tileType == TileType.Board)
                {
                    unitAI.currentState = UnitState.BoardIdle;
                    Debug.Log($"{gameObject.name} placed on BOARD");
                    GameManager.Instance.RegisterUnit(unitAI, unitAI.team == Team.Player);
                }
                else if (tile.tileType == TileType.Bench)
                {
                    unitAI.currentState = UnitState.Bench;
                    Debug.Log($"{gameObject.name} placed on BENCH");
                    GameManager.Instance.UnregisterUnit(unitAI);
                }
            }
        }
        else
        {
            transform.position = oldPosition;
        }
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
