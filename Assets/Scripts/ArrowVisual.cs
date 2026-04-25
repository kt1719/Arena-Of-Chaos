using System;
using UnityEngine;

/// <summary>
/// Visual representation of an arrow. Pool-friendly — does not self-destroy.
/// Manages its own TrailRenderer lifecycle for clean reuse.
/// </summary>
public class ArrowVisual : MonoBehaviour
{
    // ===== Constants =====
    private const float PREDICTION_TIMEOUT = 0.5f;

    // ===== Public State =====
    public int BufferIndex { get; private set; }
    public bool IsPredictedHit { get; private set; }
    public bool PredictionTimerExpired => _predictionTimer <= 0f;

    /// <summary>
    /// Called when this visual is finished and should be returned to the pool.
    /// Set by ArrowVisualManager after retrieving from pool.
    /// </summary>
    public Action<ArrowVisual> OnReturnToPool { get; set; }

    // ===== Private Fields =====
    private Renderer[] _cachedRenderers;
    private TrailRenderer _trailRenderer;
    private bool _vfxPlayed;
    private float _predictionTimer;

    private void Awake() {
        _cachedRenderers = GetComponentsInChildren<Renderer>();
        _trailRenderer = GetComponentInChildren<TrailRenderer>();
    }

    /// <summary>
    /// Resets all state for clean reuse from pool.
    /// Called internally by Init and can be called externally for manual reset.
    /// </summary>
    public void ResetForReuse() {
        IsPredictedHit = false;
        _vfxPlayed = false;
        _predictionTimer = PREDICTION_TIMEOUT;

        SetRenderersEnabled(true);

        if (_trailRenderer != null) {
            _trailRenderer.Clear();
        }
    }

    public void Init(int bufferIndex, Vector2 direction, Vector2 position) {
        BufferIndex = bufferIndex;
        ResetForReuse();

        transform.position = (Vector3)position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void UpdatePosition(Vector3 newPosition) {
        transform.position = newPosition;
    }

    /// <summary>
    /// Clears the trail so no stale segments are drawn after a teleport or reactivation.
    /// </summary>
    public void ClearTrail() {
        if (_trailRenderer != null) {
            _trailRenderer.Clear();
        }
    }

    /// <summary>
    /// Temporarily disables trail emission so the next position change
    /// doesn't record a connecting segment (prevents snap artifacts).
    /// Call before a discontinuous position update, then call ResumeTrail() after.
    /// </summary>
    public void PauseTrail() {
        if (_trailRenderer != null) {
            _trailRenderer.emitting = false;
        }
    }

    public void ResumeTrail() {
        if (_trailRenderer != null) {
            _trailRenderer.emitting = true;
        }
    }

    public void PredictHit(Vector2 hitPosition, Transform vfxPrefab) {
        IsPredictedHit = true;
        _predictionTimer = PREDICTION_TIMEOUT;

        SetRenderersEnabled(false);
        PlayVFX(hitPosition, vfxPrefab);
    }

    public void TickPredictionTimer(float deltaTime) {
        _predictionTimer -= deltaTime;
    }

    public void RecoverFromMisprediction() {
        IsPredictedHit = false;
        _predictionTimer = PREDICTION_TIMEOUT;

        SetRenderersEnabled(true);
    }

    /// <summary>
    /// Called when the arrow has a confirmed hit. Plays VFX and returns to pool.
    /// </summary>
    public void Finish(Vector3 hitPosition, Transform vfxPrefab) {
        PlayVFX(hitPosition, vfxPrefab);
        OnReturnToPool?.Invoke(this);
    }

    /// <summary>
    /// Called when the arrow expires (lifetime ended, no hit). Returns to pool.
    /// </summary>
    public void Expire() {
        OnReturnToPool?.Invoke(this);
    }

    // ===== Helpers =====

    private void SetRenderersEnabled(bool enabled) {
        for (int i = 0; i < _cachedRenderers.Length; i++) {
            _cachedRenderers[i].enabled = enabled;
        }
    }

    private void PlayVFX(Vector2 position, Transform vfxPrefab) {
        if (vfxPrefab == null || _vfxPlayed) return;

        _vfxPlayed = true;
        Instantiate(vfxPrefab, (Vector3)position, Quaternion.identity);
    }
}
