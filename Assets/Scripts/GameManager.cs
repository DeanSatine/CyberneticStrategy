using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public List<UnitAI> playerUnits = new List<UnitAI>();
    private List<UnitAI> enemyUnits = new List<UnitAI>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- Register / Unregister ---
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

    // --- Access ---
    public List<UnitAI> GetPlayerUnits() => playerUnits;
    public List<UnitAI> GetEnemyUnits() => enemyUnits;

    // --- Reset Player Units after combat ---
    public void ResetPlayerUnits()
    {
        foreach (var unit in playerUnits)
        {
            if (unit != null && unit.isAlive)
            {
                unit.ResetAfterCombat();        // stop attacking
                if (unit.startingTile != null)  // snap back to saved tile
                    unit.AssignToTile(unit.startingTile);
            }
        }
    }
}
