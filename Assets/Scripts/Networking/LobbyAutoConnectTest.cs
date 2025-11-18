using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class LobbyAutoConnectTest : MonoBehaviourPunCallbacks
{
    [Header("Test Settings")]
    public string testPlayerName = "TestPlayer";

    private void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("✅ Already connected to Photon!");
            return;
        }

        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.NickName = testPlayerName + Random.Range(1000, 9999);
        PhotonNetwork.GameVersion = "1.0";

        Debug.Log($"🌐 Connecting to Photon as {PhotonNetwork.NickName}...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("✅ Connected to Photon Master Server!");
        Debug.Log("🔍 Attempting to join or create room...");

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 8,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.JoinOrCreateRoom("TestRoom", roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"🎮 Joined room! Players: {PhotonNetwork.CurrentRoom.PlayerCount}/8");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"❌ Disconnected: {cause}");
    }
}
