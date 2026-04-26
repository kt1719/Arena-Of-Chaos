using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// All server-side arrow hit detection: lag-compensated entity hits,
/// lag-compensated environment raycasts, and destructible pass-through.
/// Plain C# — not a MonoBehaviour.
/// </summary>
public class ArrowHitDetection
{
    // ── Pre-allocated buffers (zero per-frame GC) ──
    private readonly List<LagCompensatedHit> _lagCompHits = new();
    private readonly HashSet<NetworkId> _hitDedupe = new();
    private readonly List<RaycastHit2D> _raycastResults = new();

    // ── Config (set once via Init) ──
    private NetworkRunner _runner;
    private PlayerRef _owner;
    private float _hitRadius;
    private LayerMask _environmentLayer;

    /// <summary>Minimum sweep distance below which we fall back to a single overlap.</summary>
    private const float MIN_SWEEP_DISTANCE = 0.0001f;

    public void Init(NetworkRunner runner, PlayerRef owner, float hitRadius, LayerMask environmentLayer)
    {
        _runner = runner;
        _owner = owner;
        _hitRadius = hitRadius;
        _environmentLayer = environmentLayer;
    }

    // ════════════════════════════════════════
    //  Entity Hits (multi-sample swept lag-comp)
    // ════════════════════════════════════════

