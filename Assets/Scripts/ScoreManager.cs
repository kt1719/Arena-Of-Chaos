using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Networked singleton that tracks per-player scores (player kills, slime kills, deaths).
/// Scores accumulate across rounds and are host-authoritative.
/// </summary>
public class ScoreManager : NetworkBehaviour
{
    // ===== Static Reference =====
    public static ScoreManager Instance { get; private set; }

    // ===== Constants =====
    private const int MAX_PLAYERS = 4;

    // ===== Networked Score Arrays =====
    // Using parallel arrays indexed by player slot (0–3).
    // PlayerRef.PlayerId maps to the slot index.
    [Networked, Capacity(4)] private NetworkArray<int> PlayerKills => default;
    [Networked, Capacity(4)] private NetworkArray<int> SlimeKills => default;
    [Networked, Capacity(4)] private NetworkArray<int> Deaths => default;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ===== Score Modification (Host Only) =====

    /// <summary>
    /// Awards player kill points to the killer. Called from PlayerCombat.Die().
    /// </summary>
    public void AddPlayerKill(PlayerRef killer)
    {
        if (!HasStateAuthority) return;
        if (killer.IsNone) return;

        int slot = GetSlot(killer);
        if (slot < 0 || slot >= MAX_PLAYERS) return;

        PlayerKills.Set(slot, PlayerKills[slot] + 1);
    }

    /// <summary>
    /// Awards slime kill points to the killer. Called from SlimeCombat.Die().
    /// </summary>
    public void AddSlimeKill(PlayerRef killer)
    {
        if (!HasStateAuthority) return;
        if (killer.IsNone) return;

        int slot = GetSlot(killer);
        if (slot < 0 || slot >= MAX_PLAYERS) return;

        SlimeKills.Set(slot, SlimeKills[slot] + 1);
    }

    /// <summary>
    /// Increments the death counter for the victim. Called from PlayerCombat.Die().
    /// </summary>
    public void AddDeath(PlayerRef victim)
    {
        if (!HasStateAuthority) return;
        if (victim.IsNone) return;

        int slot = GetSlot(victim);
        if (slot < 0 || slot >= MAX_PLAYERS) return;

        Deaths.Set(slot, Deaths[slot] + 1);
    }

    // ===== Score Reading (All Clients) =====

    public int GetPlayerKills(PlayerRef player)
    {
        int slot = GetSlot(player);
        return (slot >= 0 && slot < MAX_PLAYERS) ? PlayerKills[slot] : 0;
    }

    public int GetSlimeKills(PlayerRef player)
    {
        int slot = GetSlot(player);
        return (slot >= 0 && slot < MAX_PLAYERS) ? SlimeKills[slot] : 0;
    }

    public int GetDeaths(PlayerRef player)
    {
        int slot = GetSlot(player);
        return (slot >= 0 && slot < MAX_PLAYERS) ? Deaths[slot] : 0;
    }

    // ===== Helpers =====

    /// <summary>
    /// Maps a PlayerRef to a score array slot.
    /// Fusion Host Mode assigns PlayerId starting from 1 for the host, 2+ for clients.
    /// We subtract 1 to get a 0-based index.
    /// </summary>
    private int GetSlot(PlayerRef player)
    {
        return player.PlayerId - 1;
    }
}
