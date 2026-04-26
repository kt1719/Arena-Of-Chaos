using Fusion;
using UnityEngine;

/// <summary>
/// Ticks every active arrow in the networked ring buffer each FixedUpdateNetwork.
/// Handles lifetime expiry, position sampling, and delegates hit detection.
/// Plain C# — not a MonoBehaviour.
/// </summary>
public class ArrowSimulation
{
    // ── Movement ──
    private NetworkRunner _runner;
    private float _arrowSpeed;
    private int _lifetimeTicks;

    // ── Damage (forwarded to hit detection) ──
    private int _damage;
    private float _knockbackForce;
    private float _knockbackDuration;

    // ── Subsystem ──
    private readonly ArrowHitDetection _hitDetection = new();

    /// <summary>Minimum movement magnitude to run directional raycasts.</summary>
    private const float MIN_MOVE_THRESHOLD = 0.001f;

    public void Init(
        NetworkRunner runner, PlayerRef owner,
        float arrowSpeed, float arrowLifetime,
        float hitRadius, LayerMask environmentLayer,
        int damage, float knockbackForce, float knockbackDuration)
    {
        _runner = runner;
        _arrowSpeed = arrowSpeed;
        _lifetimeTicks = Mathf.CeilToInt(arrowLifetime / runner.DeltaTime);
        _damage = damage;
        _knockbackForce = knockbackForce;
        _knockbackDuration = knockbackDuration;

        _hitDetection.Init(runner, owner, hitRadius, environmentLayer);
    }

    /// <summary>
    /// Called each FixedUpdateNetwork by BowWeapon (state authority only).
    /// Iterates all active arrows: expires stale ones, then runs hit detection.
    /// </summary>
    public void Tick(NetworkArray<ArrowData> buffer, int capacity)
    {
        for (int i = 0; i < capacity; i++)
        {
            var data = buffer[i];
            if (!data.IsActive || data.IsFinished) continue;

            if (TryExpire(ref data, i, buffer)) continue;

            TickArrow(ref data, i, buffer);
        }
    }

    // ════════════════════════════════════════
    //  Per-Arrow Logic
    // ════════════════════════════════════════

    private bool TryExpire(ref ArrowData data, int index, NetworkArray<ArrowData> buffer)
    {
        if (_runner.Tick - data.FireTick < _lifetimeTicks) return false;

        data.Resolve(Vector2.zero, _runner.Tick);
        buffer.Set(index, data);
        return true;
    }

    private void TickArrow(ref ArrowData data, int index, NetworkArray<ArrowData> buffer)
    {
        Vector2 prevPos = data.GetPositionAtTick(_runner.Tick - 1, _runner.DeltaTime, _arrowSpeed);
        Vector2 currPos = data.GetPositionAtTick(_runner.Tick, _runner.DeltaTime, _arrowSpeed);

        // Entity overlap runs even on the fire tick (zero movement) —
        // at close range the arrow may already overlap a target at FirePosition.
        if (_hitDetection.DetectEntityHit(
                ref data, currPos, index, buffer,
                _damage, data.FireDirection, _knockbackForce, _knockbackDuration))
            return;

        // Directional raycasts need a movement vector — skip when stationary.
        Vector2 moveDir = currPos - prevPos;
        float moveDist = moveDir.magnitude;
        if (moveDist < MIN_MOVE_THRESHOLD) return;

        Vector2 moveNorm = moveDir / moveDist;

        if (_hitDetection.DetectEnvironmentHit(ref data, prevPos, moveNorm, moveDist, index, buffer))
            return;

        _hitDetection.DetectDestructibleHit(prevPos, moveNorm, moveDist);
    }
}
