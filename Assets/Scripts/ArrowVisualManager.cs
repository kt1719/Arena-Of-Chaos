using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class ArrowVisualManager
{
    // Delay before trail emits — lets resimulation corrections settle first.
    private const float TRAIL_START_DELAY = 0.1f;

    private int _bufferCapacity;
    private float _arrowSpeed;
    private float _hitRadius;
    private LayerMask _environmentLayer;
    private Transform _hitPredictionVFX;
    private NetworkRunner _runner;
    private NetworkObject _networkObject;

    private ArrowVisual[] _activeVisuals;
    private int _visibleFireCount;

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

        InitPool(bufferCapacity);
    }

    // ===== Pool =====

    private void InitPool(int initialCount) {
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
        ArrowVisual visual = _pool.Count > 0 ? _pool.Dequeue() : CreateVisualInstance();
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
        go.SetActive(false);
        ArrowVisual visual = go.GetComponent<ArrowVisual>();
        visual.OnReturnToPool = Return;
        return visual;
    }

    // ===== Visual Spawning =====

    public void SpawnNewVisuals(NetworkArray<ArrowData> buffer, int fireCount) {
        // Resimulation can momentarily drop _fireCount — just reset the counter.
        if (_visibleFireCount > fireCount) {
            _visibleFireCount = fireCount;
        }

        while (_visibleFireCount < fireCount) {
            int index = _visibleFireCount % _bufferCapacity;
            var data = buffer[index];

            ArrowVisual existing = _activeVisuals[index];

            // Skip if a visual already exists for this exact arrow (resimulation re-spawn guard).
            if (existing != null && existing.FireTick == data.FireTick) {
                _visibleFireCount++;
                continue;
            }

            if (existing != null) {
                Return(existing);
                _activeVisuals[index] = null;
            }

            if (_prefab != null && data.IsActive) {
                ArrowVisual visual = Get(data.FirePosition);

                if (visual != null) {
                    visual.Init(index, data.FireTick, data.FireDirection, data.FirePosition);
                    visual.gameObject.SetActive(false);
                    _activeVisuals[index] = visual;
                }
            }

            _visibleFireCount++;
        }
    }

    // ===== Visual Updates =====

    public void UpdateVisuals(NetworkArray<ArrowData> buffer) {
        bool isInputAuthority = _networkObject != null && _networkObject.HasInputAuthority;
        NetworkObject localPlayerObject = NetworkManager.Instance != null ? NetworkManager.Instance.LocalPlayerObject : null;

        // LocalRenderTime for all clients — arrows are deterministic, no interpolation needed.
        // RemoteRenderTime on proxies caused visuals to lag behind the server hitbox.
        float renderTime = _runner.LocalRenderTime;

        for (int i = 0; i < _bufferCapacity; i++) {
            ArrowVisual visual = _activeVisuals[i];
            if (visual == null) continue;

            var data = buffer[i];

            if (TryRecycleStaleVisual(i, visual, data)) continue;
            if (TryFinishVisual(i, visual, data)) continue;
            if (TryExpireVisual(i, visual, data)) continue;

            float elapsed = renderTime - data.FireTick * _runner.DeltaTime;

            var positions = ComputeAndApplyPosition(visual, elapsed);
            if (positions == null) continue;

            var (prevPos, newPos) = positions.Value;

            RunPredictionTick(visual, prevPos, newPos, isInputAuthority, localPlayerObject);
        }
    }

    // ===== Visual Update Helpers =====

    private bool TryRecycleStaleVisual(int index, ArrowVisual visual, ArrowData data) {
        if (data.FireTick != visual.FireTick) {
            Return(visual);
            _activeVisuals[index] = null;
            return true;
        }

        return false;
    }

    private bool TryFinishVisual(int index, ArrowVisual visual, ArrowData data) {
        if (!data.IsFinished) return false;

        if (visual.IsPredictedHit) {
            Return(visual);
        } else {
            Vector2 hitPos = data.HitPosition != Vector2.zero
                ? data.HitPosition
                : (Vector2)visual.transform.position;
            // Clear callback to prevent Finish from double-pooling.
            visual.OnReturnToPool = null;
            visual.Finish((Vector3)hitPos, _hitPredictionVFX);
            visual.OnReturnToPool = Return;
            Return(visual);
        }

        _activeVisuals[index] = null;
        return true;
    }

    private bool TryExpireVisual(int index, ArrowVisual visual, ArrowData data) {
        if (data.IsActive) return false;

        visual.Expire();
        _activeVisuals[index] = null;
        return true;
    }

    private (Vector2 prevPos, Vector2 newPos)? ComputeAndApplyPosition(ArrowVisual visual, float elapsed) {
        // Negative elapsed = render time hasn't caught up to fire tick yet.
        // Showing the arrow now would pop it ahead of the interpolated weapon barrel.
        if (elapsed < 0f) {
            visual.gameObject.SetActive(false);
            return null;
        }

        // Position from snapshot — immune to resimulation mutations.
        Vector2 newPos = visual.SnapshotPosition + visual.SnapshotDirection * _arrowSpeed * elapsed;

        if (!visual.gameObject.activeSelf) {
            // Position before activating so TrailRenderer doesn't record a snap artifact.
            visual.UpdatePosition((Vector3)newPos);
            visual.ClearTrail();
            visual.gameObject.SetActive(true);
        }

        Vector2 prevPos = visual.transform.position;
        visual.UpdatePosition((Vector3)newPos);

        if (elapsed >= TRAIL_START_DELAY) {
            visual.ResumeTrail();
        }

        return (prevPos, newPos);
    }

    private void RunPredictionTick(ArrowVisual visual, Vector2 prevPos, Vector2 newPos, bool isInputAuthority, NetworkObject localPlayerObject) {
        if (isInputAuthority && !visual.IsPredictedHit) {
            RunLocalHitPrediction(visual, prevPos, newPos);
        }

        if (!isInputAuthority && localPlayerObject != null && !visual.IsPredictedHit) {
            // Skip arrows fired by the local player — those use the shooter prediction path above.
            if (_runner.LocalPlayer != _networkObject.InputAuthority) {
                RunProxyHitPrediction(visual, prevPos, newPos, localPlayerObject);
            }
        }

        if (visual.IsPredictedHit) {
            visual.TickPredictionTimer(Time.deltaTime);

            if (visual.PredictionTimerExpired) {
                visual.RecoverFromMisprediction();
            }
        }
    }

    // ===== Client-Side Hit Prediction =====

    private void RunProxyHitPrediction(ArrowVisual visual, Vector2 prevPos, Vector2 currPos, NetworkObject localPlayerObject) {
        Vector2 moveDir = currPos - prevPos;
        if (moveDir.magnitude < 0.001f) return;

        Collider2D[] results = Physics2D.OverlapCircleAll(currPos, _hitRadius);

        foreach (Collider2D col in results) {
            if (col.transform.root == localPlayerObject.transform.root) {
                visual.PredictHit(currPos, _hitPredictionVFX);
                return;
            }
        }
    }

    private void RunLocalHitPrediction(ArrowVisual visual, Vector2 prevPos, Vector2 currPos) {
        Vector2 moveDir = currPos - prevPos;
        if (moveDir.magnitude < 0.001f) return;

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
