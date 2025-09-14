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

        // ✅ Reset lingering trait visuals at round start
        EradicatorTrait.ResetAllEradicators();

        savedPlayerPositions.Clear();

        foreach (var unit in FindObjectsOfType<UnitAI>())
        {
            if (!unit.isAlive || unit.currentState == UnitAI.UnitState.Bench)
                continue;

            if (unit.team == Team.Player && unit.currentTile != null)
                savedPlayerPositions[unit] = unit.currentTile;

            unit.SetState(UnitAI.UnitState.Combat);
            Debug.Log($"✅ Set {unit.team} unit {unit.unitName} to Combat state");
        }
    }

    public void ClearProjectiles()
    {
        GameObject[] projectiles = GameObject.FindGameObjectsWithTag("Projectile");

        foreach (var proj in projectiles)
        {
            Destroy(proj);
        }

        Debug.Log($"🧹 Cleared {projectiles.Length} projectiles at round reset");
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

        // ✅ Only count units that are alive AND not benched
        bool anyPlayersAlive = allUnits.Any(u =>
            u.team == Team.Player &&
            u.isAlive &&
            u.currentState != UnitAI.UnitState.Bench);

        bool anyEnemiesAlive = allUnits.Any(u =>
            u.team == Team.Enemy &&
            u.isAlive &&
            u.currentState != UnitAI.UnitState.Bench);

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
