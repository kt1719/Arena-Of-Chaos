using System;
using System.Collections.Generic;
using Fusion;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    // ===== Static Reference =====
    public static PlayerManager Instance;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        if (Instance == this) Instance = null;
    }

    public void EnablePlayer(PlayerRef playerRef) {
        if (!HasStateAuthority) return;
        PlayerController playerController = NetworkManager.Instance.GetNetworkObjectFromPlayerRef(playerRef).GetComponent<PlayerController>();
        playerController.ChangePlayerEnable(true);
    }

    public void DisablePlayer(PlayerRef playerRef) {
        if (!HasStateAuthority) return;
        PlayerController playerController = NetworkManager.Instance.GetNetworkObjectFromPlayerRef(playerRef).GetComponent<PlayerController>();
        playerController.ChangePlayerEnable(false);
    }
}
