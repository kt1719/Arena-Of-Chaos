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
    }

    private void UpdateRoundTimer()
    {
        if (GameManager.Instance == null) return;

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
}