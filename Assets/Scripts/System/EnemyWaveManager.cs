using UnityEngine;
using System.Collections.Generic;

public class EnemyWaveManager : MonoBehaviour
{
    public static EnemyWaveManager Instance;

    [Header("Enemy Spawn Settings")]
    public Transform[] enemyHexes; // assign enemy-side hexes in Inspector
    public int minUnits = 2;
    public int maxUnits = 5;

    [Header("Unit Pool (same as Shop)")]
    public List<ShopUnit> allUnits; // reference your shop units list

    private List<UnitAI> activeEnemies = new List<UnitAI>();

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnEnemyWave(int stage)
    {
        ClearOldWave();

        int enemyCount = Random.Range(minUnits, maxUnits + 1);

        for (int i = 0; i < enemyCount; i++)
        {
            ShopUnit chosenUnit = GetRandomUnitByStage(stage);

            // pick a random free hex
            Transform hex = GetFreeEnemyHex();
            if (hex == null) break;

            // after you pick: HexTile chosen = BoardManager.Instance.GetEnemyTiles()...
            HexTile chosen = BoardManager.Instance.GetEnemyTiles().Find(t => t.occupyingUnit == null);
            if (chosen == null) break;

            GameObject enemyObj = Instantiate(chosenUnit.prefab, hex.position, Quaternion.identity);

            // keep enemies on ground
            Vector3 pos = enemyObj.transform.position;
            pos.y = 0.6f;
            enemyObj.transform.position = pos;

            UnitAI enemyAI = enemyObj.GetComponent<UnitAI>();
            enemyAI.team = Team.Enemy;
            enemyAI.teamID = 1;
            enemyAI.SetState(UnitAI.UnitState.BoardIdle);
            enemyObj.transform.rotation = Quaternion.Euler(0, -90f, 0);

            // ✅ assign enemy to its hex
            HexTile tile = BoardManager.Instance.GetTileFromWorld(hex.position);
            if (tile != null)
            {
                enemyAI.currentTile = tile;
                tile.occupyingUnit = enemyAI;
            }

            // ✅ register with GameManager so combat sees them
            activeEnemies.Add(enemyAI);
            GameManager.Instance.RegisterUnit(enemyAI, false);

        }

        Debug.Log($"Spawned {activeEnemies.Count} enemies for Stage {stage}");
    }

    private void ClearOldWave()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }
        activeEnemies.Clear();
    }

    private Transform GetFreeEnemyHex()
    {
        List<HexTile> enemyTiles = BoardManager.Instance.GetEnemyTiles()
            .FindAll(t => t.occupyingUnit == null);

        if (enemyTiles.Count == 0) return null;

        HexTile chosen = enemyTiles[Random.Range(0, enemyTiles.Count)];
        return chosen.transform;
    }

    private ShopUnit GetRandomUnitByStage(int stage)
    {
        int roll = Random.Range(0, 100);
        int chosenCost = 1;

        if (stage == 1)
        {
            chosenCost = (roll < 70) ? 1 : 2;
        }
        else if (stage == 2)
        {
            if (roll < 30) chosenCost = 1;
            else if (roll < 70) chosenCost = 2;
            else if (roll < 90) chosenCost = 3;
            else chosenCost = 4;
        }
        else if (stage >= 3)
        {
            if (roll < 10) chosenCost = 1;
            else if (roll < 30) chosenCost = 2;
            else if (roll < 60) chosenCost = 3;
            else if (roll < 85) chosenCost = 4;
            else chosenCost = 5;
        }

        List<ShopUnit> valid = allUnits.FindAll(u => u.cost == chosenCost);
        if (valid.Count == 0) return allUnits[0];
        return valid[Random.Range(0, valid.Count)];
    }
}
