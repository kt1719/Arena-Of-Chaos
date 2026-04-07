using System;
using Fusion;
using UnityEngine;

public class EnemyPathfinding : NetworkBehaviour
{
    [SerializeField] private float movementSpeed = 1f;

    private Vector2 currentRoamPosition;
    private Rigidbody2D rb;
    private Knockback knockback;
    public bool FacingLeft { get { return facingLeft; } }
    private bool facingLeft = false;

    private enum EnemyState {
        Moving,
        KnockedBack
    }

    private EnemyState state;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        TryGetComponent(out knockback);
        state = EnemyState.Moving;
    }

    public override void FixedUpdateNetwork()
    {
        UpdateState();
        MoveToPosition();
    }

    private void UpdateState() {
        if (knockback.IsKnockedBack) {
            state = EnemyState.KnockedBack;
            return;
        }

        state = EnemyState.Moving;
    }

    private void MoveToPosition()
    {
        bool isMoving = currentRoamPosition == null || state != EnemyState.Moving;
        if (isMoving) return;

        CalculateVelocity();
        CheckFacingLeft();
    }

    private void CheckFacingLeft()
    {
        if (currentRoamPosition.x < 0)
        {
            facingLeft = true;
        }
        else
        {
            facingLeft = false;
        }
    }

    private void CalculateVelocity()
    {
        Vector2 direction = (currentRoamPosition - (Vector2)transform.position);
        float magnitude = movementSpeed > direction.magnitude ? direction.magnitude : movementSpeed;
        Vector2 velocity = magnitude * direction.normalized;
        
        float magnitudeMinBound = 0.01f;
        rb.linearVelocity = (velocity.magnitude <= magnitudeMinBound) ? Vector2.zero : velocity;
    }

    public void SetRoamPosition(Vector2 roamPosition)
    {
        currentRoamPosition = roamPosition;
    }
}
