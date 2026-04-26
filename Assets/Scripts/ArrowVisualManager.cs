using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Manages the visual (render-side) lifecycle of arrows:
/// pooling, spawning, position interpolation, trail timing,
/// and client-side hit prediction (shooter only).
/// Plain C# — not a MonoBehaviour.
/// </summary>
public class ArrowVisualManager
{
    /// <summary>
    /// Delay before trail emission starts — gives resimulation corrections
    /// time to settle so the trail doesn't record a snap artifact.
    /// </summary>
    private const float TRAIL_START_DELAY = 0.1f;

    // ── Config (set once via Init) ──
    private int _bufferCapacity;
    private float _arrowSpeed;
    private float _hitRadius;
    private LayerMask _environmentLayer;
    private Transform _hitVfxPrefab;
    private NetworkRunner _runner;
    private NetworkObject _networkObject;

    // ── Active visuals (mirrors the networked ring buffer) ──
    private ArrowVisual[] _activeVisuals;
    private int _visibleFireCount;

    // ── Object pool ──
    private readonly Queue<ArrowVisual> _pool = new();
    private GameObject _prefab;
    private Transform _poolRoot;

    // ── Pre-allocated overlap buffer for prediction ──
    private readonly Collider2D[] _predictionOverlaps = new Collider2D[16];

    // ════════════════════════════════════════
    //  Initialisation
    // ════════════════════════════════════════

    public void Init(
        NetworkRunner runner, NetworkObject networkObject, int bufferCapacity,
        float arrowSpeed, float hitRadius, LayerMask environmentLayer,
        Transform hitVfxPrefab, GameObject arrowVisualPrefab, int initialFireCount)
    {
        _runner = runner;
        _networkObject = networkObject;
        _bufferCapacity = bufferCapacity;
        _arrowSpeed = arrowSpeed;
        _hitRadius = hitRadius;
        _environmentLayer = environmentLayer;
        _hitVfxPrefab = hitVfxPrefab;
        _prefab = arrowVisualPrefab;
        _visibleFireCount = initialFireCount;

        _activeVisuals = new ArrowVisual[bufferCapacity];
        WarmPool(bufferCapacity);
    }

    // ════════════════════════════════════════
    //  Pool
    // ════════════════════════════════════════

    private void WarmPool(int count)
    {
        var go = new GameObject("[ArrowVisualPool]");
        Object.DontDestroyOnLoad(go);
        _poolRoot = go.transform;

        if (_prefab == null) return;

        for (int i = 0; i < count; i++)
        {
            ArrowVisual visual = CreateInstance();
            visual.gameObject.SetActive(false);
            _pool.Enqueue(visual);
        }
    }

    private ArrowVisual Rent(Vector2 position)
    {
        ArrowVisual visual = _pool.Count > 0 ? _pool.Dequeue() : CreateInstance();
        visual.transform.position = (Vector3)position;
        return visual;
    }

    private void Recycle(ArrowVisual visual)
    {
        if (visual == null) return;
        visual.gameObject.SetActive(false);
        visual.transform.SetParent(_poolRoot);
        _pool.Enqueue(visual);
    }

    private ArrowVisual CreateInstance()
    {
        GameObject go = Object.Instantiate(_prefab, _poolRoot);
        go.SetActive(false);

        ArrowVisual visual = go.GetComponent<ArrowVisual>();
        visual.OnReturnToPool = Recycle;
        return visual;
    }

    // ════════════════════════════════════════
    //  Spawning (called from Render)
    // ════════════════════════════════════════

    /// <summary>
    /// Catches the visual layer up to the current networked fire count.
    /// Creates new <see cref="ArrowVisual"/> instances for any arrows that
    /// don't yet have a visual representation.
    /// </summary>
    public void SpawnNewVisuals(NetworkArray<ArrowData> buffer, int fireCount)
    {
        // Resimulation can momentarily drop fireCount — just reset our counter.
        if (_visibleFireCount > fireCount)
            _visibleFireCount = fireCount;

        while (_visibleFireCount < fireCount)
        {
            int index = _visibleFireCount % _bufferCapacity;
            var data = buffer[index];
            ArrowVisual existing = _activeVisuals[index];

            // Guard: skip if a visual already exists for this exact arrow (resimulation re-spawn).
            if (existing != null && existing.FireTick == data.FireTick)
            {
                _visibleFireCount++;
                continue;
            }

            RecycleSlot(index);
            SpawnVisualForSlot(index, data);
            _visibleFireCount++;
        }
    }

