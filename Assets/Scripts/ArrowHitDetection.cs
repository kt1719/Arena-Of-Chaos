using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Handles all arrow hit detection: lag-compensated entity hits,
/// environment raycasts, and destructible pass-through.
/// Plain C# class — not a MonoBehaviour.
/// </summary>
public class ArrowHitDetection
{
    // ===== Pre-allocated Buffers (zero GC) =====
    private readonly List<LagCompensatedHit> _lagCompHits = new();
    private readonly List<RaycastHit2D> _raycastResults = new();

    // ===== Config =====
    private NetworkRunner _runner;
    private PlayerRef _inputAuthority;
    private float _hitRadius;
    private LayerMask _environmentLayer;

    public void Init(NetworkRunner runner, PlayerRef inputAuthority, float hitRadius, LayerMask environmentLayer) {
        _runner = runner;
        _inputAuthority = inputAuthority;
        _hitRadius = hitRadius;
        _environmentLayer = environmentLayer;
    }

    /// <summary>
    /// Lag-compensated entity hit detection via Fusion OverlapSphere.
    /// Returns true if an entity was hit and arrow should be consumed.
    /// </summary>
    public bool DetectEntityHit(ref ArrowData data, Vector2 position, int bufferIndex,
        NetworkArray<ArrowData> buffer, int damage, Vector2 direction, float knockbackForce, float knockbackDuration) {

        _lagCompHits.Clear();

        _runner.LagCompensation.OverlapSphere(
            (Vector3)position,
            _hitRadius,
            _inputAuthority,
            _lagCompHits,
            options: HitOptions.SubtickAccuracy
        );

        foreach (var hit in _lagCompHits) {
            if (hit.Hitbox == null || hit.Hitbox.Root == null) continue;

            NetworkObject targetNetObj = hit.Hitbox.Root.Object;
            if (targetNetObj == null || targetNetObj.InputAuthority == _inputAuthority) continue;

            IHittable hittable = targetNetObj.GetComponent<IHittable>();
            if (hittable == null) continue;

            // Only apply damage on forward ticks — resimulation would double-apply.
            if (_runner.IsForward) {
                hittable.ApplyHit(damage, direction, knockbackForce, knockbackDuration);
            }

            data.HitPosition = position;
            data.FinishTick = _runner.Tick;
            data.IsActive = false;
            buffer.Set(bufferIndex, data);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Raycast-based environment hit detection.
    /// Skips destructibles (they are handled separately).
    /// Returns true if a solid environment collider was hit and arrow should stop.
    /// </summary>
    public bool DetectEnvironmentHit(ref ArrowData data, Vector2 origin, Vector2 direction, float distance,
        int bufferIndex, NetworkArray<ArrowData> buffer) {

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, _environmentLayer);

        if (hit.collider != null) {
            if (hit.collider.GetComponent<Destructible>() != null) return false;

            data.HitPosition = hit.point;
            data.FinishTick = _runner.Tick;
            data.IsActive = false;
            buffer.Set(bufferIndex, data);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Detects and destroys destructible objects along the arrow path.
    /// Arrow passes through — does not stop.
    /// Uses pre-allocated list to avoid GC allocations.
    /// </summary>
    public void DetectDestructibleHit(Vector2 origin, Vector2 direction, float distance) {
        _raycastResults.Clear();
        var filter = new ContactFilter2D { useTriggers = true };
        Physics2D.Raycast(origin, direction, filter, _raycastResults, distance);

        for (int i = 0; i < _raycastResults.Count; i++) {
            if (_raycastResults[i].collider == null) continue;

            Destructible destructible = _raycastResults[i].collider.GetComponent<Destructible>();
            if (destructible == null) continue;

            destructible.DestroyGameObject();
        }
    }
}
