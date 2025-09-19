using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

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

    public void SpawnEnemyWave(int stage, int round)
    {
        ClearEnemies();

        // ✅ Base enemy count starts at 2
        int enemyCount = 2 + ((stage - 1) * 3) + (round - 1);

        // ✅ Cap at 9 enemies
        enemyCount = Mathf.Min(enemyCount, 9);

        Debug.Log($"🌊 Spawning {enemyCount} enemies for Stage {stage} Round {round}");

        // ✅ Track used tiles to prevent overlaps
        HashSet<HexTile> usedTiles = new HashSet<HexTile>();

        for (int i = 0; i < enemyCount; i++)
        {
            ShopUnit chosenUnit = GetRandomUnitByStage(stage);

            // ✅ FIXED: Get a free tile that hasn't been used this spawn cycle
            HexTile freeTile = GetNextAvailableEnemyTile(usedTiles);
            if (freeTile == null)
            {
                Debug.LogWarning($"⚠️ No more free enemy tiles available! Spawned {i}/{enemyCount} enemies.");
                break;
            }

            // ✅ Mark this tile as used immediately
            usedTiles.Add(freeTile);

            // ✅ Spawn enemy at the exact tile position
            Vector3 spawnPosition = freeTile.transform.position;
            spawnPosition.y = 0.6f; // Keep enemies above ground

            GameObject enemyObj = Instantiate(chosenUnit.prefab, spawnPosition, Quaternion.identity);

            UnitAI enemyAI = enemyObj.GetComponent<UnitAI>();
            enemyAI.team = Team.Enemy;
            enemyAI.teamID = 1;
            enemyAI.SetState(UnitAI.UnitState.BoardIdle);
            enemyObj.transform.rotation = Quaternion.Euler(0, -90f, 0);

            // ✅ Properly assign enemy to the tile using the TryClaim system
            if (freeTile.TryClaim(enemyAI))
            {
                Debug.Log($"✅ Enemy {chosenUnit.unitName} assigned to tile at {freeTile.gridPosition}");
            }
            else
            {
                Debug.LogError($"❌ Failed to claim tile for enemy {chosenUnit.unitName}! Tile was supposedly free.");
                // Clean up the failed spawn
                Destroy(enemyObj);
                continue;
            }

            // ✅ Register with GameManager so combat sees them
            activeEnemies.Add(enemyAI);
            GameManager.Instance.RegisterUnit(enemyAI, false);
        }

        Debug.Log($"✅ Successfully spawned {activeEnemies.Count} enemies for Stage {stage} Round {round}");

        // ✅ Debug check for overlaps
        VerifyNoOverlaps();
    }

    // ✅ NEW: Get the next available enemy tile that hasn't been used
    private HexTile GetNextAvailableEnemyTile(HashSet<HexTile> usedTiles)
    {
        List<HexTile> enemyTiles = BoardManager.Instance.GetEnemyTiles();

        // Filter out tiles that are occupied OR already used in this spawn cycle
        List<HexTile> availableTiles = new List<HexTile>();

        foreach (var tile in enemyTiles)
        {
            if (tile.occupyingUnit == null && !usedTiles.Contains(tile))
            {
                availableTiles.Add(tile);
            }
        }

        if (availableTiles.Count == 0)
        {
            Debug.LogWarning("⚠️ No available enemy tiles remaining!");
            return null;
        }

        // Return a random available tile
        return availableTiles[Random.Range(0, availableTiles.Count)];
    }

    // ✅ NEW: Debug method to verify no enemies are overlapping
    private void VerifyNoOverlaps()
    {
        Dictionary<Vector3, List<UnitAI>> positionGroups = new Dictionary<Vector3, List<UnitAI>>();

        foreach (var enemy in activeEnemies)
        {
            if (enemy == null) continue;

            Vector3 pos = enemy.transform.position;
            // Round position to avoid floating point precision issues
            Vector3 roundedPos = new Vector3(
                Mathf.Round(pos.x * 100f) / 100f,
                Mathf.Round(pos.y * 100f) / 100f,
                Mathf.Round(pos.z * 100f) / 100f
            );

            if (!positionGroups.ContainsKey(roundedPos))
                positionGroups[roundedPos] = new List<UnitAI>();

            positionGroups[roundedPos].Add(enemy);
        }

        // Check for overlaps
        foreach (var kvp in positionGroups)
        {
            if (kvp.Value.Count > 1)
            {
                Debug.LogError($"🚨 OVERLAP DETECTED at position {kvp.Key}:");
                foreach (var unit in kvp.Value)
                {
                    Debug.LogError($"  - {unit.unitName} (tile: {unit.currentTile?.gridPosition})");
                }
            }
        }
    }

    public void ClearEnemies()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                // ✅ Properly free the tile before destroying
                if (enemy.currentTile != null)
                {
                    enemy.currentTile.Free(enemy);
                }
                Destroy(enemy.gameObject);
            }
        }
        activeEnemies.Clear();
        Debug.Log("🧹 All enemies cleared and tiles freed");
    }

    // ✅ REMOVED: Old GetFreeEnemyHex method since it was causing the issue

    public List<UnitAI> GetActiveEnemies()
    {
        return activeEnemies;
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
