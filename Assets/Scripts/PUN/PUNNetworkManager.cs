using System.Collections;
using Photon.Pun;
using UnityEngine;

public class PUNNetworkManager : MonoBehaviourPunCallbacks
{
    public static PUNNetworkManager Instance;

    private void Start()
    {
        PhotonNetwork.SendRate = 30;
        PhotonNetwork.SerializationRate = 30;
        
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        StartCoroutine(JoinRoom());
    }

    private IEnumerator JoinRoom()
    {
        yield return new WaitUntil(() => PhotonNetwork.IsConnectedAndReady);

        PhotonNetwork.JoinRandomOrCreateRoom(); // For testing...
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Connected to room");
    }
}
