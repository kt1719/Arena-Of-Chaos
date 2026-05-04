using System;
using Fusion;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    // ===== Networked Properties =====
    [Networked] private float _dashCurrentCooldown { get; set; }
    [Networked] private bool _isDashing { get; set; }

    // ===== Networked Dash Snapshot (mirrors ArrowData fire snapshot) =====
    [Networked] private int _dashStartTick { get; set; }
    [Networked] private Vector2 _dashStartPosition { get; set; }
    [Networked] private Vector2 _dashDirection { get; set; }

    // ===== Serialized Fields =====
    [SerializeField] private Knockback _playerKnockback;
    [SerializeField] private PlayerCombat _playerCombat;
    [SerializeField] private Collider2D _bodyCollider;

    // ===== Events =====
    public event Action OnDashStart;
    public event Action OnDashEnd;

    // ===== Private Variables =====
    private PlayerStats _stats;
    private Rigidbody2D _rb;
    private ChangeDetector _changeDetector;

    // ===== Public Accessors =====
    public bool IsDashing => _isDashing;
    public int DashStartTick => _dashStartTick;
    public Vector2 DashStartPosition => _dashStartPosition;
    public Vector2 DashDirection => _dashDirection;
    public float DashSpeed => _stats != null ? _stats.MoveSpeed * _stats.DashSpeedMultiplier : 0f;

    private float EffectiveSpeed => _isDashing
        ? _stats.MoveSpeed * _stats.DashSpeedMultiplier
        : _stats.MoveSpeed;

    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
    }

    public override void Spawned()
    {
        _stats = GetComponent<PlayerStats>();
        if (_stats == null)
        {
            Debug.LogError($"[PlayerMovement] PlayerStats component not found on {gameObject.name}. Movement will not function correctly.", this);
            return;
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _dashCurrentCooldown = 0;
        _isDashing = false;

        _playerCombat.OnDied += OnDied;
        _playerCombat.OnRespawned += EnableBody;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_playerCombat != null)
        {
            _playerCombat.OnDied -= OnDied;
            _playerCombat.OnRespawned -= EnableBody;
        }
    }

    public override void Render()
    {
        if (_changeDetector == null) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(_isDashing):
                    if (_isDashing)
                        OnDashStart?.Invoke();
                    else
                        OnDashEnd?.Invoke();
                    break;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (_stats == null) return;

        // Knockback interrupts an active dash so the deterministic position
        // formula doesn't keep claiming a position the player isn't actually
        // at while the rigidbody is being driven by the knockback.
        if (_isDashing && _playerKnockback.IsKnockedBack)
            EndDash();

        if (_playerKnockback.IsKnockedBack) return;

        if (GetInput(out NetworkInputData data))
        {
            bool dashPressed = data.buttons.IsSet(NetworkInputData.DASH);

            // Tick the cooldown regardless of dash state.
            if (_dashCurrentCooldown > 0)
                _dashCurrentCooldown = Mathf.Max(0, _dashCurrentCooldown - Runner.DeltaTime);

            if (_isDashing)
            {
                TickDash();
            }
            else
            {
                TryStartDash(dashPressed, data.movementDirection);
                MovePlayer(data.movementDirection.normalized);
            }
        }
    }

    private void MovePlayer(Vector2 movementDirection)
    {
        _rb.linearVelocity = movementDirection * EffectiveSpeed;
    }

    // ===== Dash =====

    /// <summary>
    /// Strict rising-edge dash start. Snapshots tick/position/direction so all
    /// peers can compute the dasher's position from
    /// <c>startPos + direction * dashSpeed * elapsed</c>, mirroring the
    /// ArrowData fire-snapshot pattern. Only writes the snapshot fields when
    /// transitioning false→true so resimulation doesn't continuously rewrite
    /// _dashStartTick.
    /// </summary>
    private void TryStartDash(bool dashPressed, Vector2 movementDirection)
    {
        if (!dashPressed || _dashCurrentCooldown > 0) return;

        // Refuse zero-input dash: burning a cooldown on a no-op is bad UX
        // and would seed _dashDirection = Vector2.zero, breaking the formula.
        if (movementDirection.sqrMagnitude < 0.0001f) return;

        _dashStartTick = Runner.Tick;
        _dashStartPosition = _rb.position;
        _dashDirection = movementDirection.normalized;
        _isDashing = true;
    }

    /// <summary>
    /// Drives dash motion using the locked-in direction (NOT current input)
    /// so the trajectory is deterministic and observable to all peers via the
    /// dash snapshot fields. Velocity-based motion preserves natural wall
    /// collisions; the wall-stop heuristic ends the dash early so i-frames
    /// don't pin against geometry.
    /// </summary>
    private void TickDash()
    {
        float elapsed = (Runner.Tick - _dashStartTick) * Runner.DeltaTime;
        if (elapsed >= _stats.DashTotalDuration)
        {
            EndDash();
            return;
        }

        // Wall-stop: read the post-physics velocity from the previous tick
        // before overwriting it. If physics zeroed it (collision), the dash
        // is wedged against geometry — end it so i-frames don't pin and the
        // deterministic formula doesn't keep producing positions the rb
        // isn't actually at. Skip on the first dash tick (elapsed near zero)
        // because last frame's velocity was pre-dash idle.
        if (elapsed > Runner.DeltaTime && _rb.linearVelocity.sqrMagnitude < 0.01f)
        {
            EndDash();
            return;
        }

        _rb.linearVelocity = _dashDirection * EffectiveSpeed;
    }

    private void EndDash() {
        _isDashing = false;
        _dashCurrentCooldown = _stats.DashTotalCooldown;
    }

    // ===== Deterministic Position Query =====

    /// <summary>
    /// Returns the deterministic dash position at <paramref name="elapsed"/>
    /// seconds since dash start. Mirrors <see cref="ArrowData.GetPosition"/>.
    /// Caller is responsible for checking <see cref="IsDashing"/> first —
    /// the formula is meaningless when no dash is active.
    /// </summary>
    public Vector2 GetDashPosition(float elapsed) =>
        _dashStartPosition + _dashDirection * (DashSpeed * elapsed);

    /// <summary>
    /// Convenience overload that converts a tick to elapsed time.
    /// Mirrors <see cref="ArrowData.GetPositionAtTick"/>.
    /// </summary>
    public Vector2 GetDashPositionAtTick(int tick) =>
        GetDashPosition((tick - _dashStartTick) * Runner.DeltaTime);

    // ===== Event Handlers =====

    private void OnDied()
    {
        // Defensive: end any active dash on death so the deterministic
        // formula doesn't keep producing positions for a corpse.
        if (_isDashing) EndDash();
        DisableBody();
    }

    private void DisableBody() => SetBodyEnabled(false);
    private void EnableBody()  => SetBodyEnabled(true);

    private void SetBodyEnabled(bool enabled)
    {
        if (_bodyCollider != null) _bodyCollider.enabled = enabled;
    }
}
