using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Manages arrow visual lifecycle: pooling, spawning, position updates,
/// client-side hit prediction, and cleanup.
/// Plain C# class — not a MonoBehaviour.
/// </summary>
public class ArrowVisualManager
{
    // ===== Config =====
    private int _bufferCapacity;
    private float _arrowSpeed;
    private float _hitRadius;
    private LayerMask _environmentLayer;
    private Transform _hitPredictionVFX;
    private NetworkRunner _runner;
    private NetworkObject _networkObject;

    // ===== Visual Tracking =====
    private ArrowVisual[] _activeVisuals;
    private int[] _visualFireTicks;
    private int _visibleFireCount;

    // ===== Object Pool =====
    private readonly Queue<ArrowVisual> _pool = new();
    private GameObject _prefab;
    private Transform _poolParent;

    public void Init(NetworkRunner runner, NetworkObject networkObject, int bufferCapacity,
        float arrowSpeed, float hitRadius, LayerMask environmentLayer, Transform hitPredictionVFX,
        GameObject arrowVisualPrefab, int initialFireCount) {

        _runner = runner;
        _networkObject = networkObject;
        _bufferCapacity = bufferCapacity;
        _arrowSpeed = arrowSpeed;
        _hitRadius = hitRadius;
        _environmentLayer = environmentLayer;
        _hitPredictionVFX = hitPredictionVFX;
        _prefab = arrowVisualPrefab;
        _visibleFireCount = initialFireCount;

        _activeVisuals = new ArrowVisual[bufferCapacity];
        _visualFireTicks = new int[bufferCapacity];

        InitPool(bufferCapacity);
    }

    // ===== Pool =====

    private void InitPool(int initialCount) {
        // Create a persistent parent for pooled objects
        var poolGO = new GameObject("[ArrowVisualPool]");
        Object.DontDestroyOnLoad(poolGO);
        _poolParent = poolGO.transform;

        if (_prefab == null) return;

        for (int i = 0; i < initialCount; i++) {
            ArrowVisual visual = CreateVisualInstance();
            visual.gameObject.SetActive(false);
            _pool.Enqueue(visual);
        }
    }

    private ArrowVisual Get(Vector2 position) {
        ArrowVisual visual;

        if (_pool.Count > 0) {
            visual = _pool.Dequeue();
            visual.gameObject.SetActive(true);
        } else {
            visual = CreateVisualInstance();
        }

        visual.transform.position = (Vector3)position;
        return visual;
    }

    private void Return(ArrowVisual visual) {
        if (visual == null) return;

        visual.gameObject.SetActive(false);
        visual.transform.SetParent(_poolParent);
        _pool.Enqueue(visual);
    }

    private ArrowVisual CreateVisualInstance() {
        GameObject go = Object.Instantiate(_prefab, _poolParent);
        ArrowVisual visual = go.GetComponent<ArrowVisual>();
        visual.OnReturnToPool = Return;
        return visual;
    }

    // ===== Visual Spawning =====

    public void SpawnNewVisuals(NetworkArray<ArrowData> buffer, int fireCount) {
        if (_visibleFireCount > fireCount) {
            _visibleFireCount = fireCount;
        }

        while (_visibleFireCount < fireCount) {
            int index = _visibleFireCount % _bufferCapacity;
            var data = buffer[index];

            if (_activeVisuals[index] != null) {
                Return(_activeVisuals[index]);
                _activeVisuals[index] = null;
                _visualFireTicks[index] = 0;
            }

            if (_prefab != null && data.IsActive) {
                ArrowVisual visual = Get(data.FirePosition);

                if (visual != null) {
                    visual.Init(index, data.FireDirection, data.FirePosition);
                    visual.gameObject.SetActive(false); // Hidden until render time catches up to fire tick
                    _activeVisuals[index] = visual;
                    _visualFireTicks[index] = data.FireTick;
                }
            }

            _visibleFireCount++;
        }
    }

    // ===== Visual Updates =====