    /// <summary>
    /// Multi-sample swept lag-compensated overlap between the arrow's previous and
    /// current tick positions. Samples are spaced so consecutive overlap spheres
    /// always overlap, eliminating the discretisation gap that lets fast targets
    /// slip between per-tick checks.
    /// </summary>
    /// <returns>True if an entity was hit and the arrow should be consumed.</returns>
    public bool DetectEntityHit(
        ref ArrowData data, Vector2 prevPosition, Vector2 currPosition,
        int bufferIndex, NetworkArray<ArrowData> buffer, int damage,
        Vector2 direction, float knockbackForce, float knockbackDuration)
    {
        _lagCompHits.Clear();
        _hitDedupe.Clear();

        const HitOptions hitOptions = HitOptions.SubtickAccuracy | HitOptions.IgnoreInputAuthority;

        Vector2 delta = currPosition - prevPosition;
        float distance = delta.magnitude;

        if (distance < MIN_SWEEP_DISTANCE)
        {
            // Fire-tick edge case: arrow hasn't moved yet. Single overlap at current position.
            _runner.LagCompensation.OverlapSphere(
                currPosition, _hitRadius, _owner, _lagCompHits, options: hitOptions);
        }
        else
        {
            // Multi-sample sweep: number of samples so consecutive spheres always overlap.
            // distance / radius + 1 guarantees overlap even when distance > radius.
            int sampleCount = Mathf.CeilToInt(distance / _hitRadius) + 1;

            for (int i = 0; i <= sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                Vector2 samplePos = Vector2.Lerp(prevPosition, currPosition, t);

                // Each call appends to _lagCompHits; we dedupe below.
                _runner.LagCompensation.OverlapSphere(
                    samplePos, _hitRadius, _owner, _lagCompHits, options: hitOptions);

                // Early exit: if we already have at least one valid hit, stop sampling.
                // The first valid hit closest to the start of the sweep is the correct one.
                if (HasValidHit()) break;
            }
        }

        // Resolve against the first valid hit found.
        foreach (var hit in _lagCompHits)
        {
            if (!TryGetHittable(hit, out IHittable hittable, out NetworkId hitId)) continue;
            if (!_hitDedupe.Add(hitId)) continue; // skip duplicates from overlapping samples

            if (_runner.IsForward)
                hittable.ApplyHit(damage, direction, knockbackForce, knockbackDuration);

            ResolveArrow(ref data, currPosition, bufferIndex, buffer);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Quick check used during sampling to early-exit as soon as a valid target appears.
    /// </summary>
    private bool HasValidHit()
    {
        for (int i = 0; i < _lagCompHits.Count; i++)
        {
            if (TryGetHittable(_lagCompHits[i], out _, out _))
                return true;
        }
        return false;
    }

    // ════════════════════════════════════════
    //  Environment Hits (lag-compensated raycast)
    // ════════════════════════════════════════

    /// <summary>
    /// Lag-compensated raycast against the environment layer using PhysX inclusion.
    /// Destructibles are intentionally skipped — they're handled by
    /// <see cref="DetectDestructibleHit"/>.
    /// </summary>
    /// <returns>True if a solid wall/floor was hit and the arrow should stop.</returns>
    public bool DetectEnvironmentHit(
        ref ArrowData data, Vector2 origin, Vector2 direction, float distance,
        int bufferIndex, NetworkArray<ArrowData> buffer)
    {
        const HitOptions hitOptions =
            HitOptions.IncludePhysX | HitOptions.SubtickAccuracy | HitOptions.IgnoreInputAuthority;

        if (!_runner.LagCompensation.Raycast(
                origin, direction, distance, _owner,
                out LagCompensatedHit hit, _environmentLayer, hitOptions))
        {
            return false;
        }

        // PhysX collider path (most environment will be PhysX colliders, not Hitboxes).
        if (hit.Collider != null)
        {
            if (hit.Collider.GetComponent<Destructible>() != null) return false;

            // PhysX queries via lag comp don't populate hit.Point automatically —
            // the Photon docs note developers must compute this themselves.
            // For a raycast, the hit point is along the ray at hit.Distance.
            Vector2 hitPoint = hit.Distance > 0f
                ? origin + direction * hit.Distance
                : origin;

            ResolveArrow(ref data, hitPoint, bufferIndex, buffer);
            return true;
        }

        // Fusion Hitbox path (rare for environment, but cleanly handled if present).
        if (hit.Hitbox != null)
        {
            ResolveArrow(ref data, hit.Point, bufferIndex, buffer);
            return true;
        }

        return false;
    }

    // ════════════════════════════════════════
    //  Destructible Pass-Through
    // ════════════════════════════════════════

    /// <summary>
    /// Destroys any destructible objects along the arrow path.
    /// The arrow passes through — it is not consumed.
    /// </summary>
    public void DetectDestructibleHit(Vector2 origin, Vector2 direction, float distance)
    {
        _raycastResults.Clear();
        var filter = new ContactFilter2D { useTriggers = true };
        Physics2D.Raycast(origin, direction, filter, _raycastResults, distance);

        for (int i = 0; i < _raycastResults.Count; i++)
        {
            Destructible destructible = _raycastResults[i].collider != null
                ? _raycastResults[i].collider.GetComponent<Destructible>()
                : null;

            destructible?.DestroyGameObject();
        }
    }

    // ════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════

    /// <summary>
    /// Validates a lag-comp hit and extracts the <see cref="IHittable"/> component.
    /// IgnoreInputAuthority handles owner filtering at the Fusion level — this is a
    /// final safety check plus IHittable extraction.
    /// </summary>
    private bool TryGetHittable(LagCompensatedHit hit, out IHittable hittable, out NetworkId id)
    {
        hittable = null;
        id = default;

        if (hit.Hitbox == null || hit.Hitbox.Root == null) return false;

        NetworkObject target = hit.Hitbox.Root.Object;
        if (target == null) return false;

        hittable = target.GetComponent<IHittable>();
        if (hittable == null) return false;

        id = target.Id;
        return true;
    }

    /// <summary>
    /// Marks an arrow as resolved (hit) and writes it back to the networked buffer.
    /// Centralises the three-field write that was previously duplicated per hit type.
    /// </summary>
    private void ResolveArrow(
        ref ArrowData data, Vector2 hitPos, int bufferIndex,
        NetworkArray<ArrowData> buffer)
    {
        data.Resolve(hitPos, _runner.Tick);
        buffer.Set(bufferIndex, data);
    }
}