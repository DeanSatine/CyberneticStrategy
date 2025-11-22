using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PvPGameManager : MonoBehaviourPunCallbacks
{
    public static PvPGameManager Instance;

    [Header("Round Settings")]
    public float prepPhaseTime = 30f;
    public float combatMaxTime = 60f;
    public int damagePerLoss = 10;
    [System.Serializable]
    public class UnitPositionDataList
    {
        public List<UnitPositionData> positions;
    }
    [Header("Arena System")]
    public PvPArenaManager arenaManager;

    [System.Serializable]
    public class UnitPositionData
    {
        public string unitPrefabName;
        public Vector2Int gridPosition;
        public int ownerActorNumber;
    }

    public Dictionary<int, PvPPlayerState> allPlayers = new Dictionary<int, PvPPlayerState>();

    [Header("Current State")]
    public PvPPhase currentPhase = PvPPhase.Waiting;
    public int currentRound = 0;
    public float phaseTimer = 0f;

    public Dictionary<int, int> currentMatchups = new Dictionary<int, int>();
    private int myHomeBoardIndex = -1;
    private PvPArenaManager.PlayerBoard myHomeBoard;

    public enum PvPPhase { Waiting, Prep, Combat, Results }

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

    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("❌ Not connected to Photon!");
            return;
        }

        InitializePlayers();

        if (PvPPlayerListUI.Instance != null)
        {
            PvPPlayerListUI.Instance.InitializePlayers(allPlayers);
        }

        // ✅ Wait for all players to join before starting
        StartCoroutine(WaitForPlayersAndStart());
    }

    private IEnumerator WaitForPlayersAndStart()
    {
        // Wait 2 seconds for all players to load scene
        yield return new WaitForSeconds(5f);

        // ✅ Spawn networked player avatar FAR AWAY initially
        Vector3 tempSpawnPos = new Vector3(-1000, 0, -1000); // Far away until board assigned
        GameObject myPlayer = PhotonNetwork.Instantiate("Player", tempSpawnPos, Quaternion.identity);
        PhotonNetwork.LocalPlayer.TagObject = myPlayer;
        myPlayer.tag = "Player";
        Debug.Log("✅ Spawned networked player avatar at temp location");

        // Master assigns boards and starts game
        if (PhotonNetwork.IsMasterClient)
        {
            yield return new WaitForSeconds(1f); // Give time for all clients to spawn
            AssignHomeBoards();
            yield return new WaitForSeconds(1f); // Give time for board assignment to sync
            StartCoroutine(MasterGameLoop());
        }
        else
        {
            // Non-master clients just wait for board assignment RPC
            Debug.Log("⏳ Waiting for master to assign boards...");
        }
    }



    private void AssignHomeBoards()
    {
        List<int> playerActorNumbers = PhotonNetwork.PlayerList
            .OrderBy(p => p.ActorNumber)
            .Select(p => p.ActorNumber)
            .ToList();

        arenaManager.AssignBoardsToPlayers(playerActorNumbers);

        int[] actorNumbers = playerActorNumbers.ToArray();
        photonView.RPC("RPC_SyncBoardAssignments", RpcTarget.All, actorNumbers);
    }
    [ContextMenu("Debug Tile Heights")]
    public void DebugTileHeights()
    {
        if (myHomeBoard == null)
        {
            Debug.LogError("❌ No home board assigned yet!");
            return;
        }

        List<HexTile> benchTiles = myHomeBoard.GetBenchTiles();
        Debug.Log($"=== BENCH TILE HEIGHTS ({benchTiles.Count} tiles) ===");

        foreach (HexTile tile in benchTiles)
        {
            Debug.Log($"{tile.name}: Position = {tile.transform.position}, Y = {tile.transform.position.y}");
        }

        List<HexTile> boardTiles = myHomeBoard.GetPlayerTiles();
        if (boardTiles.Count > 0)
        {
            Debug.Log($"Board tile example: {boardTiles[0].name} at Y = {boardTiles[0].transform.position.y}");
        }
    }

    [PunRPC]
    private void RPC_SyncBoardAssignments(int[] actorNumbers)
    {
        int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        myHomeBoardIndex = System.Array.IndexOf(actorNumbers, myActorNumber);

        Debug.Log($"🏠 RPC_SyncBoardAssignments received!");
        Debug.Log($"🏠 My ActorNumber: {myActorNumber}");
        Debug.Log($"🏠 My home board index: {myHomeBoardIndex}");
        Debug.Log($"🏠 Total boards available: {arenaManager.playerBoards.Count}");

        myHomeBoard = arenaManager.GetBoardByIndex(myHomeBoardIndex);

        if (myHomeBoard == null)
        {
            Debug.LogError($"❌ Could not get board at index {myHomeBoardIndex}!");
            return;
        }

        Debug.Log($"🏠 Got my board: {myHomeBoard.boardRoot.name}");

        LinkMyBoardToSystems();
        TeleportToMyHomeBoard();
    }


    private void LinkMyBoardToSystems()
    {
        if (myHomeBoard == null)
        {
            Debug.LogError("❌ My home board is null!");
            return;
        }

        Debug.Log($"🔗 Linking systems to my board: {myHomeBoard.boardRoot.name}");

        if (BoardManager.Instance != null)
        {
            List<HexTile> playerTiles = myHomeBoard.GetPlayerTiles();
            List<HexTile> enemyTiles = myHomeBoard.GetEnemyTiles();
            List<HexTile> benchTiles = myHomeBoard.GetBenchTiles();

            Debug.Log($"📊 Tiles found - Player: {playerTiles.Count}, Enemy: {enemyTiles.Count}, Bench: {benchTiles.Count}");

            BoardManager.Instance.OverrideTiles(playerTiles, enemyTiles, benchTiles);
            Debug.Log("✅ BoardManager linked to my board");
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetActiveBoardTiles(myHomeBoard.GetPlayerTiles());
            Debug.Log("✅ GameManager linked to my board");
        }

        // ✅ Link shop to assigned board's bench
        if (ShopManager.Instance != null)
        {
            List<HexTile> benchTiles = myHomeBoard.GetBenchTiles();

            if (benchTiles == null || benchTiles.Count == 0)
            {
                Debug.LogError($"❌ No bench tiles found for board {myHomeBoard.boardRoot.name}!");
                return;
            }

            ShopManager.Instance.SetBenchSlots(benchTiles);
            Debug.Log($"✅ ShopManager bench set with {benchTiles.Count} slots");

            ShopManager.Instance.InitializeForPvP();
            Debug.Log("✅ ShopManager shop generated");
        }
        else
        {
            Debug.LogError("❌ ShopManager.Instance is null!");
        }
    }



    private void TeleportToMyHomeBoard()
    {
        if (myHomeBoard == null) return;

        myHomeBoard.ActivateCamera(true);

        // Get MY networked player from TagObject
        GameObject player = PhotonNetwork.LocalPlayer.TagObject as GameObject;

        if (player != null)
        {
            // ✅ Get the board's actual ground level
            Vector3 spawnPos = myHomeBoard.boardRoot.position;

            // If board has tiles, use first tile's Y position
            List<HexTile> playerTiles = myHomeBoard.GetPlayerTiles();
            if (playerTiles.Count > 0)
            {
                spawnPos.y = playerTiles[0].transform.position.y + 1f; // 1 unit above tile
            }
            else
            {
                spawnPos.y += 1f; // Default: 1 unit above board root
            }

            spawnPos += new Vector3(0, 0, -5); // Move back from board

            player.transform.position = spawnPos;
            Debug.Log($"📍 Teleported player to {myHomeBoard.boardRoot.name} at {spawnPos}");
        }
        else
        {
            Debug.LogWarning("⚠️ Could not find my player GameObject!");
        }
    }




    private IEnumerator MasterGameLoop()
    {
        Debug.Log("🎮 PvP Master Game Loop started");

        while (GetAlivePlayers().Count > 1)
        {
            currentRound++;
            photonView.RPC("RPC_SyncRound", RpcTarget.All, currentRound);

            yield return StartCoroutine(MasterPrepPhase());
            yield return StartCoroutine(MasterCombatPhase());
            yield return StartCoroutine(ResultsPhase());
        }

        EndGame();
    }

    private IEnumerator MasterPrepPhase()
    {
        photonView.RPC("RPC_EnterPhase", RpcTarget.All, (int)PvPPhase.Prep);
        photonView.RPC("RPC_ReturnToHomeBoard", RpcTarget.All);

        phaseTimer = prepPhaseTime;

        while (phaseTimer > 0)
        {
            photonView.RPC("RPC_SyncTimer", RpcTarget.All, phaseTimer);
            phaseTimer -= Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator MasterCombatPhase()
    {
        GenerateMatchups();

        photonView.RPC("RPC_EnterPhase", RpcTarget.All, (int)PvPPhase.Combat);

        yield return new WaitForSeconds(1f);

        photonView.RPC("RPC_TeleportToCombat", RpcTarget.All);

        yield return new WaitForSeconds(3f);

        photonView.RPC("RPC_StartLiveCombat", RpcTarget.All);

        phaseTimer = combatMaxTime;

        while (phaseTimer > 0)
        {
            photonView.RPC("RPC_SyncTimer", RpcTarget.All, phaseTimer);
            phaseTimer -= Time.deltaTime;
            yield return null;
        }

        photonView.RPC("RPC_EndCombat", RpcTarget.All);
    }

    private void InitializePlayers()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            PvPPlayerState playerState = new PvPPlayerState
            {
                actorNumber = player.ActorNumber,
                playerName = player.NickName,
                health = 100,
                isAlive = true,
                currentRound = 0
            };

            allPlayers[player.ActorNumber] = playerState;
        }

        Debug.Log($"✅ Initialized {allPlayers.Count} players");
    }

    private IEnumerator ResultsPhase()
    {
        photonView.RPC("RPC_EnterPhase", RpcTarget.All, (int)PvPPhase.Results);

        if (PhotonNetwork.IsMasterClient)
        {
            ProcessCombatResults();
        }

        yield return new WaitForSeconds(5f);
    }

    private void GenerateMatchups()
    {
        List<int> alivePlayers = GetAlivePlayers();
        alivePlayers = alivePlayers.OrderBy(x => Random.value).ToList();

        currentMatchups.Clear();

        for (int i = 0; i < alivePlayers.Count; i += 2)
        {
            if (i + 1 < alivePlayers.Count)
            {
                int player1 = alivePlayers[i];
                int player2 = alivePlayers[i + 1];

                currentMatchups[player1] = player2;
                currentMatchups[player2] = player1;

                Debug.Log($"⚔️ Matchup: Player {player1} (visitor) vs Player {player2} (home)");
            }
            else
            {
                currentMatchups[alivePlayers[i]] = -1;
                Debug.Log($"😴 Player {alivePlayers[i]} gets a bye");
            }
        }

        photonView.RPC("RPC_SyncMatchups", RpcTarget.All, currentMatchups.Keys.ToArray(), currentMatchups.Values.ToArray());
    }

    private void ProcessCombatResults()
    {
        foreach (var matchup in currentMatchups)
        {
            int player1 = matchup.Key;
            int player2 = matchup.Value;

            if (player2 == -1) continue;

            photonView.RPC("RPC_ApplyDamage", RpcTarget.All, player2, damagePerLoss);
        }
    }

    private List<int> GetAlivePlayers()
    {
        return allPlayers.Where(kvp => kvp.Value.isAlive).Select(kvp => kvp.Key).ToList();
    }

    private void EndGame()
    {
        int winner = GetAlivePlayers().FirstOrDefault();
        photonView.RPC("RPC_AnnounceWinner", RpcTarget.All, winner);
    }

    [PunRPC]
    private void RPC_ReturnToHomeBoard()
    {
        TeleportToMyHomeBoard();
        Debug.Log("🏠 Returned to home board for prep phase");
    }

    [PunRPC]
    private void RPC_TeleportToCombat()
    {
        StartCoroutine(TeleportToCombatCoroutine());
    }

    private IEnumerator TeleportToCombatCoroutine()
    {
        int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        if (!currentMatchups.ContainsKey(myActorNumber))
        {
            Debug.LogWarning("⚠️ No matchup assigned");
            yield break;
        }

        int opponentActorNumber = currentMatchups[myActorNumber];

        if (opponentActorNumber == -1)
        {
            Debug.Log("😴 Bye round");
            yield break;
        }

        // Determine visitor/home (lower actor number visits higher)
        bool iAmVisitor = myActorNumber < opponentActorNumber;
        int homePlayerActorNumber = iAmVisitor ? opponentActorNumber : myActorNumber;

        PvPArenaManager.PlayerBoard combatBoard = arenaManager.GetPlayerBoard(homePlayerActorNumber);

        if (combatBoard == null)
        {
            Debug.LogError($"❌ Combat board not found for player {homePlayerActorNumber}!");
            yield break;
        }

        Debug.Log($"🚀 Teleporting to Player {homePlayerActorNumber}'s board (I am {(iAmVisitor ? "visitor" : "home")})");

        // Activate combat board camera
        combatBoard.ActivateCamera(true);

        // Teleport player avatar
        GameObject player = PhotonNetwork.LocalPlayer.TagObject as GameObject;
        if (player != null)
        {
            Vector3 spawnPos = combatBoard.boardRoot.position + (iAmVisitor ? new Vector3(0, 1, 5) : new Vector3(0, 1, -5));
            player.transform.position = spawnPos;
        }

        // ✅ STEP 1: Collect MY unit positions from home board
        List<UnitAI> myUnits = GameManager.Instance.GetPlayerUnits();
        List<UnitPositionData> myUnitPositions = new List<UnitPositionData>();

        foreach (UnitAI unit in myUnits)
        {
            if (unit != null && unit.currentTile != null && unit.currentState != UnitAI.UnitState.Bench)
            {
                UnitPositionData data = new UnitPositionData
                {
                    unitPrefabName = unit.gameObject.name.Replace("(Clone)", "").Trim(),
                    gridPosition = unit.currentTile.gridPosition,
                    ownerActorNumber = myActorNumber
                };
                myUnitPositions.Add(data);

                // Hide unit on home board (will respawn on combat board)
                unit.gameObject.SetActive(false);
            }
        }

        Debug.Log($"📤 Player {myActorNumber}: Sending {myUnitPositions.Count} units to combat board");

        // ✅ STEP 2: Send unit positions to Master for spawning
        // Pass: unit data, combat board owner, unit owner, and whether unit owner is visitor
        string jsonData = JsonUtility.ToJson(new UnitPositionDataList { positions = myUnitPositions });

        if (PhotonNetwork.IsMasterClient)
        {
            // Master spawns their own units directly
            StartCoroutine(SpawnUnitsOnCombatBoard(jsonData, homePlayerActorNumber, myActorNumber, iAmVisitor));
        }
        else
        {
            // Non-master sends positions to master via RPC
            photonView.RPC("RPC_ReceiveUnitPositions", RpcTarget.MasterClient, jsonData, homePlayerActorNumber, myActorNumber, iAmVisitor);
        }

        yield return null;
    }


    [PunRPC]
    private void RPC_ReceiveUnitPositions(string jsonData, int combatBoardOwner, int unitOwner, bool unitOwnerIsVisitor)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"📥 Master received units from Player {unitOwner} for combat on Board {combatBoardOwner}");
        StartCoroutine(SpawnUnitsOnCombatBoard(jsonData, combatBoardOwner, unitOwner, unitOwnerIsVisitor));
    }

    private IEnumerator SpawnUnitsOnCombatBoard(string jsonData, int combatBoardOwner, int unitOwner, bool unitOwnerIsVisitor)
    {
        UnitPositionDataList dataList = JsonUtility.FromJson<UnitPositionDataList>(jsonData);
        PvPArenaManager.PlayerBoard combatBoard = arenaManager.GetPlayerBoard(combatBoardOwner);

        if (combatBoard == null)
        {
            Debug.LogError($"❌ Combat board not found for owner {combatBoardOwner}!");
            yield break;
        }

        // ✅ Units spawn on visitor side or home side based on who owns them
        List<HexTile> targetTiles = unitOwnerIsVisitor ? combatBoard.GetEnemyTiles() : combatBoard.GetPlayerTiles();

        Debug.Log($"🎯 Spawning {dataList.positions.Count} units for Player {unitOwner} (isVisitor={unitOwnerIsVisitor}) on Board {combatBoardOwner}");

        foreach (UnitPositionData data in dataList.positions)
        {
            // ✅ Mirror grid position if unit owner is visitor
            Vector2Int spawnGridPos = data.gridPosition;

            if (unitOwnerIsVisitor)
            {
                // Mirror X-axis (flip to opposite side)
                spawnGridPos.x = 7 - data.gridPosition.x;
            }

            // Find matching tile by grid position
            HexTile targetTile = targetTiles.FirstOrDefault(t => t.gridPosition == spawnGridPos);

            if (targetTile == null)
            {
                Debug.LogWarning($"⚠️ Could not find tile at {spawnGridPos}");
                continue;
            }

            if (targetTile.occupyingUnit != null)
            {
                Debug.LogWarning($"⚠️ Tile at {spawnGridPos} already occupied");
                continue;
            }

            // Spawn unit via network (spawns on ALL clients)
            Vector3 spawnPos = targetTile.transform.position + Vector3.up * 0.72f;
            GameObject unitObj = PhotonNetwork.Instantiate(data.unitPrefabName, spawnPos, Quaternion.Euler(0, -90, 0));

            // ✅ Send RPC to ALL clients to configure the unit
            PhotonView pv = unitObj.GetComponent<PhotonView>();
            if (pv != null)
            {
                // Pass: photonViewID, unit owner, board owner, spawn grid position
                photonView.RPC("RPC_ConfigureCombatUnit", RpcTarget.AllBuffered,
                    pv.ViewID,
                    unitOwner,
                    combatBoardOwner,
                    spawnGridPos.x,
                    spawnGridPos.y);
            }

            yield return new WaitForSeconds(0.05f);
        }

        Debug.Log($"✅ Finished spawning units for Player {unitOwner}");
    }

    [PunRPC]
    private void RPC_ConfigureCombatUnit(int unitPhotonViewID, int unitOwnerActor, int boardOwnerActor, int gridX, int gridY)
    {
        // Find the spawned unit by PhotonView ID
        PhotonView unitPV = PhotonView.Find(unitPhotonViewID);
        if (unitPV == null)
        {
            Debug.LogError($"❌ Could not find unit with ViewID {unitPhotonViewID}");
            return;
        }

        UnitAI unit = unitPV.GetComponent<UnitAI>();
        if (unit == null) return;

        int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        // ✅ Get combat board
        PvPArenaManager.PlayerBoard combatBoard = arenaManager.GetPlayerBoard(boardOwnerActor);
        if (combatBoard == null)
        {
            Debug.LogError($"❌ [RPC_ConfigureCombatUnit] Combat board not found for actor {boardOwnerActor}");
            return;
        }

        // ✅ Determine from MY perspective: is this MY unit or ENEMY unit?
        bool unitBelongsToMe = (unitOwnerActor == myActorNumber);

        // ✅ Get the correct tile list from MY perspective
        List<HexTile> targetTiles = unitBelongsToMe ? combatBoard.GetPlayerTiles() : combatBoard.GetEnemyTiles();

        // ✅ Find the tile
        Vector2Int gridPos = new Vector2Int(gridX, gridY);
        HexTile targetTile = targetTiles.FirstOrDefault(t => t.gridPosition == gridPos);

        if (targetTile != null)
        {
            targetTile.TryClaim(unit);
            unit.AssignToTile(targetTile);
        }
        else
        {
            Debug.LogWarning($"⚠️ Could not find tile at ({gridX},{gridY}) from my perspective");
        }

        // ✅ Set team from MY perspective
        if (unitBelongsToMe)
        {
            unit.team = Team.Player;
            unit.teamID = 1;
            Debug.Log($"✅ MY unit {unit.name} at ({gridX},{gridY}) → Player team");
        }
        else
        {
            unit.team = Team.Enemy;
            unit.teamID = 2;
            Debug.Log($"✅ ENEMY unit {unit.name} at ({gridX},{gridY}) → Enemy team");
        }

        unit.SetState(UnitAI.UnitState.BoardIdle);
    }

    [PunRPC]
    private void RPC_StartLiveCombat()
    {
        Debug.Log("⚔️ Starting LIVE combat!");

        List<UnitAI> allUnits = FindObjectsOfType<UnitAI>().ToList();

        foreach (var unit in allUnits)
        {
            if (unit != null && unit.isAlive && unit.currentState != UnitAI.UnitState.Bench)
            {
                unit.SetState(UnitAI.UnitState.Combat);
            }
        }

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.StartCombat();
        }
    }


    [PunRPC]
    private void RPC_EndCombat()
    {
        Debug.Log("🏁 Combat ended");

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.ForceResetCombatState();
        }
    }

    [PunRPC]
    private void RPC_SyncTimer(float time)
    {
        phaseTimer = time;
    }

    [PunRPC]
    private void RPC_SyncRound(int round)
    {
        currentRound = round;
        Debug.Log($"📢 Round {round} started!");
    }

    [PunRPC]
    private void RPC_EnterPhase(int phaseInt)
    {
        currentPhase = (PvPPhase)phaseInt;
        Debug.Log($"📢 Entering {currentPhase} phase");
    }

    [PunRPC]
    private void RPC_SyncMatchups(int[] players, int[] opponents)
    {
        currentMatchups.Clear();
        for (int i = 0; i < players.Length; i++)
        {
            currentMatchups[players[i]] = opponents[i];
        }
    }

    [PunRPC]
    private void RPC_ApplyDamage(int actorNumber, int damage)
    {
        if (allPlayers.ContainsKey(actorNumber))
        {
            allPlayers[actorNumber].TakeDamage(damage);

            if (PvPPlayerListUI.Instance != null)
            {
                PvPPlayerListUI.Instance.UpdatePlayerHealth(
                    actorNumber,
                    allPlayers[actorNumber].health,
                    allPlayers[actorNumber].isAlive
                );
            }
        }
    }

    [PunRPC]
    private void RPC_AnnounceWinner(int winnerActorNumber)
    {
        string winnerName = allPlayers[winnerActorNumber].playerName;
        Debug.Log($"🏆 {winnerName} wins!");
    }

    public void SpectatePlayer(int actorNumber)
    {
        PvPArenaManager.PlayerBoard board = arenaManager.GetPlayerBoard(actorNumber);

        if (board != null)
        {
            arenaManager.ActivatePlayerCamera(actorNumber);
            Debug.Log($"👁️ Spectating Player {actorNumber}'s board");
        }
    }
}
