using Fusion;
using Pathfinding;
using UnityEngine;

public class EnemyPathfinding : NetworkBehaviour
{
    // ===== Serialized Fields =====
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 2.5f;

    [Header("Pathfinding")]
    [SerializeField] private float _pathRecalcInterval = 0.5f;
    [SerializeField] private float _nextWaypointDistance = 0.5f;

    // ===== Public Properties =====
    public bool FacingLeft => _facingLeft;

    // ===== Private Variables =====
    private Rigidbody2D _rb;
    private Seeker _seeker;

    private MovementState _state;
    private Vector2 _targetPosition;
    private bool _facingLeft;

    private Pathfinding.Path _currentPath;
    private int _currentWaypoint;
    private float _pathRecalcTimer;

    private enum MovementState { Idle, Following, Paused }

    // ===== Lifecycle =====

    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
        _seeker = GetComponent<Seeker>();
    }

    public override void FixedUpdateNetwork() {
        if (!HasStateAuthority) return;

        if (_state != MovementState.Following)
        {
            if (_state == MovementState.Paused)
                SetVelocity(Vector2.zero);

            return;
        }

        TickPathMovement();
    }

    // ===== Public API =====

    public void SetTargetPosition(Vector2 targetPosition) {
        _targetPosition = targetPosition;

        if (_state == MovementState.Following)
            return;

        _state = MovementState.Following;
    }

    public void SetSpeed(float speed) {
        _moveSpeed = speed;
    }

    public void StopMovement() {
        _state = MovementState.Idle;
        _currentPath = null;
        _pathRecalcTimer = 0f;
        SetVelocity(Vector2.zero);
    }

    public void ResumeMovement() {
        _state = MovementState.Following;
    }

    public void SetPaused(bool paused) {
        if (paused && _state == MovementState.Following)
            _state = MovementState.Paused;
        else if (!paused && _state == MovementState.Paused)
            _state = MovementState.Following;
    }

    // ===== A* Path Following =====

    private void TickPathMovement() {
        RecalculatePathIfNeeded();
        FollowCurrentPath();
    }

    private void RecalculatePathIfNeeded() {
        _pathRecalcTimer -= Runner.DeltaTime;
        if (_pathRecalcTimer > 0f) return;
        if (!_seeker.IsDone()) return;

        _pathRecalcTimer = _pathRecalcInterval;

        Vector2 endPos = SnapToWalkableNode(_targetPosition);
        _seeker.StartPath(transform.position, endPos, OnPathComplete);
    }

    private void OnPathComplete(Pathfinding.Path p) {
        if (p.error) return;

        _currentPath = p;
        _currentWaypoint = 0;
    }

    private void FollowCurrentPath() {
        if (_currentPath == null) return;

        AdvancePastReachedWaypoints();

        if (_currentWaypoint >= _currentPath.vectorPath.Count)
        {
            SetVelocity(Vector2.zero);
            return;
        }

        Vector2 direction = (Vector2)_currentPath.vectorPath[_currentWaypoint] - (Vector2)transform.position;
        SetVelocity(direction.normalized * _moveSpeed);
        UpdateFacing(direction);
    }

    private void AdvancePastReachedWaypoints() {
        while (_currentWaypoint < _currentPath.vectorPath.Count)
        {
            float distance = Vector2.Distance(transform.position, _currentPath.vectorPath[_currentWaypoint]);
            if (distance >= _nextWaypointDistance) break;
            _currentWaypoint++;
        }
    }

    // ===== Helpers =====

    private Vector2 SnapToWalkableNode(Vector2 position) {
        if (AstarPath.active == null) return position;

        var nearest = AstarPath.active.GetNearest(position, NNConstraint.Default);
        return nearest.node != null ? (Vector3)nearest.node.position : position;
    }

    private void SetVelocity(Vector2 velocity) {
        _rb.linearVelocity = velocity;
    }

    private void UpdateFacing(Vector2 direction) {
        if (Mathf.Abs(direction.x) > 0.01f)
            _facingLeft = direction.x < 0f;
    }
}
