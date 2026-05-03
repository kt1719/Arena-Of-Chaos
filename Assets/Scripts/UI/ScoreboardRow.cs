using TMPro;
using UnityEngine;

/// <summary>
/// Represents a single row in the scoreboard UI.
/// Displays player name, player kills, slime kills, and deaths.
/// </summary>
public class ScoreboardRow : MonoBehaviour
{
    // ===== Serialized Fields =====
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI playerKillsText;
    [SerializeField] private TextMeshProUGUI slimeKillsText;
    [SerializeField] private TextMeshProUGUI deathsText;

    public void SetData(string playerName, int playerKills, int slimeKills, int deaths)
    {
        if (playerNameText != null) playerNameText.text = playerName;
        if (playerKillsText != null) playerKillsText.text = playerKills.ToString();
        if (slimeKillsText != null) slimeKillsText.text = slimeKills.ToString();
        if (deathsText != null) deathsText.text = deaths.ToString();
    }
}
