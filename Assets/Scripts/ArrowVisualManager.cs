using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Manages the visual (render-side) lifecycle of arrows:
/// pooling, spawning, position interpolation, trail timing,
/// and client-side hit prediction.
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
    /// interpolates position, and runs client-side hit prediction.
    /// </summary>
    public void UpdateVisuals(NetworkArray<ArrowData> buffer)
    {
        bool isOwner = _networkObject != null && _networkObject.HasInputAuthority;
        NetworkObject localPlayer = NetworkManager.Instance != null
            ? NetworkManager.Instance.LocalPlayerObject
            : null;

        // LocalRenderTime for all clients — arrows are deterministic, no interpolation needed.
        float renderTime = _runner.LocalRenderTime;

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

            RunPrediction(visual, prevPos, newPos, isOwner, localPlayer);
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

        // Single recycle path — RecycleSlot handles pool return.
        RecycleSlot(index);
        return true;
    }

    /// <summary>Handles an arrow that expired without hitting anything.</summary>
    private bool TryExpire(int index, ArrowVisual visual, ArrowData data)
    {
        if (data.IsActive) return false;
        // Single recycle path — RecycleSlot handles pool return.
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

        float renderTime = _runner.LocalRenderTime;

        // On the first frame with non-negative elapsed, record the render time.
        // Subsequent frames measure elapsed relative to that moment so proxy clients
        // see the arrow start at FirePosition instead of teleporting ahead.
        if (!visual.HasBeenVisible)
            visual.MarkFirstVisible(renderTime);

        float visualElapsed = renderTime - visual.FirstVisibleRenderTime;

        // Position from snapshot — immune to resimulation mutations.
        newPos = visual.SnapshotPosition + visual.SnapshotDirection * (_arrowSpeed * visualElapsed);

        if (!visual.gameObject.activeSelf)
        {
            visual.SetPosition((Vector3)newPos);
            visual.ClearTrail();
            visual.gameObject.SetActive(true);
        }

        prevPos = visual.transform.position;
        visual.SetPosition((Vector3)newPos);

        if (visualElapsed >= TRAIL_START_DELAY)
            visual.SetTrailEmitting(true);

        return true;
    }

    // ════════════════════════════════════════
    //  Client-Side Hit Prediction
    // ════════════════════════════════════════

    private void RunPrediction(
        ArrowVisual visual, Vector2 prevPos, Vector2 newPos,
        bool isOwner, NetworkObject localPlayer)
    {
        if (!visual.IsPredictedHit)
        {
            if (isOwner)
                PredictLocalHit(visual, prevPos, newPos);
            else if (localPlayer != null && _runner.LocalPlayer != _networkObject.InputAuthority)
                PredictProxyHit(visual, newPos, localPlayer);
        }

        if (visual.IsPredictedHit)
        {
            visual.TickPredictionTimer(Time.deltaTime);
            if (visual.PredictionTimerExpired)
                visual.RecoverFromMisprediction();
        }
    }

    /// <summary>
    /// Shooter-side prediction: checks if the arrow overlaps any valid target
    /// (enemy entity or solid environment) and shows a predicted hit VFX.
    /// </summary>
    private void PredictLocalHit(ArrowVisual visual, Vector2 prevPos, Vector2 currPos)
    {
        if (!HasMoved(prevPos, currPos)) return;

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(currPos, _hitRadius);

        foreach (Collider2D col in overlaps)
        {
            // Skip self.
            if (col.transform.root == _networkObject.transform.root) continue;

            // Enemy entity?
            NetworkObject target = col.GetComponentInParent<NetworkObject>();
            if (target != null
                && target.InputAuthority != _networkObject.InputAuthority
                && target.GetComponent<IHittable>() != null)
            {
                visual.PredictHit(currPos, _hitVfxPrefab);
                return;
            }

            // Solid environment (non-destructible)?
            if (IsEnvironmentLayer(col) && col.GetComponent<Destructible>() == null)
            {
                visual.PredictHit(currPos, _hitVfxPrefab);
                return;
            }
        }
    }

    /// <summary>
    /// Proxy-side prediction: checks if an incoming arrow overlaps the local player
    /// so the hit feels instant even though the arrow belongs to a remote shooter.
    /// </summary>
    private void PredictProxyHit(ArrowVisual visual, Vector2 currPos, NetworkObject localPlayer)
    {
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(currPos, _hitRadius);

        foreach (Collider2D col in overlaps)
        {
            if (col.transform.root == localPlayer.transform.root)
            {
                visual.PredictHit(currPos, _hitVfxPrefab);
                return;
            }
        }
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

    private static bool HasMoved(Vector2 a, Vector2 b)
    {
        return (b - a).sqrMagnitude > 0.000001f;
    }
}
