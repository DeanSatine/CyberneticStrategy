using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyWaveManager : MonoBehaviour
{
    public static EnemyWaveManager Instance;

    [Header("Enemy Spawn Settings")]
    public Transform[] enemyHexes;
    public int minUnits = 2;
    public int maxUnits = 5;

    [Header("Difficulty Scaling Settings")]
    [Tooltip("Bonus health per round after maxing out enemy count")]
    public float healthBonusPerRound = 100f;
    [Tooltip("Bonus attack damage per stack (10, 20, 30, etc.)")]
    public float attackDamageBonusPerStack = 10f;
    [Tooltip("Bonus attack speed percent per stack (0.1 = 10%, 0.2 = 20%, etc.)")]
    public float attackSpeedBonusPerStack = 0.1f;

    [Header("Unit Pool (same as Shop)")]
    public List<ShopUnit> allUnits;

    private List<UnitAI> activeEnemies = new List<UnitAI>();

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnEnemyWave(int stage, int round)
    {
        if (StageManager.Instance != null && 
            StageManager.Instance.currentPhase != StageManager.GamePhase.Prep)
        {
            Debug.LogWarning($"⚠️ Attempted to spawn enemy wave during {StageManager.Instance.currentPhase} phase - aborting!");
            return;
        }
        
        Debug.Log($"🌊 Starting SpawnEnemyWave for Stage {stage} Round {round}");
        
        ClearEnemies();
        List<HexTile> enemyTiles = BoardManager.Instance.GetEnemyTiles();
        foreach (var tile in enemyTiles)
        {
            if (tile.occupyingUnit != null && tile.occupyingUnit.team == Team.Player)
            {
                Debug.LogWarning($"🧹 Found player unit {tile.occupyingUnit.unitName} still on enemy tile {tile.gridPosition} - force-freeing!");
                tile.occupyingUnit.currentTile = null;  // Clear the unit's reference
                tile.occupyingUnit = null;  // Clear the tile's reference
            }
        }
        int baseEnemyCount = 2 + ((stage - 1) * 3) + (round - 1);
        int actualEnemyCount = Mathf.Min(baseEnemyCount, 9);

        int difficultyStacks = CalculateDifficultyStacks(stage, round, baseEnemyCount);
        float healthBonus = difficultyStacks * healthBonusPerRound;
        float attackDamageBonus = difficultyStacks * attackDamageBonusPerStack;
        float attackSpeedBonus = difficultyStacks * attackSpeedBonusPerStack;

        Debug.Log($"🌊 Spawning {actualEnemyCount} enemies for Stage {stage} Round {round}");

        if (difficultyStacks > 0)
        {
            Debug.Log($"💪 Difficulty Stack {difficultyStacks}: +{healthBonus} HP, +{attackDamageBonus} AD, +{attackSpeedBonus * 100f}% AS");
        }

        HashSet<HexTile> usedTiles = new HashSet<HexTile>();

        for (int i = 0; i < actualEnemyCount; i++)
        {
            ShopUnit chosenUnit = GetRandomUnitByStage(stage);

            HexTile freeTile = GetNextAvailableEnemyTile(usedTiles);
            if (freeTile == null)
            {
                Debug.LogError($"⚠️ No more free enemy tiles! Checking all enemy tiles:");
                List<HexTile> allEnemyTiles = BoardManager.Instance.GetEnemyTiles();
                foreach (var tile in allEnemyTiles)
                {
                    Debug.LogError($"  Tile {tile.gridPosition}: occupied={tile.occupyingUnit != null}, occupant={(tile.occupyingUnit != null ? tile.occupyingUnit.unitName : "NONE")}");
                }
                break;
            }

            usedTiles.Add(freeTile);

            Vector3 spawnPosition = freeTile.transform.position;
            spawnPosition.y = 0.6f;

            GameObject enemyObj = Instantiate(chosenUnit.prefab, spawnPosition, Quaternion.identity);

            UnitAI enemyAI = enemyObj.GetComponent<UnitAI>();
            enemyAI.team = Team.Enemy;
            enemyAI.teamID = 1;
            enemyAI.SetState(UnitAI.UnitState.BoardIdle);
            enemyObj.transform.rotation = Quaternion.Euler(0, -90f, 0);

            if (freeTile.TryClaim(enemyAI))
            {
                Debug.Log($"✅ Enemy {chosenUnit.unitName} assigned to tile at {freeTile.gridPosition}");
            }
            else
            {
                Debug.LogError($"❌ Failed to claim tile for enemy {chosenUnit.unitName}! Tile was supposedly free.");
                Destroy(enemyObj);
                continue;
            }

            activeEnemies.Add(enemyAI);
            GameManager.Instance.RegisterUnit(enemyAI, false);
        }

        if (difficultyStacks > 0)
        {
            StartCoroutine(ApplyDifficultyBonusesDelayed(healthBonus, attackDamageBonus, attackSpeedBonus, difficultyStacks));
        }

        Debug.Log($"✅ Successfully spawned {activeEnemies.Count} enemies for Stage {stage} Round {round}");

        VerifyNoOverlaps();
    }

    private System.Collections.IEnumerator ApplyDifficultyBonusesDelayed(float healthBonus, float attackDamageBonus, float attackSpeedBonus, int stacks)
    {
        yield return null;

        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                ApplyDifficultyBonuses(enemy, healthBonus, attackDamageBonus, attackSpeedBonus);
            }
        }

        Debug.Log($"✅ Applied difficulty bonuses (Stack {stacks}) to {activeEnemies.Count} enemies");
    }

    private void ApplyDifficultyBonuses(UnitAI enemy, float healthBonus, float attackDamageBonus, float attackSpeedBonus)
    {
        float originalMaxHealth = enemy.maxHealth;
        float originalAttackDamage = enemy.attackDamage;
        float originalAttackSpeed = enemy.attackSpeed;

        enemy.bonusMaxHealth += healthBonus;
        enemy.RecalculateMaxHealth();
        enemy.currentHealth = enemy.maxHealth;

        enemy.attackDamage += attackDamageBonus;
        enemy.attackSpeed += originalAttackSpeed * attackSpeedBonus;

        if (enemy.ui != null)
        {
            enemy.ui.UpdateHealth(enemy.currentHealth);
        }

        Debug.Log($"💪 {enemy.unitName} buffed: HP {originalMaxHealth} → {enemy.maxHealth}, AD {originalAttackDamage} → {enemy.attackDamage:F1}, AS {originalAttackSpeed:F2} → {enemy.attackSpeed:F2}");
    }

    private int CalculateDifficultyStacks(int stage, int round, int baseEnemyCount)
    {
        if (baseEnemyCount > 9)
        {
            return baseEnemyCount - 9;
        }

        return 0;
    }

    private HexTile GetNextAvailableEnemyTile(HashSet<HexTile> usedTiles)
    {
        List<HexTile> enemyTiles = BoardManager.Instance.GetEnemyTiles();

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

        return availableTiles[Random.Range(0, availableTiles.Count)];
    }

    private void VerifyNoOverlaps()
    {
        Dictionary<Vector3, List<UnitAI>> positionGroups = new Dictionary<Vector3, List<UnitAI>>();

        foreach (var enemy in activeEnemies)
        {
            if (enemy == null) continue;

            Vector3 pos = enemy.transform.position;
            Vector3 roundedPos = new Vector3(
                Mathf.Round(pos.x * 100f) / 100f,
                Mathf.Round(pos.y * 100f) / 100f,
                Mathf.Round(pos.z * 100f) / 100f
            );

            if (!positionGroups.ContainsKey(roundedPos))
                positionGroups[roundedPos] = new List<UnitAI>();

            positionGroups[roundedPos].Add(enemy);
        }

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

    public List<UnitAI> GetActiveEnemies()
    {
        return activeEnemies;
    }

    public string GetEnemyScalingInfo(int stage, int round)
    {
        int baseEnemyCount = 2 + ((stage - 1) * 3) + (round - 1);
        int actualCount = Mathf.Min(baseEnemyCount, 9);
        int stacks = CalculateDifficultyStacks(stage, round, baseEnemyCount);

        if (stacks > 0)
        {
            float hp = stacks * healthBonusPerRound;
            float ad = stacks * attackDamageBonusPerStack;
            float as_percent = stacks * attackSpeedBonusPerStack * 100f;
            return $"Enemies: {actualCount} units (+{hp} HP, +{ad} AD, +{as_percent:F0}% AS)";
        }
        else
        {
            return $"Enemies: {actualCount} units";
        }
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
