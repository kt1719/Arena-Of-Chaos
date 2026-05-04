using System;
using Fusion;
using UnityEngine;

public class SlimeLunge : NetworkBehaviour
{
    // ===== Serialized Fields =====
    [Header("Timing")]
    [SerializeField] private float _windUpDuration = 0.4f;
    [SerializeField] private float _dashMaxDuration = 0.3f;
    [SerializeField] private float _recoveryDuration = 0.3f;
    [SerializeField] private float _cooldown = 1.5f;

    [Header("Dash")]
    [SerializeField] private float _dashSpeed = 12f;

    [Header("Damage")]
    [SerializeField] private int _damage = 10;
    [SerializeField] private float _knockbackForce = 8f;
    [SerializeField] private float _knockbackDuration = 0.2f;

    [Header("Hit Detection")]
    [SerializeField] private float _hitRadius = 0.5f;
    [SerializeField] private LayerMask _playerLayer;

    // ===== Events =====
    public event Action OnLungeStart;
    public event Action OnLungeEnd;

    // ===== Public Properties =====
    public bool IsLunging => _phase != LungePhase.Idle;
    public bool IsOnCooldown => _phase == LungePhase.Idle && _cooldownTimer > 0f;
    public bool CanLunge => _phase == LungePhase.Idle && _cooldownTimer <= 0f;

    // ===== Private Variables =====
    private Rigidbody2D _rb;

    private LungePhase _phase;
    private float _phaseTimer;
    private float _cooldownTimer;
    private Vector2 _dashDirection;
    private bool _hasHitThisLunge;

    private enum LungePhase { Idle, WindUp, Dashing, Recovery }

    // ===== Lifecycle =====

    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
    }

    public override void FixedUpdateNetwork() {
        if (!HasStateAuthority) return;

        TickCooldown();

        switch (_phase)
        {
            case LungePhase.WindUp:   TickWindUp();   break;
            case LungePhase.Dashing:  TickDash();     break;
            case LungePhase.Recovery: TickRecovery(); break;
        }
    }

    // ===== Public API =====

    public void StartLunge(Vector2 targetPosition) {
        if (!HasStateAuthority || !CanLunge) return;

        _dashDirection = ((Vector2)targetPosition - (Vector2)transform.position).normalized;
        _hasHitThisLunge = false;

        EnterPhase(LungePhase.WindUp, _windUpDuration);
        OnLungeStart?.Invoke();
    }

    // ===== Phase Logic =====

    private void TickWindUp() {
        SetVelocity(Vector2.zero);

        if (!TickTimer()) return;
        EnterPhase(LungePhase.Dashing, _dashMaxDuration);
    }

    private void TickDash() {
        SetVelocity(_dashDirection * _dashSpeed);

        if (!_hasHitThisLunge)
            TryHitPlayer();

        if (!TickTimer()) return;
        EndDash();
    }

    private void TickRecovery() {
        SetVelocity(Vector2.zero);

        if (!TickTimer()) return;
        CompleteLunge();
    }

    private void TickCooldown() {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Runner.DeltaTime;
    }

    // ===== Hit Detection =====

    private void TryHitPlayer() {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _hitRadius, _playerLayer);

        foreach (Collider2D hit in hits)
        {
            if (hit.transform.root == transform.root) continue;

            HurtBox hurtBox = hit.GetComponent<HurtBox>();
            if (hurtBox == null || hurtBox.Owner == null) continue;

            hurtBox.Owner.ApplyHit(_damage, _dashDirection, _knockbackForce, _knockbackDuration, PlayerRef.None);
            _hasHitThisLunge = true;
            return;
        }
    }

    // ===== Collision =====

    private void OnCollisionEnter2D(Collision2D collision) {
        if (_phase == LungePhase.Dashing)
            EndDash();
    }

    // ===== Helpers =====

    private void EndDash() {
        SetVelocity(Vector2.zero);
        EnterPhase(LungePhase.Recovery, _recoveryDuration);
    }

    private void CompleteLunge() {
        _phase = LungePhase.Idle;
        _cooldownTimer = _cooldown;
        OnLungeEnd?.Invoke();
    }

    private void EnterPhase(LungePhase phase, float duration) {
        _phase = phase;
        _phaseTimer = duration;
    }

    private bool TickTimer() {
        _phaseTimer -= Runner.DeltaTime;
        return _phaseTimer <= 0f;
    }

    private void SetVelocity(Vector2 velocity) {
        _rb.linearVelocity = velocity;
    }
}
