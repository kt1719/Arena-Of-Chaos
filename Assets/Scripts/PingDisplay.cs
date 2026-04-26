using Fusion;
using UnityEngine;

/// <summary>
/// Displays the local client's RTT (round-trip time) to the server in the
/// top-right corner of the screen. Scales against a 1920x1080 reference
/// resolution so it looks consistent on any display. Drop on any GameObject.
/// </summary>
public class PingDisplay : MonoBehaviour
{
    // ── Reference resolution ──
    private const float REF_WIDTH = 1920f;
    private const float REF_HEIGHT = 1080f;

    [Header("Style (in reference-resolution pixels)")]
    [SerializeField] private int _fontSize = 28;
    [SerializeField] private int _padding = 20;
    [SerializeField] private int _boxWidth = 260;

    [Header("Colour Thresholds")]
    [SerializeField] private Color _goodColor = Color.green;
    [SerializeField] private Color _okColor = Color.yellow;
    [SerializeField] private Color _badColor = Color.red;
    [SerializeField] private int _goodThresholdMs = 60;
    [SerializeField] private int _okThresholdMs = 120;

    private GUIStyle _style;
    private NetworkRunner _runner;

    private void OnGUI()
    {
        if (_runner == null || !_runner.IsRunning)
        {
            _runner = FindRunner();
            if (_runner == null) return;
        }

        // Use the smaller of the two scales so text never overflows on
        // ultrawide or extra-tall displays.
        float scale = Mathf.Min(
            Screen.width / REF_WIDTH,
            Screen.height / REF_HEIGHT);

        int scaledFontSize = Mathf.RoundToInt(_fontSize * scale);
        int scaledPadding = Mathf.RoundToInt(_padding * scale);
        int scaledBoxWidth = Mathf.RoundToInt(_boxWidth * scale);
        int scaledBoxHeight = scaledFontSize + Mathf.RoundToInt(8f * scale);

        if (_style == null || _style.fontSize != scaledFontSize)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = scaledFontSize,
                alignment = TextAnchor.UpperRight,
                fontStyle = FontStyle.Bold,
            };
        }

        int pingMs = Mathf.RoundToInt((float)_runner.GetPlayerRtt(_runner.LocalPlayer) * 1000f);

        _style.normal.textColor =
            pingMs <= _goodThresholdMs ? _goodColor :
            pingMs <= _okThresholdMs ? _okColor :
            _badColor;

        var rect = new Rect(
            Screen.width - scaledBoxWidth - scaledPadding,
            scaledPadding,
            scaledBoxWidth,
            scaledBoxHeight);

        GUI.Label(rect, $"Ping: {pingMs} ms", _style);
    }

    private static NetworkRunner FindRunner()
    {
        var runners = FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None);
        for (int i = 0; i < runners.Length; i++)
        {
            if (runners[i].IsRunning) return runners[i];
        }
        return null;
    }
}