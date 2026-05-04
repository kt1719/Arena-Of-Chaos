using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Manages the visual (render-side) lifecycle of arrows: pooling, spawning,
/// deterministic-flight rendering with barrel-interpolation catchup, trail
/// timing, and client-side hit prediction (shooter only).
///
/// Visual rendering uses <see cref="NetworkRunner.LocalRenderTime"/> on every
/// observer so in-flight arrow positions converge across screens. The catchup
/// lerp absorbs the snapshot delay on proxies so arrows still appear to leave
/// the bow tip rather than spawning mid-trajectory.
///
/// Plain C# — not a MonoBehaviour.
/// </summary>
public class ArrowVisualManager
{
    /// <summary>
    /// Delay before trail emission starts. Two purposes:
    ///   1. Gives resimulation corrections time to settle so the trail doesn't
    ///      record a snap artifact.
    ///   2. Hides the accelerated catchup segment from the trail (which moves
    ///      faster than <c>_arrowSpeed</c> on proxies during the first
    ///      <see cref="CATCHUP_DURATION"/>).
    /// Kept equal to <see cref="CATCHUP_DURATION"/> so trail emission begins
    /// exactly when the visual is back on the deterministic flight line.
    /// </summary>
    private const float TRAIL_START_DELAY = 0.1f;

    /// <summary>
    /// Duration of the barrel-interpolation lerp from
    /// <see cref="ArrowVisual.SnapshotPosition"/> (the bow tip) to the
    /// deterministic trajectory point at <see cref="NetworkRunner.LocalRenderTime"/>.
    /// On the shooter and host this is effectively a no-op (first-render
    /// elapsed ≈ 0). On proxies the lerp absorbs the snapshot delay so the
    /// arrow visually leaves the bow tip rather than popping in mid-flight.
    /// Visual-only — does not affect prediction or hit detection.
    /// </summary>
    private const float CATCHUP_DURATION = 0.1f;

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
    // NOT reentrant — shooter and victim prediction passes both reuse this
    // buffer, but they run sequentially in the same UpdateVisuals call.
    private readonly Collider2D[] _predictionOverlaps = new Collider2D[16];

    // ── Victim-side prediction (lazy-resolved local hurtbox) ──
    /// <summary>
    /// Shorter than the shooter's 0.5s default. The victim mispredicts more
    /// often (any time the shooter actually missed, hit a wall first, or hit
    /// someone else) and a long timeout would visibly pop the arrow back in
    /// well after it should already have flown past the victim.
    /// </summary>
    private const float VICTIM_PREDICTION_TIMEOUT = 0.2f;
    private PlayerCombat _localCombat;
    private PlayerMovement _localMovement;

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
        if (_prefab == null) return;

        // Spawn for any real fire (FireTick > 0). Don't gate on IsActive —
        // a proxy may first observe a slot already in finished state for a
        // very-close-range hit; the visual still needs to fly its short arc
        // up to FinishTick on this peer's timeline before VFX plays.
        if (data.FireTick == 0) return;

