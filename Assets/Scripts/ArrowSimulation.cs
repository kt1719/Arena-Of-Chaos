using Fusion;
using UnityEngine;

/// <summary>
/// Ticks active arrows in the NetworkArray buffer each FixedUpdateNetwork.
/// Handles lifetime expiry, position calculation, and delegates hit detection.
/// Plain C# class — not a MonoBehaviour.
/// </summary>
public class ArrowSimulation
{
    // ===== Config =====
    private NetworkRunner _runner;
    private float _arrowSpeed;
    private int _lifetimeInTicks;

    // ===== Hit Detection Params (from WeaponInfo) =====
    private int _damage;
    private float _knockbackForce;
    private float _knockbackDuration;

    // ===== Subsystems =====
    private readonly ArrowHitDetection _hitDetection = new();

    public void Init(NetworkRunner runner, PlayerRef inputAuthority, float arrowSpeed, float arrowLifetime,
        float hitRadius, LayerMask environmentLayer, int damage, float knockbackForce, float knockbackDuration) {

        _runner = runner;
        _arrowSpeed = arrowSpeed;
        _lifetimeInTicks = Mathf.CeilToInt(arrowLifetime / runner.DeltaTime);
        _damage = damage;
        _knockbackForce = knockbackForce;
        _knockbackDuration = knockbackDuration;

        _hitDetection.Init(runner, inputAuthority, hitRadius, environmentLayer);
    }

    /// <summary>
    /// Called each FixedUpdateNetwork by BowWeapon (state authority only).
    /// Iterates all active arrows: expires, moves, detects hits.
    /// </summary>
    public void Tick(NetworkArray<ArrowData> buffer, int bufferCapacity) {
        for (int i = 0; i < bufferCapacity; i++) {
            var data = buffer[i];
            if (!data.IsActive || data.IsFinished) continue;

            if (_runner.Tick - data.FireTick >= _lifetimeInTicks) {
                data.FinishTick = _runner.Tick;
                data.IsActive = false;
                buffer.Set(i, data);
                continue;
            }

            Vector2 prevPos = data.GetPositionAtTick(_runner.Tick - 1, _runner.DeltaTime, _arrowSpeed);
            Vector2 currPos = data.GetPositionAtTick(_runner.Tick, _runner.DeltaTime, _arrowSpeed);
            Vector2 moveDir = currPos - prevPos;
            float moveDist = moveDir.magnitude;

            // Always run entity hit detection even on the fire tick when movement is zero.
            // At close range the arrow may overlap a target at FirePosition with no movement.
            if (_hitDetection.DetectEntityHit(ref data, currPos, i, buffer,
                _damage, data.FireDirection, _knockbackForce, _knockbackDuration)) continue;

            // Environment and destructible checks require a ray direction — skip when stationary.
            if (moveDist < 0.001f) continue;

            if (_hitDetection.DetectEnvironmentHit(ref data, prevPos, moveDir.normalized, moveDist, i, buffer)) continue;

            _hitDetection.DetectDestructibleHit(prevPos, moveDir.normalized, moveDist);
        }
    }
}
