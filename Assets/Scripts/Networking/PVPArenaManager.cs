using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PvPArenaManager : MonoBehaviour
{
    public static PvPArenaManager Instance;

    [System.Serializable]
    public class PlayerBoard
    {
        public int ownerActorNumber;
        public Transform boardRoot;
        public Camera boardCamera;

        private List<HexTile> cachedPlayerTiles;
        private List<HexTile> cachedEnemyTiles;
        private List<HexTile> cachedBenchTiles;

        public List<HexTile> GetPlayerTiles()
        {
            if (cachedPlayerTiles == null || cachedPlayerTiles.Count == 0)
            {
                cachedPlayerTiles = boardRoot.GetComponentsInChildren<HexTile>()
                    .Where(t => t.owner == TileOwner.Player && t.tileType == TileType.Board)
                    .ToList();
            }
            return cachedPlayerTiles;
        }

        public List<HexTile> GetEnemyTiles()
        {
            if (cachedEnemyTiles == null || cachedEnemyTiles.Count == 0)
            {
                cachedEnemyTiles = boardRoot.GetComponentsInChildren<HexTile>()
                    .Where(t => t.owner == TileOwner.Enemy && t.tileType == TileType.Board)
                    .ToList();
            }
            return cachedEnemyTiles;
        }

        public List<HexTile> GetBenchTiles()
        {
            if (cachedBenchTiles == null || cachedBenchTiles.Count == 0)
            {
                HexTile[] allTiles = boardRoot.GetComponentsInChildren<HexTile>();
                Debug.Log($"🔍 Board {boardRoot.name}: Found {allTiles.Length} total HexTiles");

                cachedBenchTiles = allTiles
                    .Where(t => t.tileType == TileType.Bench)
                    .ToList();

                Debug.Log($"🪑 Board {boardRoot.name}: Found {cachedBenchTiles.Count} bench tiles");

                if (cachedBenchTiles.Count == 0)
                {
                    Debug.LogWarning($"⚠️ No bench tiles found on {boardRoot.name}! Make sure CharacterTile objects have tileType = Bench");
                }
            }
            return cachedBenchTiles;
        }


        public void ActivateCamera(bool active)
        {
            if (boardCamera != null)
            {
                boardCamera.enabled = active;
            }
        }
    }

    [Header("Player Boards")]
    public List<PlayerBoard> playerBoards = new List<PlayerBoard>();

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
        foreach (var board in playerBoards)
        {
            board.ActivateCamera(false);
        }
    }

    public void AssignBoardsToPlayers(List<int> playerActorNumbers)
    {
        for (int i = 0; i < playerActorNumbers.Count && i < playerBoards.Count; i++)
        {
            playerBoards[i].ownerActorNumber = playerActorNumbers[i];
            Debug.Log($"🏠 Board {i} assigned to Player {playerActorNumbers[i]}");
        }
    }

    public PlayerBoard GetPlayerBoard(int actorNumber)
    {
        return playerBoards.FirstOrDefault(b => b.ownerActorNumber == actorNumber);
    }

    public PlayerBoard GetBoardByIndex(int index)
    {
        if (index >= 0 && index < playerBoards.Count)
            return playerBoards[index];
        return null;
    }

    public void ActivatePlayerCamera(int actorNumber)
    {
        foreach (var board in playerBoards)
        {
            board.ActivateCamera(board.ownerActorNumber == actorNumber);
        }
    }
}
