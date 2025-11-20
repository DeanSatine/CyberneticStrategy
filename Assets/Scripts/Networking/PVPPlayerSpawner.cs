using UnityEngine;
using Photon.Pun;

public class PvPPlayerSpawner : MonoBehaviour
{
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    private void Start()
    {
        if (PhotonNetwork.IsConnected && playerPrefab != null)
        {
            Vector3 spawnPos = GetSpawnPosition();
            PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity);
            Debug.Log($"✅ Spawned player avatar at {spawnPos}");
        }
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int index = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
            return spawnPoints[index].position;
        }

        return new Vector3(0, 1, -5);
    }
}
