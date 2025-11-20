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

    [Header("Arena System")]
    public PvPArenaManager arenaManager;

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

        bool iAmVisitor = myActorNumber < opponentActorNumber;
        int homeBoardOwner = iAmVisitor ? opponentActorNumber : myActorNumber;

        PvPArenaManager.PlayerBoard combatBoard = arenaManager.GetPlayerBoard(homeBoardOwner);

        if (combatBoard == null)
        {
            Debug.LogError($"❌ Combat board not found for player {homeBoardOwner}!");
            yield break;
        }

        Debug.Log($"🚀 Teleporting to Player {homeBoardOwner}'s board (I am {(iAmVisitor ? "visitor" : "home")})");

        combatBoard.ActivateCamera(true);

        // ✅ Get MY networked player
        GameObject player = PhotonNetwork.LocalPlayer.TagObject as GameObject;
        if (player != null)
        {
            Vector3 spawnPos = combatBoard.boardRoot.position + (iAmVisitor ? new Vector3(0, 1, 5) : new Vector3(0, 1, -5));
            player.transform.position = spawnPos;
        }

        List<UnitAI> myUnits = GameManager.Instance.GetPlayerUnits();
        List<HexTile> targetTiles = iAmVisitor ? combatBoard.GetEnemyTiles() : combatBoard.GetPlayerTiles();

        int unitIndex = 0;
        foreach (UnitAI unit in myUnits)
        {
            if (unit != null && unit.currentState != UnitAI.UnitState.Bench && unitIndex < targetTiles.Count)
            {
                HexTile targetTile = targetTiles[unitIndex];
                unit.transform.position = targetTile.transform.position + Vector3.up * 0.72f;
                targetTile.TryClaim(unit);
                unit.AssignToTile(targetTile);

                unit.team = iAmVisitor ? Team.Enemy : Team.Player;
                unit.teamID = iAmVisitor ? 2 : 1;

                unitIndex++;
            }
        }

        yield return null;
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
