using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UnitData
{
    public string unitId;
    public int starLevel;
    public Vector2Int gridPosition;
    public bool isOnBench;

    public UnitData(string id, int stars, Vector2Int pos, bool bench)
    {
        unitId = id;
        starLevel = stars;
        gridPosition = pos;
        isOnBench = bench;
    }
}

public class PlayerState
{
    public string playerId;
    public string playerName;
    public int health;
    public int gold;
    public bool isAlive;
    public int placement;

    public List<UnitData> boardUnits = new List<UnitData>();
    public List<UnitData> benchUnits = new List<UnitData>();

    public PlayerState(string id, string name)
    {
        playerId = id;
        playerName = name;
        health = 100;
        gold = 5;
        isAlive = true;
        placement = 0;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            health = 0;
            isAlive = false;
        }
        Debug.Log($"💔 {playerName} took {damage} damage! Health: {health}");
    }

    public void UpdateBoard(List<UnitData> board, List<UnitData> bench)
    {
        boardUnits = new List<UnitData>(board);
        benchUnits = new List<UnitData>(bench);
    }
}
