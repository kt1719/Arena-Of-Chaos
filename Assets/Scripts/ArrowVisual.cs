using UnityEngine;

/// <summary>
/// Plain MonoBehaviour for arrow visual lifecycle.
/// Not a NetworkObject — instantiated and destroyed locally by BowWeapon.Render().
/// </summary>
public class ArrowVisual : MonoBehaviour
{
    // ===== Public State (read by BowWeapon) =====
    public int BufferIndex { get; private set; }
    public bool IsPredictedHit { get; private set; }
    public bool PredictionTimerExpired => _predictionTimer <= 0f;

    // ===== Configuration =====
    private const float PREDICTION_TIMEOUT = 0.5f;

    // ===== Private Fields =====
    private Renderer[] _cachedRenderers;
    private bool _vfxPlayed;
    private float _predictionTimer;

    private void Awake()
    {
        _cachedRenderers = GetComponentsInChildren<Renderer>();
    }

    /// <summary>
    /// Initialize the arrow visual with direction and position.
    /// Called once when the visual is spawned.
    /// </summary>
    public void Init(int bufferIndex, Vector2 direction, Vector2 position)
    {
        BufferIndex = bufferIndex;
        IsPredictedHit = false;
        _vfxPlayed = false;
        _predictionTimer = PREDICTION_TIMEOUT;

        transform.position = (Vector3)position;

        // Rotate sprite to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// Move the visual to a new calculated position.
    /// Called every Render frame by BowWeapon.
    /// </summary>
    public void UpdatePosition(Vector3 newPosition)
    {
        transform.position = newPosition;
    }

    /// <summary>
    /// Client-side hit prediction — hide the arrow and play VFX immediately.
    /// The server may confirm or deny this prediction.
    /// </summary>
    public void PredictHit(Vector2 hitPosition, Transform vfxPrefab)
    {
        IsPredictedHit = true;
        _predictionTimer = PREDICTION_TIMEOUT;

        SetRenderersEnabled(false);
        PlayVFX(hitPosition, vfxPrefab);
    }

    /// <summary>
    /// Tick the prediction reconciliation timer.
    /// Called every Render frame while IsPredictedHit is true.
    /// </summary>
    public void TickPredictionTimer(float deltaTime)
    {
        _predictionTimer -= deltaTime;
    }

    /// <summary>
    /// Misprediction recovery — re-show the arrow and resume movement.
    /// Called when the prediction timer expires but the server says the arrow is still active.
    /// </summary>
    public void RecoverFromMisprediction()
    {
        IsPredictedHit = false;
        _predictionTimer = PREDICTION_TIMEOUT;

        SetRenderersEnabled(true);
    }

    /// <summary>
    /// Server confirmed the arrow hit something. Play VFX and destroy.
    /// </summary>
    public void Finish(Vector3 hitPosition, Transform vfxPrefab)
    {
        PlayVFX(hitPosition, vfxPrefab);
        Destroy(gameObject);
    }

    /// <summary>
    /// Arrow expired (lifetime ended, no hit). Just destroy.
    /// </summary>
    public void Expire()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Safety: play VFX if not already played (e.g., server despawn path)
        // No-op if VFX was already played
    }

    // ===== Helpers =====

    private void SetRenderersEnabled(bool enabled)
    {
        for (int i = 0; i < _cachedRenderers.Length; i++)
        {
            _cachedRenderers[i].enabled = enabled;
        }
    }

    private void PlayVFX(Vector2 position, Transform vfxPrefab)
    {
        if (vfxPrefab == null || _vfxPlayed) return;

        _vfxPlayed = true;
        Instantiate(vfxPrefab, (Vector3)position, Quaternion.identity);
    }
}
