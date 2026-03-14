using Fusion;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Networked Properties")]
    [Networked] private float _moveSpeed { get; set; } = 5f;

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
        MovePlayer(Runner.DeltaTime);
    }

    private void MovePlayer(float deltaTime)
    {
        if (GetInput(out NetworkInputData data))
        {
            _rb.MovePosition(_rb.position + data.movementDirection * _moveSpeed * deltaTime);
        }
    }
}
