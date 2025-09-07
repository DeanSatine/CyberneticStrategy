using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    private void Awake()
    {
        Instance = this;
    }
    public Dictionary<UnitAI, HexTile> savedPlayerPositions = new Dictionary<UnitAI, HexTile>();

    public void StartCombat()
    {
        Debug.Log("⚔️ Combat started!");

        savedPlayerPositions.Clear(); // reset from previous round

        foreach (var unit in FindObjectsOfType<UnitAI>())
        {
            if (unit.isAlive && unit.team == Team.Player && unit.currentState != UnitAI.UnitState.Bench)
            {
                // save where they were before combat
                if (unit.currentTile != null)
                    savedPlayerPositions[unit] = unit.currentTile;

                unit.SetState(UnitAI.UnitState.Combat);
            }
        }
    }

    public Dictionary<UnitAI, HexTile> GetSavedPlayerPositions()
    {
        return savedPlayerPositions;
    }


    private void OnEnable()
    {
        UnitAI.OnAnyUnitDeath += HandleUnitDeath;
    }

    private void OnDisable()
    {
        UnitAI.OnAnyUnitDeath -= HandleUnitDeath;
    }

    private void HandleUnitDeath(UnitAI deadUnit)
    {
        CheckForRoundEnd();
    }

    private void CheckForRoundEnd()
    {
        List<UnitAI> allUnits = FindObjectsOfType<UnitAI>().ToList();

        bool anyPlayersAlive = allUnits.Any(u => u.team == Team.Player && u.isAlive);
        bool anyEnemiesAlive = allUnits.Any(u => u.team == Team.Enemy && u.isAlive);

        if (!anyEnemiesAlive)
        {
            StageManager.Instance.OnCombatEnd(true);
        }
        else if (!anyPlayersAlive)
        {
            StageManager.Instance.OnCombatEnd(false);
        }
    }
}
