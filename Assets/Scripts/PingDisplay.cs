using Fusion;
using UnityEngine;

public class PingDisplay : MonoBehaviour
{
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

    [Header("Update Settings")]
    [SerializeField] private float _updateInterval = 0.1f; // seconds (100 ms)

    private GUIStyle _style;
    private NetworkRunner _runner;

    private float _nextUpdateTime;
    private int _cachedPing;

    private void OnGUI()
    {
        if (_runner == null || !_runner.IsRunning)
        {
            _runner = FindRunner();
            if (_runner == null) return;
        }

        // Update ping only every X seconds
        if (Time.unscaledTime >= _nextUpdateTime)
        {
            _cachedPing = Mathf.RoundToInt(
                (float)_runner.GetPlayerRtt(_runner.LocalPlayer) * 1000f
            );

            _nextUpdateTime = Time.unscaledTime + _updateInterval;
        }

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

        _style.normal.textColor =
            _cachedPing <= _goodThresholdMs ? _goodColor :
            _cachedPing <= _okThresholdMs ? _okColor :
            _badColor;

        var rect = new Rect(
            Screen.width - scaledBoxWidth - scaledPadding,
            scaledPadding,
            scaledBoxWidth,
            scaledBoxHeight);

        GUI.Label(rect, $"Ping: {_cachedPing} ms", _style);
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