    public void UpdateVisuals(NetworkArray<ArrowData> buffer) {
        bool isInputAuthority = _networkObject != null && _networkObject.HasInputAuthority;
        float renderTime = (_networkObject != null && _networkObject.IsProxy)
            ? _runner.RemoteRenderTime
            : _runner.LocalRenderTime;

        for (int i = 0; i < _bufferCapacity; i++) {
            ArrowVisual visual = _activeVisuals[i];
            if (visual == null) continue;

            var data = buffer[i];

            // Stale visual from rollback — buffer slot was overwritten
            if (data.FireTick != _visualFireTicks[i]) {
                Return(visual);
                _activeVisuals[i] = null;
                _visualFireTicks[i] = 0;
                continue;
            }

            if (data.IsFinished) {
                if (visual.IsPredictedHit) {
                    Return(visual);
                } else {
                    Vector2 hitPos = data.HitPosition != Vector2.zero
                        ? data.HitPosition
                        : (Vector2)visual.transform.position;
                    visual.Finish((Vector3)hitPos, _hitPredictionVFX);
                    // Finish plays VFX then returns to pool via OnReturnToPool callback
                }

                _activeVisuals[i] = null;
                _visualFireTicks[i] = 0;
                continue;
            }

            if (!data.IsActive) {
                visual.Expire();
                _activeVisuals[i] = null;
                _visualFireTicks[i] = 0;
                continue;
            }

            float elapsed = renderTime - data.FireTick * _runner.DeltaTime;

            // Arrow was fired at tick N but LocalRenderTime interpolates between tick N-1 and N.
            // Until render time catches up to the fire tick, elapsed is negative.
            // Showing the arrow at FirePosition during this window causes it to visually
            // pop ahead of the interpolated weapon barrel. Hide it for this sub-frame period.
            if (elapsed < 0f) {
                visual.gameObject.SetActive(false);
                continue;
            }

            Vector2 newPos = data.GetPosition(elapsed, _arrowSpeed);

            if (!visual.gameObject.activeSelf) {
                // Move transform to the correct position BEFORE activating so the
                // TrailRenderer doesn't record a segment from the stale pooled position
                // to the current computed position (the "snap" artifact).
                visual.UpdatePosition((Vector3)newPos);
                visual.ClearTrail();
                visual.gameObject.SetActive(true);
            }

            Vector2 prevPos = visual.transform.position;

            // Detect discontinuous jump: if the visual moved more than
            // the arrow could physically travel in one render frame, it's a correction.
            // Pause trail emission across the jump to prevent snap artifacts.
            float maxExpectedMove = _arrowSpeed * Time.deltaTime * 2f;
            float actualMove = Vector2.Distance(prevPos, newPos);

            if (actualMove > maxExpectedMove && visual.gameObject.activeSelf) {
                visual.PauseTrail();
                visual.UpdatePosition((Vector3)newPos);
                visual.ResumeTrail();
            } else {
                visual.UpdatePosition((Vector3)newPos);
            }

            if (isInputAuthority && !visual.IsPredictedHit) {
                RunLocalHitPrediction(visual, prevPos, newPos);
            }

            if (isInputAuthority && visual.IsPredictedHit) {
                visual.TickPredictionTimer(Time.deltaTime);

                if (visual.PredictionTimerExpired) {
                    visual.RecoverFromMisprediction();
                }
            }
        }
    }

    // ===== Client-Side Hit Prediction =====

    private void RunLocalHitPrediction(ArrowVisual visual, Vector2 prevPos, Vector2 currPos) {
        Vector2 moveDir = currPos - prevPos;
        float moveDist = moveDir.magnitude;

        if (moveDist < 0.001f) return;

        // Note: OverlapCircleAll still allocates — deferred per FR-2 scope (Render path only)
        Collider2D[] results = Physics2D.OverlapCircleAll(currPos, _hitRadius);

        foreach (Collider2D col in results) {
            if (col.transform.root == _networkObject.transform.root) continue;

            NetworkObject targetNetObj = col.GetComponentInParent<NetworkObject>();
            if (targetNetObj != null && targetNetObj.InputAuthority != _networkObject.InputAuthority
                && targetNetObj.GetComponent<IHittable>() != null) {
                visual.PredictHit(currPos, _hitPredictionVFX);
                return;
            }

            if (((1 << col.gameObject.layer) & _environmentLayer) != 0
                && col.GetComponent<Destructible>() == null) {
                visual.PredictHit(currPos, _hitPredictionVFX);
                return;
            }
        }
    }

    // ===== Cleanup =====

    public void Cleanup() {
        if (_activeVisuals == null) return;

        for (int i = 0; i < _activeVisuals.Length; i++) {
            if (_activeVisuals[i] != null) {
                Object.Destroy(_activeVisuals[i].gameObject);
                _activeVisuals[i] = null;
            }
        }

        while (_pool.Count > 0) {
            ArrowVisual pooled = _pool.Dequeue();
            if (pooled != null) {
                Object.Destroy(pooled.gameObject);
            }
        }

        if (_poolParent != null) {
            Object.Destroy(_poolParent.gameObject);
        }
    }
}
