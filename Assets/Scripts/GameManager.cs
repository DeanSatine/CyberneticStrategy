using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnitAI;

public class GameManager : Singleton<GameManager>
{
    //CharacterSpace
    public GameObject selectionTable;
    public List<GameObject> selections;

    public List<UnitAI> playerUnits = new List<UnitAI>();
    public List<UnitAI> enemyUnits = new List<UnitAI>();

    //Scripts
    public Draggable draggable;

    private bool combatStarted = false;

    public void RegisterUnit(UnitAI unit, bool isPlayer)
    {
        if (isPlayer)
        {
            if (!playerUnits.Contains(unit))
                playerUnits.Add(unit);
        }
        else
        {
            if (!enemyUnits.Contains(unit))
                enemyUnits.Add(unit);
        }

        // ✅ update trait UI
        TraitManager.Instance.EvaluateTraits(GetActivePlayerUnits());

        // ✅ apply trait abilities in planning phase
        TraitManager.Instance.ApplyTraits(playerUnits);
    }
    public void ResetPlayerUnits()
    {
        foreach (var unit in playerUnits)
        {
            if (unit == null) continue;
            if (!unit.isAlive) continue;

            // return unit to its hex position, idle state
            if (unit.currentTile != null)
            {
                unit.transform.position = unit.currentTile.transform.position + Vector3.up * 0.6f;
            }
            unit.SetState(UnitAI.UnitState.BoardIdle);
        }
    }
    public void UnregisterUnit(UnitAI unit)
    {
        playerUnits.Remove(unit);
        enemyUnits.Remove(unit);

        // ✅ Only count Board units
        TraitManager.Instance.EvaluateTraits(GetActivePlayerUnits());

        // ✅ also update trait abilities in planning phase
        TraitManager.Instance.ApplyTraits(playerUnits);
    }


    public List<UnitAI> GetActivePlayerUnits()
    {
        return playerUnits.FindAll(u =>
            u != null &&
            u.isAlive &&
            (u.currentState == UnitAI.UnitState.BoardIdle || u.currentState == UnitAI.UnitState.Combat)
        );
    }

    public void StartCombat()
    {
        if (combatStarted) return;
        combatStarted = true;

        Debug.Log("Combat started!");

        foreach (var unit in playerUnits)
        {
            if (unit != null && unit.isAlive)
                unit.currentState = UnitState.Combat;
        }

        foreach (var unit in enemyUnits)
        {
            if (unit != null && unit.isAlive)
                unit.currentState = UnitState.Combat;
        }

        if (TraitManager.Instance != null)
        {
            TraitManager.Instance.ApplyTraits(playerUnits);
            TraitManager.Instance.ApplyTraits(enemyUnits);
        }
    }

    public void ResetCombatFlag()
    {
        combatStarted = false; // ✅ reset between rounds
    }
}
