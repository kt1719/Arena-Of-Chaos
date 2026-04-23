using Fusion;
using UnityEngine;

/// <summary>
/// Networked data struct for a single arrow in the projectile data buffer.
/// Stored in a NetworkArray on BowWeapon — not a NetworkObject.
/// </summary>
public struct ArrowData : INetworkStruct
{
    // ===== Fire Data (set once on fire) =====
    public int FireTick;
    public Vector2 FirePosition;
    public Vector2 FireDirection;

    // ===== Resolution Data (set once on hit/expire) =====
    public Vector2 HitPosition;
    public int FinishTick;

    // ===== State =====
    public NetworkBool IsActive;

    // ===== Helpers =====

    public bool IsFinished => FinishTick > 0;

    /// <summary>
    /// Calculate the arrow's world position at a given time (in seconds since fire).
    /// </summary>
    public Vector2 GetPosition(float elapsedTime, float speed)
    {
        if (elapsedTime <= 0f)
            return FirePosition;

        return FirePosition + FireDirection * speed * elapsedTime;
    }

    /// <summary>
    /// Calculate the arrow's world position at a given tick.
    /// </summary>
    public Vector2 GetPositionAtTick(int tick, float deltaTime, float speed)
    {
        float elapsed = (tick - FireTick) * deltaTime;
        return GetPosition(elapsed, speed);
    }
}
