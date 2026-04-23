using UnityEngine;

public class ArrowVisual : MonoBehaviour
{
    // ===== Constants =====
    private const float PREDICTION_TIMEOUT = 0.5f;
    private const float SMOOTH_SPEED = 30f;
    private const float SNAP_THRESHOLD = 2f;

    // ===== Public State =====
    public int BufferIndex { get; private set; }
    public bool IsPredictedHit { get; private set; }
    public bool PredictionTimerExpired => _predictionTimer <= 0f;

    // ===== Private Fields =====
    private Renderer[] _cachedRenderers;
    private bool _vfxPlayed;
    private float _predictionTimer;

    private void Awake() {
        _cachedRenderers = GetComponentsInChildren<Renderer>();
    }

    public void Init(int bufferIndex, Vector2 direction, Vector2 position) {
        BufferIndex = bufferIndex;
        IsPredictedHit = false;
        _vfxPlayed = false;
        _predictionTimer = PREDICTION_TIMEOUT;

        transform.position = (Vector3)position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void UpdatePosition(Vector3 newPosition) {
        float distance = Vector3.Distance(transform.position, newPosition);

        if (distance > SNAP_THRESHOLD) {
            transform.position = newPosition;
        } else {
            transform.position = Vector3.Lerp(transform.position, newPosition, SMOOTH_SPEED * Time.deltaTime);
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

    public void Finish(Vector3 hitPosition, Transform vfxPrefab) {
        PlayVFX(hitPosition, vfxPrefab);
        Destroy(gameObject);
    }

    public void Expire() {
        Destroy(gameObject);
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
