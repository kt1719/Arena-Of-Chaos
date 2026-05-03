using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // ===== Static Reference =====
    public static UIManager Instance { get; private set; }

    // ===== Serialized Fields =====
    [Header("Round Timer")]
    [SerializeField] private TextMeshProUGUI roundTimerText;
    [SerializeField] private Image roundTimerBackground;
    [SerializeField] private Color roundTimerStartColor = Color.green;
    [SerializeField] private Color roundTimerEndColor = Color.red;

    [Header("Round Info")]
    [SerializeField] private TextMeshProUGUI roundCounterText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        UpdateRoundTimer();
        UpdateRoundCounter();
    }

    private void UpdateRoundTimer()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.Object == null || !GameManager.Instance.Object.IsValid) return;

        // Game Over state
        if (GameManager.Instance.IsGameOver)
        {
            if (roundTimerText != null)
                roundTimerText.text = "Game Over";

            if (roundTimerBackground != null)
                roundTimerBackground.color = roundTimerEndColor;

            return;
        }

        float? remaining = GameManager.Instance.RoundTimeRemaining;
        float totalDuration = GameManager.Instance.RoundDuration;

        if (remaining.HasValue)
        {
            // Update timer text
            if (roundTimerText != null)
            {
                int totalSeconds = Mathf.CeilToInt(remaining.Value);
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                roundTimerText.text = $"{minutes:00}:{seconds:00}";
            }

            // Update image color (lerp from start -> end as time runs out)
            if (roundTimerBackground != null && totalDuration > 0f)
            {
                float t = 1f - Mathf.Clamp01(remaining.Value / totalDuration);
                roundTimerBackground.color = Color.Lerp(roundTimerStartColor, roundTimerEndColor, t);
            }
        }
        else
        {
            if (roundTimerText != null)
                roundTimerText.text = "00:00";

            if (roundTimerBackground != null)
                roundTimerBackground.color = roundTimerEndColor;
        }
    }

    private void UpdateRoundCounter()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.Object == null || !GameManager.Instance.Object.IsValid) return;
        if (roundCounterText == null) return;

        int current = GameManager.Instance.CurrentRound;
        int max = GameManager.Instance.MaxRounds;

        if (current > 0)
            roundCounterText.text = $"Round {current}/{max}";
        else
            roundCounterText.text = "";
    }
}