    private void SpawnVisualForSlot(int index, ArrowData data)
    {
        if (_prefab == null || !data.IsActive) return;

        ArrowVisual visual = Rent(data.FirePosition);
        visual.Init(index, data.FireTick, data.FireDirection, data.FirePosition);
        visual.gameObject.SetActive(false);
        _activeVisuals[index] = visual;
    }

    // ════════════════════════════════════════
    //  Per-Frame Update (called from Render)
    // ════════════════════════════════════════

    /// <summary>
    /// Drives every active visual: recycles stale ones, finalises hits/expires,
    /// interpolates position, and runs client-side hit prediction (shooter only).
    /// </summary>
    public void UpdateVisuals(NetworkArray<ArrowData> buffer)
    {
        bool isOwner = _networkObject != null && _networkObject.HasInputAuthority;

        // Per Fusion docs: proxies render on the remote (interpolated) timeline,
        // matching the timeline the snapshot data lives on. Local/state authority
        // render on the local timeline. Mixing these causes the arrow to pop forward
        // along its trajectory on proxy clients.
        float renderTime = _networkObject.IsProxy
            ? _runner.RemoteRenderTime
            : _runner.LocalRenderTime;

        for (int i = 0; i < _bufferCapacity; i++)
        {
            ArrowVisual visual = _activeVisuals[i];
            if (visual == null) continue;

            var data = buffer[i];

            if (TryRecycleStale(i, visual, data)) continue;
            if (TryFinalise(i, visual, data)) continue;
            if (TryExpire(i, visual, data)) continue;

            float elapsed = renderTime - data.FireTick * _runner.DeltaTime;
            if (!TryUpdatePosition(visual, elapsed, out Vector2 prevPos, out Vector2 newPos))
                continue;

            // Only the shooter runs prediction. Proxies rely on the server-confirmed
            // hit path (TryFinalise) — predicting on a proxy compares mismatched
            // timelines and produced wrong results often enough to be net-negative.
            if (isOwner)
                RunShooterPrediction(visual, prevPos, newPos);
        }
    }

    // ── Update helpers ──

    /// <summary>Recycles a visual whose buffer slot has been reused by a newer arrow.</summary>
    private bool TryRecycleStale(int index, ArrowVisual visual, ArrowData data)
    {
        if (data.FireTick == visual.FireTick) return false;
        RecycleSlot(index);
        return true;
    }

    /// <summary>Handles a server-confirmed hit: plays VFX and recycles.</summary>
    private bool TryFinalise(int index, ArrowVisual visual, ArrowData data)
    {
        if (!data.IsFinished) return false;

        if (!visual.IsPredictedHit)
        {
            Vector2 hitPos = data.HitPosition != Vector2.zero
                ? data.HitPosition
                : (Vector2)visual.transform.position;

            visual.PlayHitVFX(hitPos, _hitVfxPrefab);
        }

        RecycleSlot(index);
        return true;
    }

    /// <summary>Handles an arrow that expired without hitting anything.</summary>
    private bool TryExpire(int index, ArrowVisual visual, ArrowData data)
    {
        if (data.IsActive) return false;
        RecycleSlot(index);
        return true;
    }

    /// <summary>
    /// Computes the deterministic render position and applies it to the visual.
    /// Returns false if the arrow shouldn't be visible yet (negative elapsed).
    /// </summary>
    private bool TryUpdatePosition(
        ArrowVisual visual, float elapsed,
        out Vector2 prevPos, out Vector2 newPos)
    {
        prevPos = default;
        newPos = default;

        // Negative elapsed = render time hasn't caught up to fire tick.
        // Showing the arrow now would pop it ahead of the interpolated weapon barrel.
        if (elapsed < 0f)
        {
            visual.gameObject.SetActive(false);
            return false;
        }

        // Position from snapshot — immune to resimulation mutations.
        newPos = visual.SnapshotPosition + visual.SnapshotDirection * (_arrowSpeed * elapsed);

        if (!visual.gameObject.activeSelf)
        {
            visual.SetPosition((Vector3)newPos);
            visual.ClearTrail();
            visual.gameObject.SetActive(true);
        }

        prevPos = visual.transform.position;
        visual.SetPosition((Vector3)newPos);

        if (elapsed >= TRAIL_START_DELAY)
            visual.SetTrailEmitting(true);

        return true;
    }

