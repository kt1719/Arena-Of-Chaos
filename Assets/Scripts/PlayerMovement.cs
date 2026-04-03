using System;
using Fusion;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    // ===== Networked Properties =====
    [Networked] private float _moveSpeed { get; set; }
    [Networked] private float _dashCurrentDuration { get; set; }
    [Networked] private float _dashCurrentCooldown { get; set; }
    [Networked] private bool _isDashing { get; set; }

    // ===== Serialized Fields =====
    [SerializeField] private PlayerKnockback _playerKnockback;
    // ===== Events =====
    public event Action OnDashStart;
    public event Action OnDashEnd;
    
    // ===== Private Variables =====
    private PlayerStats _stats;
    private Rigidbody2D _rb;
    private ChangeDetector _changeDetector;
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
        Init();
    }

    public override void Render()
    {
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

    private void Init() {
        _moveSpeed = _stats.MoveSpeed;
        _dashCurrentDuration = 0;
        _dashCurrentCooldown = 0;
        _isDashing = false;
    }

    public override void FixedUpdateNetwork()
    {
        if (_playerKnockback.IsKnockedBack) return;

        if (GetInput(out NetworkInputData data))
        {
            Vector2 movementDirection = data.movementDirection.normalized;
            bool dashPressed = data.buttons.IsSet(NetworkInputData.DASH);

            MovePlayer(movementDirection);
            Dash(dashPressed, Runner.DeltaTime);
        }
    }

    private void MovePlayer(Vector2 movementDirection)
    {
        _rb.linearVelocity = movementDirection * _moveSpeed;
    }

    private void Dash(bool dashPressed, float deltaTime)
    {
        if (_dashCurrentCooldown > 0) {
            _dashCurrentCooldown = Mathf.Max(0, _dashCurrentCooldown - deltaTime);
        }

        switch (_isDashing) {
            case true:
                // If we are dashing then we simply add to the duration and check if we should end the dash
                _dashCurrentDuration += deltaTime;
                if (_dashCurrentDuration >= _stats.DashTotalDuration) {
                    EndDash();
                }
                break;
            case false:
                if (dashPressed) {
                    if (_dashCurrentCooldown <= 0) {
                    _moveSpeed = _stats.MoveSpeed * _stats.DashSpeedMultiplier;
                        _dashCurrentDuration = 0;
                        _isDashing = true;
                    }
                }
                break;
        }
    }
    
    private void EndDash() {
        _moveSpeed = _stats.MoveSpeed;
        _isDashing = false;
        _dashCurrentCooldown = _stats.DashTotalCooldown;
    }
}
