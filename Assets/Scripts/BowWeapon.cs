using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

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
    [SerializeField] private float _arrowSpeed = 12f;
    [SerializeField] private float _arrowLifetime = 3f;

    [Header("Hit Detection")]
    [SerializeField] private float _hitRadius = 0.3f;
    [SerializeField] private LayerMask _environmentLayer;

    [Header("Hit Prediction VFX")]
    [SerializeField] private Transform _hitPredictionVFX;

    // ===== Private Fields =====
    private int _visibleFireCount;
    private ArrowVisual[] _activeVisuals;
    private int[] _visualFireTicks;
    private int _lifetimeInTicks;
    private readonly List<LagCompensatedHit> _lagCompHits = new();

    public override void Spawned() {
        base.Spawned();

        _activeVisuals = new ArrowVisual[BUFFER_CAPACITY];
        _visualFireTicks = new int[BUFFER_CAPACITY];
        _visibleFireCount = _fireCount;
        _lifetimeInTicks = Mathf.CeilToInt(_arrowLifetime / Runner.DeltaTime);
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        if (_activeVisuals == null) return;

        for (int i = 0; i < _activeVisuals.Length; i++) {
            if (_activeVisuals[i] != null) {
                Destroy(_activeVisuals[i].gameObject);
                _activeVisuals[i] = null;
            }
        }
    }

    public override void FixedUpdateNetwork() {
        base.FixedUpdateNetwork();

        if (GetInput(out NetworkInputData data)) {
            WeaponAimDirection = data.weaponAimDirection;
        }

        if (HasStateAuthority) {
            UpdateArrows();
        }
    }

    protected override bool AttackAction() {
        FireArrow();

        if (Runner.IsForward) {
            OnBowShoot?.Invoke();
        }

        return true;
    }

    private void FireArrow() {
        Vector2 aimDir = WeaponAimDirection.normalized;
        if (aimDir.sqrMagnitude < 0.01f) aimDir = Vector2.right;

        int index = _fireCount % BUFFER_CAPACITY;

        var data = new ArrowData
        {
            FireTick = Runner.Tick,
            FirePosition = (Vector2)arrowSpawnPoint.position,
            FireDirection = aimDir,
            HitPosition = Vector2.zero,
            FinishTick = 0,
            IsActive = true
        };

        _arrowBuffer.Set(index, data);
        _fireCount++;
    }

    // ===== Server-Authoritative Arrow Update =====

    private void UpdateArrows() {
        for (int i = 0; i < BUFFER_CAPACITY; i++) {
            var data = _arrowBuffer[i];
            if (!data.IsActive || data.IsFinished) continue;

            if (Runner.Tick - data.FireTick >= _lifetimeInTicks) {
                data.FinishTick = Runner.Tick;
                data.IsActive = false;
                _arrowBuffer.Set(i, data);
                continue;
            }

            Vector2 prevPos = data.GetPositionAtTick(Runner.Tick - 1, Runner.DeltaTime, _arrowSpeed);
            Vector2 currPos = data.GetPositionAtTick(Runner.Tick, Runner.DeltaTime, _arrowSpeed);
            Vector2 moveDir = currPos - prevPos;
            float moveDist = moveDir.magnitude;

            if (moveDist < 0.001f) continue;

            if (DetectEntityHit(ref data, currPos, i)) continue;
            if (DetectEnvironmentHit(ref data, prevPos, moveDir.normalized, moveDist, i)) continue;
            DetectDestructibleHit(prevPos, moveDir.normalized, moveDist);
        }
    }

    private bool DetectEntityHit(ref ArrowData data, Vector2 position, int index) {
        _lagCompHits.Clear();

        Runner.LagCompensation.OverlapSphere(
            (Vector3)position,
            _hitRadius,
            Object.InputAuthority,
            _lagCompHits,
            options: HitOptions.SubtickAccuracy
        );

        foreach (var hit in _lagCompHits) {
            if (hit.Hitbox == null) continue;

            NetworkObject targetNetObj = hit.Hitbox.Root.Object;
            if (targetNetObj == null || targetNetObj.InputAuthority == Object.InputAuthority) continue;

            IHittable hittable = targetNetObj.GetComponent<IHittable>();
            if (hittable == null) continue;

            hittable.ApplyHit(
                weaponInfo.weaponDamage,
                data.FireDirection,
                weaponInfo.knockbackForce,
                weaponInfo.knockbackDuration
            );

            data.HitPosition = position;
            data.FinishTick = Runner.Tick;
            data.IsActive = false;
            _arrowBuffer.Set(index, data);
            return true;
        }

        return false;
    }

    private bool DetectEnvironmentHit(ref ArrowData data, Vector2 origin, Vector2 direction, float distance, int index) {
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, _environmentLayer);

        if (hit.collider != null) {
            if (hit.collider.GetComponent<Destructible>() != null) return false;

            data.HitPosition = hit.point;
            data.FinishTick = Runner.Tick;
            data.IsActive = false;
            _arrowBuffer.Set(index, data);
            return true;
        }

        return false;
    }

    private void DetectDestructibleHit(Vector2 origin, Vector2 direction, float distance) {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, distance);

        foreach (var hit in hits) {
            if (hit.collider == null) continue;

            Destructible destructible = hit.collider.GetComponent<Destructible>();
            if (destructible == null) continue;

            destructible.DestroyGameObject();
        }
    }

    // ===== Visual Management =====

    public override void Render() {
        base.Render();

        if (_activeVisuals == null) return;

        SpawnNewVisuals();
        UpdateVisuals();
    }

    private void SpawnNewVisuals() {
        if (_visibleFireCount > _fireCount) {
            _visibleFireCount = _fireCount;
        }

        while (_visibleFireCount < _fireCount) {
            int index = _visibleFireCount % BUFFER_CAPACITY;
            var data = _arrowBuffer[index];

            if (_activeVisuals[index] != null) {
                Destroy(_activeVisuals[index].gameObject);
                _activeVisuals[index] = null;
                _visualFireTicks[index] = 0;
            }

            if (arrowVisualPrefab != null && data.IsActive) {
                GameObject visualGO = Instantiate(arrowVisualPrefab, (Vector3)data.FirePosition, Quaternion.identity);
                ArrowVisual visual = visualGO.GetComponent<ArrowVisual>();

                if (visual != null) {
                    visual.Init(index, data.FireDirection, data.FirePosition);
                    _activeVisuals[index] = visual;
                    _visualFireTicks[index] = data.FireTick;
                }
            }

            _visibleFireCount++;
        }
    }

    private void UpdateVisuals() {
        bool isInputAuthority = Object != null && Object.HasInputAuthority;
        float renderTime = (Object != null && Object.IsProxy)
            ? Runner.RemoteRenderTime
            : Runner.LocalRenderTime;

        for (int i = 0; i < BUFFER_CAPACITY; i++) {
            ArrowVisual visual = _activeVisuals[i];
            if (visual == null) continue;

            var data = _arrowBuffer[i];

            // Stale visual from rollback — buffer slot was overwritten
            if (data.FireTick != _visualFireTicks[i]) {
                Destroy(visual.gameObject);
                _activeVisuals[i] = null;
                _visualFireTicks[i] = 0;
                continue;
            }

            if (data.IsFinished) {
                if (visual.IsPredictedHit) {
                    Destroy(visual.gameObject);
                } else {
                    Vector2 hitPos = data.HitPosition != Vector2.zero ? data.HitPosition : (Vector2)visual.transform.position;
                    visual.Finish((Vector3)hitPos, _hitPredictionVFX);
                }

                _activeVisuals[i] = null;
                _visualFireTicks[i] = 0;
                continue;
            }

            if (!data.IsActive) {
                visual.Expire();
                _activeVisuals[i] = null;
                _visualFireTicks[i] = 0;
                continue;
            }

            float elapsed = renderTime - data.FireTick * Runner.DeltaTime;
            Vector2 newPos = data.GetPosition(elapsed, _arrowSpeed);
            Vector2 prevPos = visual.transform.position;

            visual.UpdatePosition((Vector3)newPos);

            if (isInputAuthority && !visual.IsPredictedHit) {
                RunLocalHitPrediction(visual, prevPos, newPos);
            }

            if (isInputAuthority && visual.IsPredictedHit) {
                visual.TickPredictionTimer(Time.deltaTime);

                if (visual.PredictionTimerExpired) {
                    visual.RecoverFromMisprediction();
                }
            }
        }
    }

    private void RunLocalHitPrediction(ArrowVisual visual, Vector2 prevPos, Vector2 currPos) {
        Vector2 moveDir = currPos - prevPos;
        float moveDist = moveDir.magnitude;

        if (moveDist < 0.001f) return;

        Collider2D[] results = Physics2D.OverlapCircleAll(currPos, _hitRadius);

        foreach (Collider2D col in results) {
            if (col.transform.root == transform.root) continue;

            NetworkObject targetNetObj = col.GetComponentInParent<NetworkObject>();
            if (targetNetObj != null && targetNetObj.InputAuthority != Object.InputAuthority
                && targetNetObj.GetComponent<IHittable>() != null) {
                visual.PredictHit(currPos, _hitPredictionVFX);
                return;
            }

            if (((1 << col.gameObject.layer) & _environmentLayer) != 0
                && col.GetComponent<Destructible>() == null) {
                visual.PredictHit(currPos, _hitPredictionVFX);
                return;
            }
        }
    }
}
