using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class NetworkedBattleManager : MonoBehaviourPunCallbacks
{
    public static NetworkedBattleManager Instance;

    [Header("Battle Settings")]
    public float battleStartDelay = 2f;

    [Header("Unit Registry")]
    public List<ShopUnit> allUnits;

    private Player currentOpponent;
    private Dictionary<int, PlayerBoardState> playerBoards = new Dictionary<int, PlayerBoardState>();
    private bool isBattleActive = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartNetworkedBattle()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.PlayerList.Length < 2)
        {
            Debug.LogWarning("⚠️ Not enough players! Starting AI battle fallback.");
            StartAIBattle();
            return;
        }

        MatchOpponent();
        SendBoardStateToOpponent();

        Invoke(nameof(BeginBattle), battleStartDelay);
    }

    private void MatchOpponent()
    {
        Player[] players = PhotonNetwork.PlayerList;

        foreach (Player player in players)
        {
            if (player != PhotonNetwork.LocalPlayer)
            {
                currentOpponent = player;
                Debug.Log($"⚔️ Matched against: {currentOpponent.NickName}");
                break;
            }
        }

        if (currentOpponent == null)
        {
            Debug.LogWarning("⚠️ No opponent found!");
        }
    }

    private void SendBoardStateToOpponent()
    {
        List<UnitAI> playerUnits = GameManager.Instance.playerUnits;

        List<UnitSyncData> unitData = new List<UnitSyncData>();

        foreach (var unit in playerUnits)
        {
            if (unit.currentState == UnitAI.UnitState.BoardIdle && unit.currentTile != null)
            {
                unitData.Add(new UnitSyncData(unit));
            }
        }

        string serializedData = JsonUtility.ToJson(new UnitSyncDataList(unitData));

        photonView.RPC("RPC_ReceiveOpponentBoard", RpcTarget.Others,
            PhotonNetwork.LocalPlayer.ActorNumber, serializedData);

        Debug.Log($"📡 Sent board state with {unitData.Count} units to opponent");
    }

    [PunRPC]
    private void RPC_ReceiveOpponentBoard(int opponentActorNumber, string serializedData)
    {
        UnitSyncDataList dataList = JsonUtility.FromJson<UnitSyncDataList>(serializedData);

        PlayerBoardState boardState = new PlayerBoardState
        {
            actorNumber = opponentActorNumber,
            units = dataList.units
        };

        playerBoards[opponentActorNumber] = boardState;

        Debug.Log($"📥 Received opponent board with {dataList.units.Count} units");
    }

    private void BeginBattle()
    {
        if (currentOpponent == null)
        {
            StartAIBattle();
            return;
        }

        if (!playerBoards.ContainsKey(currentOpponent.ActorNumber))
        {
            Debug.LogWarning($"⚠️ Haven't received board from {currentOpponent.NickName} - waiting...");
            Invoke(nameof(BeginBattle), 1f);
            return;
        }

        SpawnOpponentUnits(currentOpponent.ActorNumber);
        StartCombat();
    }

    private void SpawnOpponentUnits(int opponentActorNumber)
    {
        PlayerBoardState opponentBoard = playerBoards[opponentActorNumber];
        List<HexTile> enemyTiles = BoardManager.Instance.GetEnemyTiles();

        foreach (var tile in enemyTiles)
        {
            if (tile.occupyingUnit != null && tile.occupyingUnit.team == Team.Player)
            {
                tile.occupyingUnit.currentTile = null;
                tile.occupyingUnit = null;
            }
        }

        foreach (var unitData in opponentBoard.units)
        {
            HexTile spawnTile = GetMirroredTile(unitData.gridPosition);

            if (spawnTile == null || spawnTile.occupyingUnit != null)
            {
                spawnTile = enemyTiles.FirstOrDefault(t => t.occupyingUnit == null);
            }

            if (spawnTile == null)
            {
                Debug.LogWarning($"⚠️ No free tile for opponent unit {unitData.unitId}");
                continue;
            }

            GameObject unitPrefab = GetUnitPrefabById(unitData.unitId);

            if (unitPrefab == null)
            {
                Debug.LogError($"❌ Could not find prefab for unit: {unitData.unitId}");
                continue;
            }

            Vector3 spawnPosition = spawnTile.transform.position;
            spawnPosition.y = 0.6f;

            GameObject enemyObj = Instantiate(unitPrefab, spawnPosition, Quaternion.identity);
            UnitAI enemyAI = enemyObj.GetComponent<UnitAI>();

            enemyAI.team = Team.Enemy;
            enemyAI.teamID = 1;
            enemyAI.starLevel = unitData.starLevel;
            enemyAI.SetState(UnitAI.UnitState.BoardIdle);
            enemyObj.transform.rotation = Quaternion.Euler(0, -90f, 0);

            enemyAI.RecalculateMaxHealth();
            enemyAI.currentHealth = enemyAI.maxHealth;

            if (spawnTile.TryClaim(enemyAI))
            {
                GameManager.Instance.RegisterUnit(enemyAI, false);
                Debug.Log($"✅ Spawned opponent's {unitData.unitId} ⭐{unitData.starLevel} at {spawnTile.gridPosition}");
            }
            else
            {
                Debug.LogError($"❌ Failed to claim tile for {unitData.unitId}");
                Destroy(enemyObj);
            }
        }
    }

    private GameObject GetUnitPrefabById(string unitId)
    {
        ShopUnit shopUnit = allUnits.FirstOrDefault(u => u.unitName == unitId);
        return shopUnit?.prefab;
    }

    private HexTile GetMirroredTile(Vector2Int playerTilePos)
    {
        List<HexTile> enemyTiles = BoardManager.Instance.GetEnemyTiles();
        List<HexTile> playerTiles = BoardManager.Instance.GetPlayerTiles();

        if (playerTiles.Count == 0 || enemyTiles.Count == 0)
            return enemyTiles.FirstOrDefault();

        float totalPlayerX = 0f;
        float totalEnemyX = 0f;

        foreach (var tile in playerTiles)
            totalPlayerX += tile.transform.position.x;
        foreach (var tile in enemyTiles)
            totalEnemyX += tile.transform.position.x;

        float avgPlayerX = totalPlayerX / playerTiles.Count;
        float avgEnemyX = totalEnemyX / enemyTiles.Count;

        Vector2Int mirroredPos = new Vector2Int(-playerTilePos.x, playerTilePos.y);

        HexTile closestTile = null;
        float closestDist = float.MaxValue;

        foreach (var tile in enemyTiles)
        {
            float dist = Vector2Int.Distance(tile.gridPosition, mirroredPos);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestTile = tile;
            }
        }

        return closestTile;
    }

    private void StartCombat()
    {
        isBattleActive = true;

        List<UnitAI> allUnits = FindObjectsOfType<UnitAI>().ToList();

        foreach (var unit in allUnits)
        {
            if (unit.currentState == UnitAI.UnitState.BoardIdle)
            {
                unit.SetState(UnitAI.UnitState.Combat);
            }
        }

        int enemyCount = allUnits.Count(u => u.team == Team.Enemy);
        Debug.Log($"⚔️ Battle started! {GameManager.Instance.playerUnits.Count} vs {enemyCount} units");
    }

    public void StartAIBattle()
    {
        if (EnemyWaveManager.Instance != null && StageManager.Instance != null)
        {
            Debug.Log("🤖 Starting AI battle (no opponent or fallback mode)");
            EnemyWaveManager.Instance.SpawnEnemyWave(
                StageManager.Instance.currentStage,
                StageManager.Instance.roundInStage
            );

            StartCombat();
        }
    }

    public void EndBattle(Team winningTeam)
    {
        isBattleActive = false;

        if (currentOpponent != null && PhotonNetwork.InRoom)
        {
            if (winningTeam == Team.Player)
            {
                Debug.Log("🎉 Victory!");
                photonView.RPC("RPC_ReportBattleResult", RpcTarget.Others, false);
            }
            else
            {
                Debug.Log("💀 Defeat!");
                photonView.RPC("RPC_ReportBattleResult", RpcTarget.Others, true);
            }
        }

        ClearEnemyBoard();
    }

    [PunRPC]
    private void RPC_ReportBattleResult(bool youWon)
    {
        if (youWon)
        {
            Debug.Log($"📩 Opponent was defeated by you");
        }
        else
        {
            Debug.Log($"📩 Opponent defeated you");
        }
    }

    private void ClearEnemyBoard()
    {
        List<UnitAI> enemyUnits = FindObjectsOfType<UnitAI>().Where(u => u.team == Team.Enemy).ToList();

        foreach (var enemy in enemyUnits)
        {
            if (enemy.currentTile != null)
            {
                enemy.currentTile.Free(enemy);
            }
            Destroy(enemy.gameObject);
        }

        Debug.Log("🧹 Enemy board cleared");
    }

    public bool IsNetworkedBattle()
    {
        return PhotonNetwork.InRoom && PhotonNetwork.PlayerList.Length >= 2;
    }
}

[System.Serializable]
public class UnitSyncData
{
    public string unitId;
    public int starLevel;
    public Vector2Int gridPosition;

    public UnitSyncData(UnitAI unit)
    {
        unitId = unit.unitName;
        starLevel = unit.starLevel;
        gridPosition = unit.currentTile != null ? unit.currentTile.gridPosition : Vector2Int.zero;
    }
}

[System.Serializable]
public class UnitSyncDataList
{
    public List<UnitSyncData> units;

    public UnitSyncDataList(List<UnitSyncData> unitList)
    {
        units = unitList;
    }
}

public class PlayerBoardState
{
    public int actorNumber;
    public List<UnitSyncData> units;
}
