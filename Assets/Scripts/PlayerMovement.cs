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
    [Networked] private bool _isDebugOrbitActive { get; set; }
    [Networked] private float _debugOrbitAngleRad { get; set; }

    // ===== Serialized Fields =====
    // Will most likely need to be networked in the future when I need to change values on runtime.
    [SerializeField] private float _originalMoveSpeed = 5f;
    [SerializeField] private float _dashSpeedMultiplier = 4f;
    [SerializeField] private float _dashTotalDuration = .5f;
    [SerializeField] private float _dashTotalCooldown = .1f;
    [SerializeField] private float _debugOrbitAngularSpeedDegrees = 40f;
    [SerializeField] private float _debugOrbitCorrectionSpeed = 10f;
    // ===== Events =====
    public event Action OnDashStart;
    public event Action OnDashEnd;
    public bool IsDebugOrbitActive => _isDebugOrbitActive;
    
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

    private void Init() {
        _moveSpeed = _originalMoveSpeed;
        _dashCurrentDuration = 0;
        _dashCurrentCooldown = 0;
        _isDashing = false;
        _isDebugOrbitActive = false;
        _debugOrbitAngleRad = 0f;
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            Vector2 movementDirection = data.movementDirection.normalized;
            bool dashPressed = data.buttons.IsSet(NetworkInputData.DASH);
            bool debugOrbitEnabled = data.buttons.IsSet(NetworkInputData.DEBUG_ORBIT);

            if (debugOrbitEnabled && data.debugOrbitRadius > 0.001f)
            {
                UpdateDebugOrbit(data.debugOrbitCenter, data.debugOrbitRadius, Runner.DeltaTime);
                return;
            }

            if (_isDebugOrbitActive)
            {
                _isDebugOrbitActive = false;
                _rb.linearVelocity = Vector2.zero;
            }

            MovePlayer(movementDirection);
            Dash(dashPressed, Runner.DeltaTime);
        }
    }

    private void UpdateDebugOrbit(Vector2 orbitCenter, float orbitRadius, float deltaTime)
    {
        if (!_isDebugOrbitActive)
        {
            _isDebugOrbitActive = true;
            if (_isDashing)
            {
                EndDash();
            }

            Vector2 startOffset = _rb.position - orbitCenter;
            _debugOrbitAngleRad = startOffset.sqrMagnitude > 0.0001f
                ? Mathf.Atan2(startOffset.y, startOffset.x)
                : 0f;
        }

        float angularSpeedRad = _debugOrbitAngularSpeedDegrees * Mathf.Deg2Rad;
        _debugOrbitAngleRad = Mathf.Repeat(_debugOrbitAngleRad + angularSpeedRad * deltaTime, Mathf.PI * 2f);

        Vector2 desiredUnit = new Vector2(Mathf.Cos(_debugOrbitAngleRad), Mathf.Sin(_debugOrbitAngleRad));
        Vector2 desiredOffset = desiredUnit * orbitRadius;
        Vector2 currentOffset = _rb.position - orbitCenter;
        Vector2 radialCorrection = desiredOffset - currentOffset;

        Vector2 tangent = new Vector2(-desiredUnit.y, desiredUnit.x);
        float tangentialSpeed = angularSpeedRad * orbitRadius;

        _rb.linearVelocity = tangent * tangentialSpeed + radialCorrection * _debugOrbitCorrectionSpeed;
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
