using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // ===== Static Reference =====
    public static NetworkManager Instance { get; private set; }

    // ===== Events =====
    public Action<NetworkObject> OnPlayerJoinedNetworkManager;

    // ===== Serialized Fields =====
    [SerializeField] private NetworkPrefabRef _playerPrefab;

    // ===== Public Properties =====
    public NetworkObject LocalPlayerObject => _localPlayerController != null ? _localPlayerController.Object : null;
    public Dictionary<PlayerRef, NetworkObject> SpawnedCharacters { get; private set; } = new Dictionary<PlayerRef, NetworkObject>();

    public NetworkObject GetNetworkObjectFromPlayerRef(PlayerRef playerRef) {
        return SpawnedCharacters[playerRef];
    } 

    // ===== Private Fields =====
    private PlayerController _localPlayerController;
    private NetworkRunner _runner;
    private bool _startedGame = false;

    // ===== Unity Methods =====

    private void Awake() {
        Instance = this;
    }

    private void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    public void StartGameHost() {
        StartGameAsync(GameMode.Host);
    }

    public void StartGameClient() {
        StartGameAsync(GameMode.Client);
    }

    async void StartGameAsync(GameMode mode)
    {
        if (_startedGame) return;

        _startedGame = true;
        
        // Create the Fusion runner and let it know that we will be providing user input
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var runnerSimulatePhysics2D = gameObject.AddComponent<RunnerSimulatePhysics2D>();
        runnerSimulatePhysics2D.ClientPhysicsSimulation = ClientPhysicsSimulation.SimulateAlways;

        // Create the NetworkSceneInfo from the current scene
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid) {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        // Start or join (depends on gamemode) a session with a specific name
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void RegisterLocalPlayer(PlayerController playerController) {
        _localPlayerController = playerController;
        CameraFollow.Instance?.SetTarget(playerController.transform);
    }

    public void UnregisterLocalPlayer() {
        _localPlayerController = null;
        CameraFollow.Instance?.ClearTarget();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, spawnRotation, player);
            SpawnedCharacters.Add(player, networkPlayerObject);
            runner.SetPlayerObject(player, networkPlayerObject);
            
            OnPlayerJoinedNetworkManager?.Invoke(networkPlayerObject);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {
        if (SpawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            SpawnedCharacters.Remove(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) {
        // If the local player is not set, return
        if (_localPlayerController == null) {
            return;
        }
        PlayerControllerLocalInputData localInputData = _localPlayerController.ConsumeInput();

        NetworkInputData data = new NetworkInputData();

        data.movementDirection = localInputData.movementDirection;
        data.weaponAimDirection = localInputData.weaponAimDirection;
        data.buttons.Set(NetworkInputData.DASH, localInputData.dashPressed);
        data.buttons.Set(NetworkInputData.ATTACK, localInputData.attackPressed);

        input.Set(data);
    }

    // Callbacks
    // public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    // public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    // public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){ }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){ }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data){ }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress){ }

}
