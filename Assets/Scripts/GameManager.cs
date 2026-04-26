using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    // ===== Static Reference =====
    public static GameManager Instance { get; private set; }
    
    // ===== Networked Variables =====
    [Networked] private TickTimer RoundTimer { get; set; }
    [Networked] private bool RoundStarted { get; set; }

    // ===== Serialized Fields =====
    [SerializeField] private float defaultRoundTime = 120f;
    [SerializeField] private List<Transform> defaultPlayerSpawnPoints = new();
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform menuCamera;

    // ===== Public Properties =====
    public List<Transform> DefaultPlayerSpawnPoints => defaultPlayerSpawnPoints;
    public Camera CurrentActiveCamera => GetActiveCamera();
    public float RoundDuration => defaultRoundTime;
    public float? RoundTimeRemaining => (Object != null && Object.IsValid) ? RoundTimer.RemainingTime(Runner) : null;

    // ===== Private Fields =====
    private Dictionary<PlayerRef, int> _playerSpawnIdxMapping = new();
    private ChangeDetector _changeDetector;

    private void Awake()
    {
        Instance = this;
        if (defaultPlayerSpawnPoints == null)
            Debug.LogWarning("[GameManager] defaultPlayerSpawnPoints is not assigned.");
    }

    public override void Spawned() {
        // NetworkManager.Instance.OnPlayerJoinedNetworkManager += SpawnPlayer;
        NetworkManager.Instance.OnPlayerJoinedNetworkManager += AddToLobby;
        UpdateActiveCamera();
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void Render() {
        if (_changeDetector == null) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(RoundStarted)) {
                UpdateActiveCamera();
            }
        }
    }

    private Camera GetActiveCamera() {
        return (RoundStarted ? playerCamera : menuCamera).GetComponent<Camera>();
    }

    private void UpdateActiveCamera() {
        if (RoundStarted) {
            playerCamera.gameObject.SetActive(true);
            menuCamera.gameObject.SetActive(false);
        }
        else {
            playerCamera.gameObject.SetActive(false);
            menuCamera.gameObject.SetActive(true);
        }
    }

    private void AddToLobby(NetworkObject player) {
        Debug.Log($"{player.InputAuthority.PlayerId}: Added to Lobby!");
    }

    public void StartRound() {
        if (!HasStateAuthority) return;
        
        RoundStarted = true;
        UpdateActiveCamera();
        ResetTimer();
        ResetPlayers();
        SpawnPlayers();
    }

    private void ResetTimer() {
        RoundTimer = TickTimer.CreateFromSeconds(Runner, defaultRoundTime);
    }

    private void ResetPlayers() {
        _playerSpawnIdxMapping = new(); // Release previous mapping
    }

    private void SpawnPlayers() {
        foreach ((PlayerRef playerRef, NetworkObject networkObject) in NetworkManager.Instance.SpawnedCharacters) {
            SpawnPlayer(networkObject);
            ActivatePlayer(playerRef);
        }
    }

    private void SpawnPlayer(NetworkObject player)
    {
        // Move Player to spawn location
        int[] candidates = { 0, 1, 2, 3 };
        var usedIndices = new HashSet<int>(_playerSpawnIdxMapping.Values);
        var allowed = candidates.Where(i => !usedIndices.Contains(i)).ToArray();
        int spawnPointIdx = allowed[UnityEngine.Random.Range(0, allowed.Length)];

        Vector3 randomSpawnPosition = DefaultPlayerSpawnPoints[spawnPointIdx].position;
        player.transform.position = randomSpawnPosition;

        _playerSpawnIdxMapping.Add(player.InputAuthority, spawnPointIdx);
    }
    
    private void ActivatePlayer(PlayerRef player) {
        PlayerManager.Instance.EnablePlayer(player);
    }
}