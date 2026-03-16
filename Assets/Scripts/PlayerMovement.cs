using System;
using Fusion;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    // ===== Networked Properties =====
    [Networked] private float _moveSpeed { get; set; } = 5f;

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
    }
    public override void Render()
    {

    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            Vector2 movementDirection = data.movementDirection.normalized;

            MovePlayer(movementDirection);
        }
    }

    private void MovePlayer(Vector2 movementDirection)
    {
        _rb.linearVelocity = movementDirection * _moveSpeed;
    }
}
