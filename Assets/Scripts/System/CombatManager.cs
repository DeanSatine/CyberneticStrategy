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
    public void StartCombat()
    {
        Debug.Log("⚔️ Combat started!");

        foreach (var unit in FindObjectsOfType<UnitAI>())
        {
            if (unit.isAlive && unit.currentState != UnitAI.UnitState.Bench)
            {
                unit.SetState(UnitAI.UnitState.Combat);
            }
        }
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
            Debug.Log("✅ Player wins the round!");
            StageManager.Instance.NextRound();
        }
        else if (!anyPlayersAlive)
        {
            Debug.Log("❌ Player loses the round!");
            StageManager.Instance.NextRound();
        }
    }
}
