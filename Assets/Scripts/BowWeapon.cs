using System;
using Fusion;
using UnityEngine;

/// <summary>
/// Thin orchestrator for the arrow data buffer pattern.
/// Owns networked state and delegates simulation + visuals to subsystems.
/// </summary>
public class BowWeapon : BaseWeapon
{
    // ===== Constants =====
    private const int BUFFER_CAPACITY = 8;

    // ===== Events =====
    public event Action OnBowShoot;

    // ===== Networked Fields =====
    [Networked] private Vector2 WeaponAimDirection { get; set; }
    [Networked] private int _fireCount { get; set; }
    [Networked, Capacity(BUFFER_CAPACITY)] private NetworkArray<ArrowData> _arrowBuffer => default;

    // ===== Serialized Fields =====
    [SerializeField] private Transform arrowSpawnPoint;
    [SerializeField] private GameObject arrowVisualPrefab;

    [Header("Arrow Properties")]
    [SerializeField] private float _arrowSpeed = 13f;
    [SerializeField] private float _arrowLifetime = 3f;

    [Header("Hit Detection")]
    [SerializeField] private float _hitRadius = 0.3f;
    [SerializeField] private LayerMask _environmentLayer;

    [Header("Hit Prediction VFX")]
    [SerializeField] private Transform _hitPredictionVFX;

    // ===== Subsystems =====
    private ArrowSimulation _simulation;
    private ArrowVisualManager _visualManager;

    // ===== Lifecycle =====

    public override void Spawned() {
        base.Spawned();

        _simulation = new ArrowSimulation();
        _simulation.Init(
            Runner, Object.InputAuthority,
            _arrowSpeed, _arrowLifetime, _hitRadius, _environmentLayer,
            weaponInfo.weaponDamage, weaponInfo.knockbackForce, weaponInfo.knockbackDuration
        );

        _visualManager = new ArrowVisualManager();
        _visualManager.Init(
            Runner, Object, BUFFER_CAPACITY,
            _arrowSpeed, _hitRadius, _environmentLayer, _hitPredictionVFX,
            arrowVisualPrefab, _fireCount
        );
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        _visualManager?.Cleanup();
    }

    // ===== Network Tick =====

    public override void FixedUpdateNetwork() {
        base.FixedUpdateNetwork();

        if (GetInput(out NetworkInputData data)) {
            WeaponAimDirection = data.weaponAimDirection;
        }

        if (HasStateAuthority) {
            _simulation.Tick(_arrowBuffer, BUFFER_CAPACITY);
        }
    }

    // ===== Attack =====

    protected override bool AttackAction() {
        FireArrow();

        if (Runner.IsForward) {
            OnBowShoot?.Invoke();
        }

        return true;
    }

    private void FireArrow() {
        Vector2 aimDir = WeaponAimDirection;
        if (aimDir.sqrMagnitude < 0.01f) aimDir = Vector2.right;
        else aimDir = aimDir.normalized;

        int index = _fireCount % BUFFER_CAPACITY;

        var arrowData = new ArrowData
        {
            FireTick = Runner.Tick,
            FirePosition = (Vector2)arrowSpawnPoint.position,
            FireDirection = aimDir,
            HitPosition = Vector2.zero,
            FinishTick = 0,
            IsActive = true
        };

        _arrowBuffer.Set(index, arrowData);
        _fireCount++;
    }

    // ===== Render =====

    public override void Render() {
        base.Render();

        if (_visualManager == null) return;

        _visualManager.SpawnNewVisuals(_arrowBuffer, _fireCount);
        _visualManager.UpdateVisuals(_arrowBuffer);
    }
}
