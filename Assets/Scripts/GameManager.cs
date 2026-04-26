using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Networked] private TickTimer RoundTimer { get; set; }

    [SerializeField] private List<Transform> defaultPlayerSpawnPoints = new();
    public List<Transform> DefaultPlayerSpawnPoints => defaultPlayerSpawnPoints;

    private const float _defaultRoundTime = 120f;
    private Dictionary<PlayerRef, int> _playerSpawnIdxMapping = new();

    private void Awake()
    {
        Instance = this;
        if (defaultPlayerSpawnPoints == null)
            Debug.LogWarning("[GameManager] defaultPlayerSpawnPoints is not assigned.");
    }

    private void Start() {
        NetworkManager.Instance.OnPlayerJoinedNetworkManager += SpawnPlayer;
    }

    private void SpawnPlayer(NetworkObject player)
    {
        int[] candidates = { 0, 1, 2, 3 };
        var usedIndices = new HashSet<int>(_playerSpawnIdxMapping.Values);
        var allowed = candidates.Where(i => !usedIndices.Contains(i)).ToArray();
        int spawnPointIdx = allowed[UnityEngine.Random.Range(0, allowed.Length)];

        Vector3 randomSpawnPosition = DefaultPlayerSpawnPoints[spawnPointIdx].position;
        player.transform.position = randomSpawnPosition;

        _playerSpawnIdxMapping[player.InputAuthority] = spawnPointIdx;
    }

    private void StartRound() {
        if (!HasStateAuthority) return;
        
        ResetTimer();
        ResetPlayers();
    }

    private void ResetTimer() {
        RoundTimer = TickTimer.CreateFromSeconds(Runner, _defaultRoundTime);
    }

    private void ResetPlayers() {
        _playerSpawnIdxMapping = new(); // Release previous mapping
        foreach ((PlayerRef playerRef, NetworkObject networkObject) in NetworkManager.Instance.SpawnedCharacters) {
            SpawnPlayer(networkObject);
        }
    }
}
