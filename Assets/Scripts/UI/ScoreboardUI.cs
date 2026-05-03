using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Manages the Tab-held scoreboard overlay. Shows player scores during active rounds.
/// Reads networked score data from ScoreManager and displays rows for all active players.
/// Uses Runner.ActivePlayers + Runner.GetPlayerObject (Fusion-native, works on all clients).
/// Listens to GameInput events for Tab press/release (same pattern as PlayerController).
/// </summary>
public class ScoreboardUI : MonoBehaviour
{
    // ===== Serialized Fields =====
    [Header("Scoreboard Panel")]
    [SerializeField] private GameObject scoreboardPanel;

    [Header("Row Template")]
    [SerializeField] private Transform rowContainer;
    [SerializeField] private GameObject rowPrefab;

    // ===== Private Fields =====
    private readonly List<ScoreboardRow> _rows = new();
    private bool _scoreboardHeld;

    private void Start()
    {
        GameInput.Instance.OnScoreboardPressed += OnScoreboardPressed;
        GameInput.Instance.OnScoreboardReleased += OnScoreboardReleased;

        scoreboardPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnScoreboardPressed -= OnScoreboardPressed;
            GameInput.Instance.OnScoreboardReleased -= OnScoreboardReleased;
        }
    }

    private void OnScoreboardPressed()
    {
        _scoreboardHeld = true;
    }

    private void OnScoreboardReleased()
    {
        _scoreboardHeld = false;
    }

    private void Update()
    {
        bool shouldShow = _scoreboardHeld && IsRoundActive();

        if (shouldShow != scoreboardPanel.activeSelf)
            scoreboardPanel.SetActive(shouldShow);

        if (shouldShow)
            RefreshScoreboard();
    }

    // ===== Helpers =====

    private bool IsRoundActive()
    {
        return GameManager.Instance != null
            && GameManager.Instance.Object != null
            && GameManager.Instance.Object.IsValid
            && GameManager.Instance.CurrentRound > 0
            && !GameManager.Instance.IsGameOver;
    }

    private NetworkRunner GetRunner()
    {
        return GameManager.Instance != null && GameManager.Instance.Runner != null
            ? GameManager.Instance.Runner
            : null;
    }

    // ===== Scoreboard Refresh =====

    private void RefreshScoreboard()
    {
        if (ScoreManager.Instance == null) return;

        NetworkRunner runner = GetRunner();
        if (runner == null) return;

        // Collect active players into a temp list to get count
        var activePlayers = new List<PlayerRef>();
        foreach (PlayerRef playerRef in runner.ActivePlayers)
        {
            activePlayers.Add(playerRef);
        }

        if (activePlayers.Count == 0) return;

        EnsureRowCount(activePlayers.Count);

        for (int i = 0; i < activePlayers.Count; i++)
        {
            PlayerRef playerRef = activePlayers[i];

            ScoreboardRow row = _rows[i];
            row.gameObject.SetActive(true);

            string playerName = $"Player {playerRef.PlayerId}";
            int playerKills = ScoreManager.Instance.GetPlayerKills(playerRef);
            int slimeKills = ScoreManager.Instance.GetSlimeKills(playerRef);
            int deaths = ScoreManager.Instance.GetDeaths(playerRef);

            row.SetData(playerName, playerKills, slimeKills, deaths);
        }

        // Hide unused rows
        for (int i = activePlayers.Count; i < _rows.Count; i++)
        {
            _rows[i].gameObject.SetActive(false);
        }
    }

    private void EnsureRowCount(int needed)
    {
        while (_rows.Count < needed)
        {
            GameObject rowObj = Instantiate(rowPrefab, rowContainer);
            ScoreboardRow row = rowObj.GetComponent<ScoreboardRow>();

            if (row == null)
                row = rowObj.AddComponent<ScoreboardRow>();

            _rows.Add(row);
        }
    }
}
