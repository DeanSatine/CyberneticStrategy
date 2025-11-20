using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PvPPlayerListUI : MonoBehaviour
{
    public static PvPPlayerListUI Instance;

    [Header("UI References")]
    public GameObject playerListPanel;
    public Transform playerSlotContainer;
    public GameObject playerSlotPrefab;

    [Header("Round Info")]
    public TMP_Text roundText;
    public TMP_Text timerText;
    public TMP_Text phaseText;

    private Dictionary<int, PlayerSlotUI> playerSlots = new Dictionary<int, PlayerSlotUI>();

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
        if (playerListPanel != null)
            playerListPanel.SetActive(true);
    }

    public void InitializePlayers(Dictionary<int, PvPPlayerState> allPlayers)
    {
        ClearPlayerSlots();

        foreach (var kvp in allPlayers)
        {
            CreatePlayerSlot(kvp.Value);
        }
    }

    private void CreatePlayerSlot(PvPPlayerState playerState)
    {
        if (playerSlotPrefab == null || playerSlotContainer == null)
        {
            Debug.LogError("❌ PlayerSlotPrefab or Container is null!");
            return;
        }

        GameObject slotObj = Instantiate(playerSlotPrefab, playerSlotContainer);
        PlayerSlotUI slotUI = slotObj.GetComponent<PlayerSlotUI>();

        if (slotUI != null)
        {
            slotUI.Initialize(playerState);
            playerSlots[playerState.actorNumber] = slotUI;

            Button button = slotObj.GetComponent<Button>();
            if (button != null)
            {
                int actorNumber = playerState.actorNumber;
                button.onClick.AddListener(() => OnPlayerSlotClicked(actorNumber));
            }
        }
    }

    public void UpdatePlayerHealth(int actorNumber, int health, bool isAlive)
    {
        if (playerSlots.ContainsKey(actorNumber))
        {
            playerSlots[actorNumber].UpdateHealth(health, isAlive);
        }
    }

    public void UpdateRoundInfo(int round, float timer, PvPGameManager.PvPPhase phase)
    {
        if (roundText != null)
            roundText.text = $"Round {round}";

        if (timerText != null)
            timerText.text = $"{Mathf.CeilToInt(timer)}s";

        if (phaseText != null)
        {
            string phaseString = phase switch
            {
                PvPGameManager.PvPPhase.Waiting => "Waiting...",
                PvPGameManager.PvPPhase.Prep => "⚙️ Preparation",
                PvPGameManager.PvPPhase.Combat => "⚔️ Combat",
                PvPGameManager.PvPPhase.Results => "📊 Results",
                _ => "Unknown"
            };
            phaseText.text = phaseString;
        }
    }

    private void OnPlayerSlotClicked(int actorNumber)
    {
        if (PvPGameManager.Instance != null)
        {
            PvPGameManager.Instance.SpectatePlayer(actorNumber);
            Debug.Log($"👁️ Spectating Player {actorNumber}");
        }
    }

    private void ClearPlayerSlots()
    {
        foreach (var slot in playerSlots.Values)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        playerSlots.Clear();
    }

    private void Update()
    {
        if (PvPGameManager.Instance != null)
        {
            UpdateRoundInfo(
                PvPGameManager.Instance.currentRound,
                PvPGameManager.Instance.phaseTimer,
                PvPGameManager.Instance.currentPhase
            );
        }
    }
}