        ArrowVisual visual = Rent(data.FirePosition);
        visual.Init(index, data.FireTick, data.FireDirection, data.FirePosition);
        visual.gameObject.SetActive(false);
        _activeVisuals[index] = visual;
    }

    // ════════════════════════════════════════
    //  Per-Frame Update (called from Render)
    // ════════════════════════════════════════

    /// <summary>
    /// Per-frame driver for every active visual. For each slot:
    ///   1. Recycle if the buffer slot has been reused by a newer arrow.
    ///   2. Advance the visual along its deterministic flight (with catchup).
    ///   3. Finalise (server-confirmed hit) or expire if the local render
    ///      timeline has reached the finish tick.
    ///   4. Run shooter-side hit prediction.
    /// </summary>
    public void UpdateVisuals(NetworkArray<ArrowData> buffer)
    {
        bool isOwner = _networkObject != null && _networkObject.HasInputAuthority;
        float renderTime = _runner.LocalRenderTime;

        for (int i = 0; i < _bufferCapacity; i++)
        {
            ArrowVisual visual = _activeVisuals[i];
            if (visual == null) continue;

            var data = buffer[i];

            if (TryRecycleStale(i, visual, data)) continue;

            // Advance position *before* finish/expire gates so the arrow keeps
            // moving up to the finish moment on this peer's render timeline
            // instead of snapping to HitPosition the instant the buffer flips
            // IsFinished.
            float elapsed = renderTime - data.FireTick * _runner.DeltaTime;
            if (!TryUpdatePosition(visual, elapsed, renderTime, out Vector2 prevPos, out Vector2 newPos))
                continue;

            if (TryFinalise(i, visual, data, renderTime)) continue;
            if (TryExpire(i, visual, data, renderTime)) continue;

            // Shooter predicts against all targets (full Physics2D scan).
            // Non-owner peers run a narrower victim-side prediction that
            // only checks the local player's own hurtbox — perfect-info
            // case (zero-latency knowledge of own position), so it doesn't
            // suffer the timeline mismatch that makes general proxy
            // prediction unreliable.
            if (isOwner)
                RunShooterPrediction(visual, prevPos, newPos);
            else
                RunVictimPrediction(visual, prevPos, newPos);
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

    /// <summary>
    /// Handles a server-confirmed hit: plays VFX and recycles. Gated on
    /// <see cref="HasReachedFinishTick"/> so the visual continues flying
    /// along its deterministic path until this peer's render timeline
    /// catches up to the finish moment — otherwise the buffer flipping
    /// <see cref="ArrowData.IsFinished"/> would snap the arrow to
    /// <see cref="ArrowData.HitPosition"/> mid-flight.
    /// </summary>
    private bool TryFinalise(int index, ArrowVisual visual, ArrowData data, float renderTime)
    {
        if (!data.IsFinished) return false;
        if (!HasReachedFinishTick(data, renderTime)) return false;

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

    /// <summary>
    /// Handles an arrow that expired without hitting anything. Same finish-tick
    /// gating as <see cref="TryFinalise"/> — <c>ArrowSimulation</c> writes a
    /// <see cref="ArrowData.FinishTick"/> on expiry too, so we wait for the
    /// local timeline to reach it before recycling.
    /// </summary>
    private bool TryExpire(int index, ArrowVisual visual, ArrowData data, float renderTime)
    {
        if (data.IsActive) return false;
        if (!HasReachedFinishTick(data, renderTime)) return false;

        RecycleSlot(index);
        return true;
    }

    /// <summary>
    /// True once this peer's render timeline reaches the server-stamped
    /// <see cref="ArrowData.FinishTick"/>. A <c>FinishTick</c> of 0 (defaulted,
    /// never set) is treated as "already reached" so callers don't need to
    /// special-case unset values.
    /// </summary>
    private bool HasReachedFinishTick(ArrowData data, float renderTime)
    {
        if (data.FinishTick <= 0) return true;
        return renderTime >= data.FinishTick * _runner.DeltaTime;
    }

    /// <summary>
    /// Advances the arrow this frame. Two parallel position computations:
    ///
    /// 1. <b>Deterministic</b> — produces the previous-frame and current-frame
    ///    positions on the arrow's true flight path. Returned via the out
    ///    params so prediction can sweep against the same trajectory the
    ///    server's hit query advances along. Same on every observer.
    ///
    /// 2. <b>Visual</b> — barrel-interpolated lerp from <see cref="ArrowVisual.SnapshotPosition"/>
    ///    toward the current deterministic position over <see cref="CATCHUP_DURATION"/>.
    ///    Render-side smoothing only; never feeds the prediction sweep.
    ///
    /// Returns false if the arrow shouldn't be visible yet (negative elapsed).
    /// </summary>
    private bool TryUpdatePosition(
        ArrowVisual visual, float elapsed, float renderTime,
        out Vector2 prevDeterministicPos, out Vector2 newDeterministicPos)
    {
        prevDeterministicPos = default;
        newDeterministicPos = default;

        // Negative elapsed = render time hasn't caught up to fire tick.
        // Showing the arrow now would pop it ahead of the interpolated weapon barrel.
        if (elapsed < 0f)
        {
            visual.gameObject.SetActive(false);
            return false;
        }

        // Deterministic sweep range: previous frame's flight position to this
        // frame's. Time.deltaTime gives the wall-clock gap between frames,
        // which is also the gap in `elapsed`. Identical math on every peer.
        float prevElapsed = Mathf.Max(0f, elapsed - Time.deltaTime);
        prevDeterministicPos =
            visual.SnapshotPosition + visual.SnapshotDirection * (_arrowSpeed * prevElapsed);
        newDeterministicPos =
            visual.SnapshotPosition + visual.SnapshotDirection * (_arrowSpeed * elapsed);

        // Barrel interpolation (visual only): lock first-render time, then
        // lerp from FirePos toward the current deterministic position over
        // CATCHUP_DURATION. On the shooter and host this is a near-no-op
        // (first-render elapsed ≈ 0). On proxies it absorbs the snapshot
        // delay so the arrow leaves the bow tip cleanly. The visual position
        // does NOT feed prediction — that uses the deterministic positions
        // above.
        visual.MarkFirstRender(renderTime);
        float catchupElapsed = Mathf.Max(0f, renderTime - visual.FirstRenderTime);

        Vector2 visualPos = catchupElapsed < CATCHUP_DURATION
            ? Vector2.Lerp(visual.SnapshotPosition, newDeterministicPos, catchupElapsed / CATCHUP_DURATION)
            : newDeterministicPos;

        if (!visual.gameObject.activeSelf)
        {
            visual.SetPosition((Vector3)visualPos);
            visual.ClearTrail();
            visual.gameObject.SetActive(true);
        }

        visual.SetPosition((Vector3)visualPos);

        // Trail starts after catchup so it doesn't record the accelerated segment.
        if (catchupElapsed >= TRAIL_START_DELAY)
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
    /// Multi-sample swept overlap check mirroring the sample count used by
    /// <see cref="ArrowHitDetection.DetectEntityHit"/>, so shooter-side
    /// prediction agrees with the server's authoritative result.
    /// </summary>
    private void PredictLocalHit(ArrowVisual visual, Vector2 prevPos, Vector2 currPos)
    {
        Vector2 delta = currPos - prevPos;
        float distance = delta.magnitude;

        // Stationary edge case (fire-tick frame): single overlap at current position.
        if (distance < 0.0001f)
        {
            TryPredictAtPosition(visual, currPos);
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
    ///
    /// With lag-comp on the server, the authoritative hit query rewinds
    /// targets to the shooter's view — which closely matches the proxy
    /// Rigidbody positions Physics2D sees on this machine. No extrapolation
    /// needed; the extra forward shift would actually push prediction past
    /// the rewound position the server uses.
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
                // Mirror the authority's i-frame gates from PlayerCombat.ApplyHit
                // so we don't predict a hit the server is guaranteed to reject —
                // that's the artifact where an arrow grazes a dasher and gets
                // stuck in a 0.5s predict-hide-recover loop.
                PlayerCombat targetCombat = target.GetComponent<PlayerCombat>();
                if (targetCombat != null)
                {
                    if (targetCombat.IsDead) continue;
                    if (targetCombat.IsInvincible) continue;
                    PlayerMovement targetMovement = target.GetComponent<PlayerMovement>();
                    if (targetMovement != null && targetMovement.IsDashing) continue;
                }

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
    //  Victim-Side Hit Prediction
    // ════════════════════════════════════════

    /// <summary>
    /// Mirrors the shooter-side prediction loop, but only checks the local
    /// player's own hurtbox. The local peer has zero-latency knowledge of
    /// its own position, so this is the one proxy-prediction case that
    /// avoids the timeline-mismatch problem that makes general proxy
    /// prediction unreliable. Predicts only the arrow's own VFX/disappear —
    /// the hit-flash / knockback / health-change still come from the
    /// authority's RPC + networked health path.
    /// </summary>
    private void RunVictimPrediction(ArrowVisual visual, Vector2 prevPos, Vector2 currPos)
    {
        if (!ResolveLocalPlayer()) return;

        // Defensive: the shooter case is already handled by the isOwner branch.
        // If the local player happens to also be the arrow owner, bail.
        if (_localCombat.Object.InputAuthority == _networkObject.InputAuthority) return;

        // Mirror authority i-frame gates from PlayerCombat.ApplyHit so we
        // don't predict a hit the server would reject.
        if (_localCombat.IsDead) return;
        if (_localCombat.IsInvincible) return;
        if (_localMovement != null && _localMovement.IsDashing) return;

        if (!visual.IsPredictedHit)
            PredictLocalVictimHit(visual, prevPos, currPos);

        if (visual.IsPredictedHit)
        {
            visual.TickPredictionTimer(Time.deltaTime);
            if (visual.PredictionTimerExpired)
                visual.RecoverFromMisprediction();
        }
    }

    /// <summary>
    /// Multi-sample swept overlap mirroring <see cref="PredictLocalHit"/>'s
    /// sample formula and <see cref="ArrowHitDetection.DetectEntityHit"/>'s
    /// sample count, so victim-side prediction agrees with the server's
    /// authoritative sweep.
    /// </summary>
    private void PredictLocalVictimHit(ArrowVisual visual, Vector2 prevPos, Vector2 currPos)
    {
        Vector2 delta = currPos - prevPos;
        float distance = delta.magnitude;

        if (distance < 0.0001f)
        {
            TryPredictVictimAtPosition(visual, currPos);
            return;
        }

        int sampleCount = Mathf.CeilToInt(distance / _hitRadius) + 1;

        for (int i = 0; i <= sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            Vector2 samplePos = Vector2.Lerp(prevPos, currPos, t);

            if (TryPredictVictimAtPosition(visual, samplePos))
                return;
        }
    }

    private bool TryPredictVictimAtPosition(ArrowVisual visual, Vector2 samplePos)
    {
        Collider2D hurtBox = _localCombat.HurtBox;
        if (hurtBox == null || !hurtBox.enabled) return false;

        // ClosestPoint returns samplePos itself if it's already inside the
        // collider (distance 0), so this handles the inside-the-hurtbox
        // case naturally.
        Vector2 closest = hurtBox.ClosestPoint(samplePos);
        if (Vector2.Distance(samplePos, closest) > _hitRadius) return false;

        visual.PredictHit(samplePos, _hitVfxPrefab, VICTIM_PREDICTION_TIMEOUT);
        return true;
    }

    /// <summary>
    /// Lazy-resolves the local player's combat + movement components.
    /// Returns false until the local player has spawned. Re-resolves if
    /// the cached object goes null (respawn / scene reload).
    /// </summary>
    private bool ResolveLocalPlayer()
    {
        if (_localCombat != null && _localCombat.Object != null) return true;

        NetworkObject localObj = NetworkManager.Instance != null
            ? NetworkManager.Instance.LocalPlayerObject
            : null;
        if (localObj == null) return false;

        _localCombat = localObj.GetComponent<PlayerCombat>();
        _localMovement = localObj.GetComponent<PlayerMovement>();
        return _localCombat != null;
    }

    // ════════════════════════════════════════
    //  Cleanup
    // ════════════════════════════════════════

    public void Cleanup()
    {
        _localCombat = null;
        _localMovement = null;

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
