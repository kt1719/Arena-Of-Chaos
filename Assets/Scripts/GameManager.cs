using System;
using Photon.Pun;
using UnityEngine;

public class GameManager : MonoBehaviourPunCallbacks
{
    public event Action<GameObject> OnPlayerSpawned;

    [SerializeField] private GameObject playerPrefab;

    public override void OnJoinedRoom()
    {
        GameObject player = PhotonNetwork.Instantiate(playerPrefab.name, Vector3.zero, Quaternion.identity);

        OnPlayerSpawned?.Invoke(player);
    }
}
