using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TMP_InputField playerNameInput;
    public Button findMatchButton;
    public TMP_Text statusText;

    [Header("Settings")]
    public string gameVersion = "1.0";
    public byte maxPlayersPerRoom = 8;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        // Override region settings
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "";
        PhotonNetwork.PhotonServerSettings.AppSettings.Server = "";

        if (playerNameInput != null)
        {
            playerNameInput.text = "Player" + Random.Range(1000, 9999);
        }

        if (findMatchButton != null)
        {
            findMatchButton.onClick.AddListener(OnFindMatchClicked);
        }

        UpdateStatus("Ready to connect");
    }


    private void OnFindMatchClicked()
    {
        if (playerNameInput != null)
        {
            PhotonNetwork.NickName = playerNameInput.text;
        }

        if (!PhotonNetwork.IsConnected)
        {
            UpdateStatus("Connecting to Photon...");
            PhotonNetwork.GameVersion = gameVersion;

            // Clear any region lock
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "";

            PhotonNetwork.ConnectUsingSettings();

            if (findMatchButton != null)
                findMatchButton.interactable = false;
        }
    }


    public override void OnConnectedToMaster()
    {
        Debug.Log("✅ Connected to Photon Master Server!");
        UpdateStatus("Finding match...");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log($"🏗️ No rooms available, creating new room... (Code: {returnCode}, Message: {message})");
        UpdateStatus("Creating room...");

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(null, roomOptions);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"✅ Room created successfully! Room name: {PhotonNetwork.CurrentRoom.Name}");
        UpdateStatus("Room created, joining...");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"❌ Failed to create room! Code: {returnCode}, Message: {message}");
        UpdateStatus($"Failed to create room: {message}");

        if (findMatchButton != null)
            findMatchButton.interactable = true;
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"🎮 Joined room: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"🎮 Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}");
        Debug.Log($"🎮 AutomaticallySyncScene: {PhotonNetwork.AutomaticallySyncScene}");
        Debug.Log($"🎮 IsMasterClient: {PhotonNetwork.IsMasterClient}");

        UpdateStatus("Joined room! Loading lobby...");

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("📡 Master client loading Lobby scene...");
            PhotonNetwork.LoadLevel("Lobby");
        }
        else
        {
            Debug.Log("📡 Waiting for master client to load scene...");
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"❌ Failed to join room! Code: {returnCode}, Message: {message}");
        UpdateStatus($"Failed to join room: {message}");

        if (findMatchButton != null)
            findMatchButton.interactable = true;
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"❌ Disconnected: {cause}");
        UpdateStatus($"Disconnected: {cause}");

        if (findMatchButton != null)
            findMatchButton.interactable = true;
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"📡 {message}");
    }
}
