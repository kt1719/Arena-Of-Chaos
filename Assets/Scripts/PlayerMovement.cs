using System;
using Fusion;
using Unity.VisualScripting;
using UnityEditor.Toolbars;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    // ===== Networked Properties =====
    [Networked] private float _moveSpeed { get; set; }
    [Networked] private float _dashCurrentDuration { get; set; }
    [Networked] private float _dashCurrentCooldown { get; set; }
    [Networked] private bool _isDashing { get; set; }

    // ===== Serialized Fields =====
    // Will most likely need to be networked in the future when I need to change values on runtime.
    [SerializeField] private float _originalMoveSpeed = 5f;
    [SerializeField] private float _dashSpeedMultiplier = 4f;
    [SerializeField] private float _dashTotalDuration = .5f;
    [SerializeField] private float _dashTotalCooldown = .1f;
    // ===== Events =====
    public event Action OnDashStart;
    public event Action OnDashEnd;
    
    // ===== Private Variables =====

    private Rigidbody2D _rb;
    private ChangeDetector _changeDetector;
    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
    }

    public override void Spawned()
    {
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

    public void Init() {
        _moveSpeed = _originalMoveSpeed;
        _dashCurrentDuration = 0;
        _dashCurrentCooldown = 0;
        _isDashing = false;
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            Vector2 movementDirection = data.movementDirection.normalized;
            bool dashPressed = data.buttons.IsSet(NetworkInputData.DASH_BUTTON);

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
                if (_dashCurrentDuration >= _dashTotalDuration) {
                    EndDash();
                }
                break;
            case false:
                if (dashPressed) {
                    if (_dashCurrentCooldown <= 0) {
                    _moveSpeed = _originalMoveSpeed * _dashSpeedMultiplier;
                        _dashCurrentDuration = 0;
                        _isDashing = true;
                    }
                }
                break;
        }
    }
    
    private void EndDash() {
        _moveSpeed = _originalMoveSpeed;
        _isDashing = false;
        _dashCurrentCooldown = _dashTotalCooldown;
    }
}
