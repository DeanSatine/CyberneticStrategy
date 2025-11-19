using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TMP_Text[] playerNameTexts = new TMP_Text[8];
    public GameObject[] playerSlots = new GameObject[8];
    public Button readyButton;
    public Button startGameButton;
    public TMP_Text roomInfoText;

    private Dictionary<int, bool> playerReadyStatus = new Dictionary<int, bool>();
    private bool isLocalPlayerReady = false;

    private void Start()
    {
        UpdateLobbyUI();

        if (readyButton != null)
            readyButton.onClick.AddListener(ToggleReady);

        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartGame);
            startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        }
    }

    private void UpdateLobbyUI()
    {
        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (i < players.Length)
            {
                playerSlots[i].SetActive(true);
                playerNameTexts[i].text = players[i].NickName;

                bool isReady = playerReadyStatus.ContainsKey(players[i].ActorNumber) &&
                               playerReadyStatus[players[i].ActorNumber];
                playerNameTexts[i].color = isReady ? Color.green : Color.white;
            }
            else
            {
                playerSlots[i].SetActive(false);
            }
        }

        if (roomInfoText != null)
        {
            roomInfoText.text = $"Players: {players.Length}/8\nReady: {GetReadyCount()}/{players.Length}";
        }

        if (startGameButton != null && PhotonNetwork.IsMasterClient)
        {
            bool allReady = players.Length >= 2 && GetReadyCount() == players.Length;
            startGameButton.interactable = allReady;
        }
    }

    private void ToggleReady()
    {
        isLocalPlayerReady = !isLocalPlayerReady;

        photonView.RPC("RPC_SetPlayerReady", RpcTarget.AllBuffered,
                       PhotonNetwork.LocalPlayer.ActorNumber, isLocalPlayerReady);

        if (readyButton != null)
        {
            TMP_Text buttonText = readyButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
                buttonText.text = isLocalPlayerReady ? "Unready" : "Ready";
        }
    }

    [PunRPC]
    private void RPC_SetPlayerReady(int actorNumber, bool ready)
    {
        playerReadyStatus[actorNumber] = ready;
        UpdateLobbyUI();
    }

    private int GetReadyCount()
    {
        int count = 0;
        foreach (var kvp in playerReadyStatus)
        {
            if (kvp.Value) count++;
        }
        return count;
    }

    private void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        PhotonNetwork.LoadLevel("Game");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"👋 {newPlayer.NickName} joined the lobby!");
        UpdateLobbyUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"👋 {otherPlayer.NickName} left the lobby");
        if (playerReadyStatus.ContainsKey(otherPlayer.ActorNumber))
            playerReadyStatus.Remove(otherPlayer.ActorNumber);
        UpdateLobbyUI();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (startGameButton != null)
            startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }
}
