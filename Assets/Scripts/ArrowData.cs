using Fusion;
using UnityEngine;

public struct ArrowData : INetworkStruct
{
    // ===== Fire Data =====
    public int FireTick;
    public Vector2 FirePosition;
    public Vector2 FireDirection;

    // ===== Resolution Data =====
    public Vector2 HitPosition;
    public int FinishTick;

    // ===== State =====
    public NetworkBool IsActive;

    public bool IsFinished => FinishTick > 0;

    public Vector2 GetPosition(float elapsedTime, float speed) {
        if (elapsedTime <= 0f)
            return FirePosition;

        return FirePosition + FireDirection * speed * elapsedTime;
    }

    public Vector2 GetPositionAtTick(int tick, float deltaTime, float speed) {
        float elapsed = (tick - FireTick) * deltaTime;
        return GetPosition(elapsed, speed);
    }
}
