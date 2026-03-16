using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // ===== Static Reference =====
    public static NetworkManager Instance { get; private set; }
    
    // ===== Serialized Fields =====
    [SerializeField] private NetworkPrefabRef _playerPrefab;
    [SerializeField] private Button _startGameHostButton;
    [SerializeField] private Button _startGameClientButton;

    // ===== Private Fields =====
    private PlayerController _localPlayerController;
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    private NetworkRunner _runner;

    // ===== Unity Methods =====

    private void Awake() {
        Instance = this;
    }
    private void Start() {
        _startGameHostButton.onClick.AddListener(StartGameHost);
        _startGameClientButton.onClick.AddListener(StartGameClient);
    }
    public void StartGameHost() {
        StartGameAsync(GameMode.Host);
    }

    public void StartGameClient() {
        StartGameAsync(GameMode.Client);
    }

    async void StartGameAsync(GameMode mode)
    {
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
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            // Create a unique position for the player
            Vector3 spawnPosition = new Vector3((player.RawEncoded % runner.Config.Simulation.PlayerCount) * 3, 1, 0);
            NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
            // Keep track of the player avatars for easy access
            _spawnedCharacters.Add(player, networkPlayerObject);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) {
        // If the local player is not set, return
        if (_localPlayerController == null) {
            return;
        }

        NetworkInputData data = new NetworkInputData();
        data.movementDirection = _localPlayerController.LocalInputData.movementDirection;
        data.buttons.Set(NetworkInputData.DASH_BUTTON, _localPlayerController.LocalInputData.isDashing);

        input.Set(data);

        // Now it's safe to clear the dash flag
        _localPlayerController.ConsumeInput();
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
