using System;
using UnityEngine;

/// <summary>
/// Visual representation of a single arrow projectile.
/// Pool-friendly — never self-destroys; always returned via <see cref="OnReturnToPool"/>.
/// Owns its own TrailRenderer lifecycle for clean reuse.
/// </summary>
public class ArrowVisual : MonoBehaviour
{
    // ── Constants ──
    private const float PREDICTION_TIMEOUT = 0.5f;

    // ── Identity (set once per spawn) ──
    public int BufferIndex { get; private set; }
    public int FireTick { get; private set; }
    public Vector2 SnapshotPosition { get; private set; }
    public Vector2 SnapshotDirection { get; private set; }

    // ── First-Visible Timing ──
    /// <summary>
    /// The render time when this arrow first became visible on screen.
    /// Used as the time origin for position calculation so proxy clients
    /// see the arrow start at FirePosition instead of jumping ahead by
    /// the network propagation delay.
    /// </summary>
    public float FirstVisibleRenderTime { get; private set; }

    // ── Prediction State ──
    public bool IsPredictedHit { get; private set; }
    public bool PredictionTimerExpired => _predictionTimer <= 0f;

    /// <summary>
    /// Callback used by the pool owner to reclaim this visual.
    /// Set by <see cref="ArrowVisualManager"/> after retrieval from pool.
    /// </summary>
    public Action<ArrowVisual> OnReturnToPool { get; set; }

    // ── Cached Components ──
    private Renderer[] _renderers;
    private TrailRenderer _trail;

    // ── Internal State ──
    private bool _vfxPlayed;
    private float _predictionTimer;
    private bool _pooled;

    // ════════════════════════════════════════
    //  Lifecycle
    // ════════════════════════════════════════

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _trail = GetComponentInChildren<TrailRenderer>();
    }

    /// <summary>
    /// Initialises identity fields and resets all mutable state for a fresh spawn.
    /// </summary>
    public void Init(int bufferIndex, int fireTick, Vector2 direction, Vector2 position)
    {
        BufferIndex = bufferIndex;
        FireTick = fireTick;
        SnapshotPosition = position;
        SnapshotDirection = direction;
        _pooled = false;
        FirstVisibleRenderTime = -1f;

        ResetVisualState();

        transform.position = (Vector3)position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ════════════════════════════════════════
    //  Position & Trail
    // ════════════════════════════════════════

    public void SetPosition(Vector3 worldPos) => transform.position = worldPos;

    /// <summary>
    /// Records the render time when this arrow first becomes visible.
    /// Only records once — subsequent calls are no-ops.
    /// </summary>
    public void MarkFirstVisible(float renderTime)
    {
        if (FirstVisibleRenderTime < 0f)
            FirstVisibleRenderTime = renderTime;
    }

    public void ClearTrail()
    {
        if (_trail != null) _trail.Clear();
    }

    public void SetTrailEmitting(bool emitting)
    {
        if (_trail != null) _trail.emitting = emitting;
    }

    // ════════════════════════════════════════
    //  Client-Side Hit Prediction
    // ════════════════════════════════════════

    /// <summary>
    /// Hides the arrow and plays a predicted-hit VFX.
    /// The visual stays alive so the manager can confirm or roll back.
    /// </summary>
    public void PredictHit(Vector2 hitPosition, Transform vfxPrefab)
    {
        IsPredictedHit = true;
        _predictionTimer = PREDICTION_TIMEOUT;

        SetRenderersVisible(false);
        SetTrailEmitting(false);
        PlayVFX(hitPosition, vfxPrefab);
    }

    public void TickPredictionTimer(float deltaTime) => _predictionTimer -= deltaTime;

    /// <summary>
    /// Server didn't confirm the hit within the timeout — make the arrow visible again.
    /// </summary>
    public void RecoverFromMisprediction()
    {
        IsPredictedHit = false;
        _predictionTimer = PREDICTION_TIMEOUT;

        SetRenderersVisible(true);
        ClearTrail();
        SetTrailEmitting(true);
    }

    // ════════════════════════════════════════
    //  Terminal States
    // ════════════════════════════════════════

    /// <summary>
    /// Plays the hit VFX without returning to pool.
    /// Used by the manager when it handles recycling separately.
    /// </summary>
    public void PlayHitVFX(Vector2 position, Transform vfxPrefab)
    {
        PlayVFX(position, vfxPrefab);
    }

    /// <summary>
    /// Server-confirmed hit. Plays VFX and signals the pool to reclaim this visual.
    /// </summary>
    public void Finish(Vector3 hitPosition, Transform vfxPrefab)
    {
        PlayVFX(hitPosition, vfxPrefab);
        ReturnToPool();
    }

    /// <summary>
    /// Arrow expired (lifetime ended, no hit). Signals the pool to reclaim.
    /// </summary>
    public void Expire() => ReturnToPool();

    // ════════════════════════════════════════
    //  Internals
    // ════════════════════════════════════════

    private void ResetVisualState()
    {
        IsPredictedHit = false;
        _vfxPlayed = false;
        _predictionTimer = PREDICTION_TIMEOUT;

        SetRenderersVisible(true);
        SetTrailEmitting(false);
        ClearTrail();
    }

    private void SetRenderersVisible(bool visible)
    {
        for (int i = 0; i < _renderers.Length; i++)
            _renderers[i].enabled = visible;
    }

    private void PlayVFX(Vector2 position, Transform vfxPrefab)
    {
        if (vfxPrefab == null || _vfxPlayed) return;
        _vfxPlayed = true;
        Instantiate(vfxPrefab, (Vector3)position, Quaternion.identity);
    }

    private void ReturnToPool()
    {
        if (_pooled) return;
        _pooled = true;
        OnReturnToPool?.Invoke(this);
    }
}
