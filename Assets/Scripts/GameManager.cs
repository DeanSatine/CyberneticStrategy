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

    //HexGrid
    public GameObject HexMap;

    //Scripts
    public Draggable draggable;

    private bool combatStarted = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // TEMP: press space to test
        {
            StartCombat();
        }
    }

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
    }

    public void UnregisterUnit(UnitAI unit)
    {
        playerUnits.Remove(unit);
        enemyUnits.Remove(unit);
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
    }


    private void GridShow()
    {
        if (draggable.isDragging == true)
        {
            HexMap.SetActive(true);
        }
        else
        {
            HexMap.SetActive(false);
        }


            
    }
   
}
