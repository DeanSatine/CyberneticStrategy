using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PhotonConnectionManager : MonoBehaviourPunCallbacks
{
    public static PhotonConnectionManager Instance;

    [Header("Connection Settings")]
    public string gameVersion = "1.0";
    public byte maxPlayersPerRoom = 8;

    [Header("Player Settings")]
    public string playerName = "Player";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void ConnectToPhoton()
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("✅ Already connected to Photon!");
            return;
        }

        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.NickName = playerName;

        Debug.Log("🌐 Connecting to Photon...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("✅ Connected to Photon Master Server!");
        Debug.Log($"👤 Player Name: {PhotonNetwork.NickName}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"❌ Disconnected from Photon: {cause}");
    }

    public void FindMatch()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("⚠️ Not connected to Photon!");
            ConnectToPhoton();
            return;
        }

        Debug.Log("🔍 Looking for match...");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("🏗️ No rooms available, creating new room...");

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(null, roomOptions);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"🎮 Joined room! Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{maxPlayersPerRoom}");

        SceneManager.LoadScene("Lobby");
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("✅ Room created successfully!");
    }
}
