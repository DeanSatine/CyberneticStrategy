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

    [Header("Hover Outline Settings")]
    [Tooltip("Outline color when hovering over the unit")]
    public Color outlineColor = Color.white;
    [Tooltip("Outline intensity (higher = brighter)")]
    [Range(0f, 2f)]
    public float outlineIntensity = 0.5f;

    private Renderer[] renderers;
    private MaterialPropertyBlock propBlock;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private bool isHovered = false;

    // ✅ Cache managers to detect networked vs offline mode
    private bool isNetworkedMode;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        renderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();

        // ✅ Detect which mode we're in
        isNetworkedMode = (GameManager.Instance != null);
    }

    private void OnMouseEnter()
    {
        if (!CanInteractWithUnit()) return;
        if (isDragging) return;

        isHovered = true;
        EnableOutline();
    }

    private void OnMouseExit()
    {
        isHovered = false;
        DisableOutline();
    }

    private void EnableOutline()
    {
        foreach (var rend in renderers)
        {
            if (rend == null) continue;

            rend.GetPropertyBlock(propBlock);
            propBlock.SetColor(EmissionColor, outlineColor * outlineIntensity);
            rend.SetPropertyBlock(propBlock);
        }
    }

    private void DisableOutline()
    {
        foreach (var rend in renderers)
        {
            if (rend == null) continue;

            rend.GetPropertyBlock(propBlock);
            propBlock.SetColor(EmissionColor, Color.black);
            rend.SetPropertyBlock(propBlock);
        }
    }

    private void OnMouseDown()
    {
        if (isHovered)
        {
            DisableOutline();
            isHovered = false;
        }

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
            if (currentlyDragging != null && currentlyDragging != this)
            {
                Debug.Log($"🚫 Already dragging {currentlyDragging.unitAI.unitName}, can't pick up another.");
                return;
            }

            isDragging = true;
            currentlyDragging = this;
            oldPosition = transform.position;
            originalTile = unitAI.currentTile;

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
        if (unitAI != null && unitAI.currentState == UnitState.Bench)
        {
            return true;
        }

        // ✅ Check StageManager in either mode
        if (StageManager.Instance != null && StageManager.Instance.currentPhase == StageManager.GamePhase.Combat)
        {
            return false;
        }

        if (unitAI != null && unitAI.currentState == UnitState.Combat)
        {
            return false;
        }

        if (unitAI != null && !unitAI.isAlive)
        {
            return false;
        }

        if (unitAI != null && unitAI.team != Team.Player)
        {
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
        if (tile.tileType == TileType.Board)
        {
            if (StageManager.Instance != null && StageManager.Instance.currentPhase == StageManager.GamePhase.Combat)
            {
                return false;
            }

            if (tile.gridPosition.x > 3)
            {
                return false;
            }

            return true;
        }

        if (tile.tileType == TileType.Bench)
        {
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
                continue;
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

            transform.position = new Vector3(
                targetTile.transform.position.x,
                transform.position.y,
                targetTile.transform.position.z);

            if (targetTile.tileType == TileType.Board)
            {
                unitAI.currentState = UnitState.BoardIdle;
                RegisterUnit(unitAI, unitAI.team == Team.Player);
            }
            else if (targetTile.tileType == TileType.Bench)
            {
                unitAI.currentState = UnitState.Bench;
                UnregisterUnit(unitAI);
            }

            TraitManager.Instance.EvaluateTraits(GetPlayerUnits());
            TraitManager.Instance.ApplyTraits(GetPlayerUnits());

            if (unitAI.team == Team.Player)
            {
                TryMergeUnits(unitAI);
            }

            UpdateFightButtonVisibility();

            Debug.Log($"✅ {unitAI.unitName} placed successfully at {targetTile.gridPosition}");
            return;
        }

        Debug.Log($"❌ No valid placement found for {unitAI.unitName}. Returning to original position.");
        transform.position = oldPosition;

        if (originalTile != null)
        {
            if (originalTile.TryClaim(unitAI))
            {
                if (originalTile.tileType == TileType.Board)
                {
                    unitAI.currentState = UnitState.BoardIdle;
                    RegisterUnit(unitAI, unitAI.team == Team.Player);
                }
                else if (originalTile.tileType == TileType.Bench)
                {
                    unitAI.currentState = UnitState.Bench;
                    UnregisterUnit(unitAI);
                }
            }
        }

        originalTile = null;
    }

    // ✅ Helper methods that work with both managers
    private void RegisterUnit(UnitAI unit, bool isPlayer)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterUnit(unit, isPlayer);
        else if (TestGameManager.Instance != null)
            TestGameManager.Instance.RegisterUnit(unit, isPlayer);
    }

    private void UnregisterUnit(UnitAI unit)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterUnit(unit);
        else if (TestGameManager.Instance != null)
            TestGameManager.Instance.UnregisterUnit(unit);
    }

    private void TryMergeUnits(UnitAI unit)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.TryMergeUnits(unit);
        else if (TestGameManager.Instance != null)
            TestGameManager.Instance.TryMergeUnits(unit);
    }

    private List<UnitAI> GetPlayerUnits()
    {
        if (GameManager.Instance != null)
            return GameManager.Instance.GetPlayerUnits();
        else if (TestGameManager.Instance != null)
            return TestGameManager.Instance.GetPlayerUnits();

        return new List<UnitAI>();
    }

    private void UpdateFightButtonVisibility()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateFightButtonVisibility();
        else if (TestUIManager.Instance != null)
            TestUIManager.Instance.UpdateFightButtonVisibility();
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
