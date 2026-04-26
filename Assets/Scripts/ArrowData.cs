using Fusion;
using UnityEngine;

/// <summary>
/// Networked state for a single arrow in the ring buffer.
/// Immutable fire data + mutable resolution data written by simulation.
/// </summary>
public struct ArrowData : INetworkStruct
{
    // ── Fire Snapshot (set once at spawn) ──
    public int FireTick;
    public Vector2 FirePosition;
    public Vector2 FireDirection;

    // ── Resolution (written by ArrowSimulation on hit/expire) ──
    public Vector2 HitPosition;
    public int FinishTick;

    // ── Lifetime Flag ──
    public NetworkBool IsActive;

    public bool IsFinished => FinishTick > 0;

    /// <summary>
    /// Returns the deterministic position of this arrow at a given elapsed time.
    /// </summary>
    public Vector2 GetPosition(float elapsedTime, float speed)
    {
        return elapsedTime <= 0f
            ? FirePosition
            : FirePosition + FireDirection * (speed * elapsedTime);
    }

    /// <summary>
    /// Convenience overload — converts a tick to elapsed time, then samples position.
    /// </summary>
    public Vector2 GetPositionAtTick(int tick, float deltaTime, float speed)
    {
        float elapsed = (tick - FireTick) * deltaTime;
        return GetPosition(elapsed, speed);
    }

    /// <summary>
    /// Marks this arrow as finished at the given position and tick.
    /// Centralises the three-field mutation that was previously duplicated
    /// across every hit-detection path.
    /// </summary>
    public void Resolve(Vector2 hitPos, int tick)
    {
        HitPosition = hitPos;
        FinishTick = tick;
        IsActive = false;
    }
}
