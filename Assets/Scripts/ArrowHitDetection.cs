using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// All server-side arrow hit detection: lag-compensated entity hits,
/// environment raycasts, and destructible pass-through.
/// Plain C# — not a MonoBehaviour.
/// </summary>
public class ArrowHitDetection
{
    // ── Pre-allocated buffers (zero per-frame GC) ──
    private readonly List<LagCompensatedHit> _lagCompHits = new();
    private readonly List<RaycastHit2D> _raycastResults = new();

    // ── Config (set once via Init) ──
    private NetworkRunner _runner;
    private PlayerRef _owner;
    private float _hitRadius;
    private LayerMask _environmentLayer;

    public void Init(NetworkRunner runner, PlayerRef owner, float hitRadius, LayerMask environmentLayer)
    {
        _runner = runner;
        _owner = owner;
        _hitRadius = hitRadius;
        _environmentLayer = environmentLayer;
    }

    // ════════════════════════════════════════
    //  Entity Hits (lag-compensated)
    // ════════════════════════════════════════

    /// <summary>
    /// Checks for hittable entities at <paramref name="position"/> using Fusion's
    /// lag-compensated OverlapSphere. On a hit the arrow is resolved in the buffer
    /// and damage is applied (forward ticks only to avoid resimulation double-hits).
    /// </summary>
    /// <returns>True if an entity was hit and the arrow should be consumed.</returns>
    public bool DetectEntityHit(
        ref ArrowData data, Vector2 position, int bufferIndex,
        NetworkArray<ArrowData> buffer, int damage, Vector2 direction,
        float knockbackForce, float knockbackDuration)
    {
        _lagCompHits.Clear();

        _runner.LagCompensation.OverlapSphere(
            (Vector3)position, _hitRadius, _owner,
            _lagCompHits, options: HitOptions.SubtickAccuracy);

        foreach (var hit in _lagCompHits)
        {
            if (!TryGetHittable(hit, out IHittable hittable)) continue;

            if (_runner.IsForward)
                hittable.ApplyHit(damage, direction, knockbackForce, knockbackDuration);

            ResolveArrow(ref data, position, bufferIndex, buffer);
            return true;
        }

        return false;
    }

    // ════════════════════════════════════════
    //  Environment Hits (raycast)
    // ════════════════════════════════════════

    /// <summary>
    /// Raycasts along the arrow's movement vector against the environment layer.
    /// Destructibles are intentionally skipped — they're handled by
    /// <see cref="DetectDestructibleHit"/>.
    /// </summary>
    /// <returns>True if a solid wall/floor was hit and the arrow should stop.</returns>
    public bool DetectEnvironmentHit(
        ref ArrowData data, Vector2 origin, Vector2 direction, float distance,
        int bufferIndex, NetworkArray<ArrowData> buffer)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, _environmentLayer);

        if (hit.collider == null) return false;
        if (hit.collider.GetComponent<Destructible>() != null) return false;

        ResolveArrow(ref data, hit.point, bufferIndex, buffer);
        return true;
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
    /// Filters out null hitboxes and arrows hitting their own owner.
    /// </summary>
    private bool TryGetHittable(LagCompensatedHit hit, out IHittable hittable)
    {
        hittable = null;

        if (hit.Hitbox == null || hit.Hitbox.Root == null) return false;

        NetworkObject target = hit.Hitbox.Root.Object;
        if (target == null || target.InputAuthority == _owner) return false;

        hittable = target.GetComponent<IHittable>();
        return hittable != null;
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