    // ════════════════════════════════════════
    //  Shooter-Side Hit Prediction
    // ════════════════════════════════════════

    /// <summary>
    /// Wraps the shooter's predicted-hit overlap check plus the misprediction
    /// recovery timer.
    /// </summary>
    private void RunShooterPrediction(ArrowVisual visual, Vector2 prevPos, Vector2 newPos)
    {
        if (!visual.IsPredictedHit)
            PredictLocalHit(visual, prevPos, newPos);

        if (visual.IsPredictedHit)
        {
            visual.TickPredictionTimer(Time.deltaTime);
            if (visual.PredictionTimerExpired)
                visual.RecoverFromMisprediction();
        }
    }

    /// <summary>
    /// Multi-sample swept overlap check matching the server's lag-compensated
    /// authority sampling. Same sample count as ArrowHitDetection.DetectEntityHit
    /// so prediction agrees with authority.
    /// </summary>
    private void PredictLocalHit(ArrowVisual visual, Vector2 prevPos, Vector2 currPos)
    {
        Vector2 delta = currPos - prevPos;
        float distance = delta.magnitude;

        if (distance < 0.0001f)
        {
            // Single overlap at current position.
            if (TryPredictAtPosition(visual, currPos)) return;
            return;
        }

        int sampleCount = Mathf.CeilToInt(distance / _hitRadius) + 1;

        for (int i = 0; i <= sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            Vector2 samplePos = Vector2.Lerp(prevPos, currPos, t);

            if (TryPredictAtPosition(visual, samplePos))
                return;
        }
    }

    /// <summary>
    /// Single-sample overlap check used by the multi-sample predictor.
    /// Returns true if a predicted hit was triggered at this position.
    /// </summary>
    private bool TryPredictAtPosition(ArrowVisual visual, Vector2 samplePos)
    {
        int count = Physics2D.OverlapCircleNonAlloc(
            samplePos, _hitRadius, _predictionOverlaps);

        for (int i = 0; i < count; i++)
        {
            Collider2D col = _predictionOverlaps[i];
            if (col == null) continue;

            // Skip self.
            if (col.transform.root == _networkObject.transform.root) continue;

            // Enemy entity?
            NetworkObject target = col.GetComponentInParent<NetworkObject>();
            if (target != null
                && target.InputAuthority != _networkObject.InputAuthority
                && target.GetComponent<IHittable>() != null)
            {
                visual.PredictHit(samplePos, _hitVfxPrefab);
                return true;
            }

            // Solid environment (non-destructible)?
            if (IsEnvironmentLayer(col) && col.GetComponent<Destructible>() == null)
            {
                visual.PredictHit(samplePos, _hitVfxPrefab);
                return true;
            }
        }

        return false;
    }

    // ════════════════════════════════════════
    //  Cleanup
    // ════════════════════════════════════════

    public void Cleanup()
    {
        if (_activeVisuals == null) return;

        for (int i = 0; i < _activeVisuals.Length; i++)
        {
            if (_activeVisuals[i] != null)
            {
                Object.Destroy(_activeVisuals[i].gameObject);
                _activeVisuals[i] = null;
            }
        }

        while (_pool.Count > 0)
        {
            ArrowVisual pooled = _pool.Dequeue();
            if (pooled != null) Object.Destroy(pooled.gameObject);
        }

        if (_poolRoot != null) Object.Destroy(_poolRoot.gameObject);
    }

    // ════════════════════════════════════════
    //  Shared Helpers
    // ════════════════════════════════════════

    private void RecycleSlot(int index)
    {
        if (_activeVisuals[index] == null) return;
        Recycle(_activeVisuals[index]);
        _activeVisuals[index] = null;
    }

    private bool IsEnvironmentLayer(Collider2D col)
    {
        return ((1 << col.gameObject.layer) & _environmentLayer) != 0;
    }
}