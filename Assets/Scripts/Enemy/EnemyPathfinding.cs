using Fusion;
using Pathfinding;
using UnityEngine;

/// <summary>
/// Networked A* pathfinding for enemies.
/// Recalculates paths on a timer and follows waypoints via Rigidbody2D velocity.
/// </summary>
public class EnemyPathfinding : NetworkBehaviour
{
    // ── Tuning ──
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 2.5f;

    [Header("Pathfinding")]
    [SerializeField] private float _pathRecalcInterval = 0.5f;
    [SerializeField] private float _nextWaypointDistance = 0.5f;

    // ── Public State ──
    public bool FacingLeft => _facingLeft;

    // ── Components ──
    private Rigidbody2D _rb;
    private Seeker _seeker;

    // ── Runtime State ──
    private MoveState _state;
    private Vector2 _targetPosition;
    private bool _facingLeft;

    // ── Path State ──
    private Pathfinding.Path _currentPath;
    private int _waypointIndex;
    private float _recalcTimer;

    private enum MoveState { Idle, Following, Paused }

    // ════════════════════════════════════════
    //  Lifecycle
    // ════════════════════════════════════════

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _seeker = GetComponent<Seeker>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        switch (_state)
        {
            case MoveState.Paused:
                SetVelocity(Vector2.zero);
                return;

            case MoveState.Following:
                TickPathMovement();
                return;

            // Idle — do nothing.
        }
    }

    // ════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════

    public void SetTargetPosition(Vector2 target)
    {
        _targetPosition = target;
        if (_state != MoveState.Following)
            _state = MoveState.Following;
    }

    public void SetSpeed(float speed) => _moveSpeed = speed;

    public void StopMovement()
    {
        _state = MoveState.Idle;
        _currentPath = null;
        _recalcTimer = 0f;
        SetVelocity(Vector2.zero);
    }

    public void ResumeMovement() => _state = MoveState.Following;

    public void SetPaused(bool paused)
    {
        if (paused && _state == MoveState.Following)
            _state = MoveState.Paused;
        else if (!paused && _state == MoveState.Paused)
            _state = MoveState.Following;
    }

    // ════════════════════════════════════════
    //  Path Following
    // ════════════════════════════════════════

    private void TickPathMovement()
    {
        RecalculatePathIfNeeded();
        FollowPath();
    }

    private void RecalculatePathIfNeeded()
    {
        _recalcTimer -= Runner.DeltaTime;
        if (_recalcTimer > 0f || !_seeker.IsDone()) return;

        _recalcTimer = _pathRecalcInterval;
        Vector2 destination = SnapToWalkableNode(_targetPosition);
        _seeker.StartPath(transform.position, destination, OnPathComplete);
    }

    private void OnPathComplete(Pathfinding.Path path)
    {
        if (path.error) return;
        _currentPath = path;
        _waypointIndex = 0;
    }

    private void FollowPath()
    {
        if (_currentPath == null) return;

        AdvancePastReachedWaypoints();

        if (_waypointIndex >= _currentPath.vectorPath.Count)
        {
            SetVelocity(Vector2.zero);
            return;
        }

        Vector2 direction = (Vector2)_currentPath.vectorPath[_waypointIndex] - (Vector2)transform.position;
        SetVelocity(direction.normalized * _moveSpeed);
        UpdateFacing(direction);
    }

    private void AdvancePastReachedWaypoints()
    {
        while (_waypointIndex < _currentPath.vectorPath.Count)
        {
            float dist = Vector2.Distance(transform.position, _currentPath.vectorPath[_waypointIndex]);
            if (dist >= _nextWaypointDistance) break;
            _waypointIndex++;
        }
    }

    // ════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════

    private Vector2 SnapToWalkableNode(Vector2 position)
    {
        if (AstarPath.active == null) return position;

        var nearest = AstarPath.active.GetNearest(position, NNConstraint.Default);
        return nearest.node != null ? (Vector3)nearest.node.position : position;
    }

    private void SetVelocity(Vector2 velocity) => _rb.linearVelocity = velocity;

    private void UpdateFacing(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > 0.01f)
            _facingLeft = direction.x < 0f;
    }
}
