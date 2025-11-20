using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;


public class PvPPlayerState
{
    public int actorNumber;
    public string playerName;
    public int health = 100;
    public bool isAlive = true;
    public int currentRound = 1;
    public Vector3 boardPosition;

    public List<UnitSyncData> currentBoard = new List<UnitSyncData>();

    [System.Serializable]
    public class UnitSyncData
    {
        public string unitName;
        public int starLevel;
        public Vector2Int gridPosition;
        public float currentHealth;
        public float maxHealth;

        public UnitSyncData(UnitAI unit)
        {
            unitName = unit.unitName;
            starLevel = unit.starLevel;
            gridPosition = unit.currentTile != null ? unit.currentTile.gridPosition : Vector2Int.zero;
            currentHealth = unit.currentHealth;
            maxHealth = unit.maxHealth;
        }
    }

    public void UpdateBoardState(List<UnitAI> units)
    {
        currentBoard.Clear();
        foreach (var unit in units)
        {
            // ✅ Only sync units that are on the board (not on bench)
            if (unit != null && unit.isAlive && unit.currentState != UnitAI.UnitState.Bench)
            {
                currentBoard.Add(new UnitSyncData(unit));
            }
        }

        Debug.Log($"📋 Updated board state: {currentBoard.Count} units on board (excluding bench)");
    }


    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            health = 0;
            isAlive = false;
            Debug.Log($"💀 {playerName} has been eliminated!");
        }
    }

    private void SpawnOpponentUnits(PvPPlayerState opponentState)
    {
        Debug.Log($"🎯 SpawnOpponentUnits() CALLED for opponent with {opponentState.currentBoard.Count} units");

        if (BoardManager.Instance == null)
        {
            Debug.LogError("❌ BoardManager.Instance is NULL!");
            return;
        }

        List<HexTile> enemyTiles = BoardManager.Instance.GetEnemyTiles();
        Debug.Log($"📍 Found {enemyTiles.Count} enemy tiles");

        for (int i = 0; i < opponentState.currentBoard.Count; i++)
        {
            PvPPlayerState.UnitSyncData unitData = opponentState.currentBoard[i];

            HexTile spawnTile = enemyTiles.Find(t => t.gridPosition == unitData.gridPosition);

            if (spawnTile == null)
            {
                Debug.LogWarning($"⚠️ No enemy tile at grid {unitData.gridPosition} - using index {i}");
                if (i < enemyTiles.Count)
                    spawnTile = enemyTiles[i];
                else
                    continue;
            }

            ShopCard unitCard = System.Array.Find(ShopManager.Instance.availableCards,
                card => card.cardPrefab != null &&
                        card.cardPrefab.GetComponent<ShopSlotUI>() != null &&
                        card.cardPrefab.GetComponent<ShopSlotUI>().unitPrefab != null &&
                        card.cardPrefab.GetComponent<ShopSlotUI>().unitPrefab.GetComponent<UnitAI>() != null &&
                        card.cardPrefab.GetComponent<ShopSlotUI>().unitPrefab.GetComponent<UnitAI>().unitName == unitData.unitName);

            if (unitCard == null)
            {
                Debug.LogError($"❌ Could not find card for unit: {unitData.unitName}");
                continue;
            }

            GameObject unitPrefab = unitCard.cardPrefab.GetComponent<ShopSlotUI>().unitPrefab;
            GameObject enemyObj = Object.Instantiate(unitPrefab, spawnTile.transform.position, Quaternion.Euler(0f, 90f, 0f));

            UnitAI enemyAI = enemyObj.GetComponent<UnitAI>();

            if (enemyAI != null)
            {
                enemyAI.team = Team.Enemy;
                enemyAI.teamID = 2;
                enemyAI.starLevel = unitData.starLevel;
                enemyAI.currentHealth = unitData.currentHealth;
                enemyAI.maxHealth = unitData.maxHealth;

                if (spawnTile.TryClaim(enemyAI))
                {
                    enemyAI.AssignToTile(spawnTile);
                    enemyAI.currentState = UnitAI.UnitState.BoardIdle;
                    GameManager.Instance.RegisterUnit(enemyAI, false);
                    Debug.Log($"   ✅ {unitData.unitName} ⭐{unitData.starLevel} spawned at {spawnTile.name}");
                }
                else
                {
                    Debug.LogError($"   ❌ Failed to claim tile for {unitData.unitName}");
                    Object.Destroy(enemyObj);
                }
            }
            else
            {
                Debug.LogError($"   ❌ Spawned object has no UnitAI component!");
                Object.Destroy(enemyObj);
            }
        }

        Debug.Log($"✅ Spawn complete - {opponentState.currentBoard.Count} units");
    }

}
