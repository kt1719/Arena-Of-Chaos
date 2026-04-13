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
    private Knockback _knockback;
    private Seeker _seeker;

    private Vector2 _targetPosition;
    private bool _hasTarget;
    private bool _movementStopped;
    private bool _paused;
    private bool _facingLeft;

    private Pathfinding.Path _currentPath;
    private int _currentWaypoint;
    private float _pathRecalcTimer;

    // ===== Lifecycle =====

    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
        TryGetComponent(out _knockback);
        _seeker = GetComponent<Seeker>();
    }

    public override void FixedUpdateNetwork() {
        if (!HasStateAuthority) return;
        if (IsKnockedBack()) return;

        if (_movementStopped || _paused)
        {
            if (_paused) SetVelocity(Vector2.zero);
            return;
        }

        TickPathMovement();
    }

    // ===== Public API =====

    public void SetTargetPosition(Vector2 targetPosition) {
        _targetPosition = targetPosition;
        _hasTarget = true;
    }

    public void SetSpeed(float speed) {
        _moveSpeed = speed;
    }

    public void StopMovement() {
        _movementStopped = true;
        _hasTarget = false;
        _currentPath = null;
        _pathRecalcTimer = 0f;
        SetVelocity(Vector2.zero);
    }

    public void ResumeMovement() {
        _movementStopped = false;
    }

    public void SetPaused(bool paused) {
        _paused = paused;
    }

    // ===== A* Path Following =====

    private void TickPathMovement() {
        RecalculatePathIfNeeded();
        FollowCurrentPath();
    }

    private void RecalculatePathIfNeeded() {
        if (!_hasTarget) return;

        _pathRecalcTimer -= Runner.DeltaTime;
        if (_pathRecalcTimer > 0f) return;
        if (_seeker != null && !_seeker.IsDone()) return;

        _pathRecalcTimer = _pathRecalcInterval;

        // Snap target to nearest walkable node to avoid "couldn't find node" errors
        Vector2 endPos = _targetPosition;
        if (AstarPath.active != null)
        {
            var nearest = AstarPath.active.GetNearest(endPos, NNConstraint.Default);
            if (nearest.node != null)
                endPos = (Vector3)nearest.node.position;
        }

        _seeker.StartPath(transform.position, endPos, OnPathComplete);
    }

    private void OnPathComplete(Pathfinding.Path p) {
        if (p.error) return;

        _currentPath = p;
        _currentWaypoint = 0;
    }

    private void FollowCurrentPath() {
        if (_currentPath == null) return;

        // Skip past waypoints we're already close to
        while (_currentWaypoint < _currentPath.vectorPath.Count)
        {
            Vector2 toWaypoint = (Vector2)_currentPath.vectorPath[_currentWaypoint] - (Vector2)transform.position;
            if (toWaypoint.magnitude >= _nextWaypointDistance) break;
            _currentWaypoint++;
        }

        if (_currentWaypoint >= _currentPath.vectorPath.Count)
        {
            SetVelocity(Vector2.zero);
            return;
        }

        Vector2 direction = (Vector2)_currentPath.vectorPath[_currentWaypoint] - (Vector2)transform.position;
        SetVelocity(direction.normalized * _moveSpeed);
        UpdateFacing(direction);
    }

    // ===== Helpers =====

    private bool IsKnockedBack() {
        return _knockback != null && _knockback.IsKnockedBack;
    }

    private void SetVelocity(Vector2 velocity) {
        _rb.linearVelocity = velocity;
    }

    private void UpdateFacing(Vector2 direction) {
        if (Mathf.Abs(direction.x) > 0.01f)
            _facingLeft = direction.x < 0f;
    }
}
