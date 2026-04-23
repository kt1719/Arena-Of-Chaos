using Fusion;
using Pathfinding;
using UnityEngine;

public class EnemyAI : NetworkBehaviour
{
    // ===== Serialized Fields =====
    [Header("Scripts")]
    [SerializeField] private EnemyPathfinding _pathfinding;
    [SerializeField] private SlimeLunge _lunge;
    [SerializeField] private Knockback _knockback;

    [Header("Detection")]
    [SerializeField] private float _detectionRadius = 8f;
    [SerializeField] private float _lungeRange = 2.5f;
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private LayerMask _obstacleLayer;

    [Header("Roaming")]
    [SerializeField] private float _roamSpeed = 1f;
    [SerializeField] private float _roamChangeDirInterval = 2f;
    [SerializeField] private float _roamRadius = 3f;

    [Header("Chase")]
    [SerializeField] private float _chaseSpeed = 2.5f;
    [SerializeField] private float _loseInterestDuration = 3f;

    // ===== Constants =====
    private const int MAX_ROAM_ATTEMPTS = 5;

    private State _state;
    private State _stateBeforeKnockback;
    private Transform _targetPlayer;
    private float _roamTimer;
    private float _loseInterestTimer;

    private enum State { Roaming, Chasing, Lunging, KnockedBack }

    // ===== Lifecycle =====
    
    public override void Spawned() {
        if (!HasStateAuthority) return;

        _state = State.Roaming;
        _knockback.OnKnockbackEnd += OnKnockbackEnd;
        _lunge.OnLungeEnd += OnLungeEnd;
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        if (_knockback != null) _knockback.OnKnockbackEnd -= OnKnockbackEnd;
        if (_lunge != null) _lunge.OnLungeEnd -= OnLungeEnd;
    }

    public override void FixedUpdateNetwork() {
        if (!HasStateAuthority) return;

        switch (_state)
        {
            case State.Roaming: TickRoaming(); break;
            case State.Chasing: TickChasing(); break;
            case State.KnockedBack: TickKnockBack(); break;
        }
    }

    // ===== State: Roaming =====

    private void TickRoaming() {
        Transform player = FindNearestPlayer();
        if (player != null)
        {
            _targetPlayer = player;
            EnterChasing();
            return;
        }

        TickRoamMovement();
    }

    private void TickRoamMovement() {
        _roamTimer -= Runner.DeltaTime;
        if (_roamTimer > 0f) return;

        _roamTimer = _roamChangeDirInterval;
        _pathfinding.SetTargetPosition(GetWalkableRoamTarget());
    }

    private Vector2 GetWalkableRoamTarget() {
        Vector2 origin = transform.position;

        if (AstarPath.active == null)
            return origin + Random.insideUnitCircle.normalized * _roamRadius;

        for (int i = 0; i < MAX_ROAM_ATTEMPTS; i++)
        {
            Vector2 candidate = origin + Random.insideUnitCircle.normalized * _roamRadius;
            var nearest = AstarPath.active.GetNearest(candidate, NNConstraint.Default);

            if (nearest.node != null && nearest.node.Walkable)
                return (Vector3)nearest.node.position;
        }

        return origin;
    }

    // ===== State: Chasing =====

    private void TickChasing() {
        if (!IsTargetValid())
        {
            EnterRoaming();
            return;
        }

        float distance = GetDistanceToTarget();

        if (distance <= _lungeRange && _lunge.CanLunge && HasLineOfSight(_targetPlayer))
        {
            EnterLunging();
            return;
        }

        if (TickLoseInterest(distance)) return;

        _pathfinding.SetTargetPosition(_targetPlayer.position);
        _pathfinding.SetPaused(_lunge.IsOnCooldown);
    }

    private bool TickLoseInterest(float distanceToPlayer) {
        if (distanceToPlayer <= _detectionRadius)
        {
            _loseInterestTimer = _loseInterestDuration;
            return false;
        }

        _loseInterestTimer -= Runner.DeltaTime;
        if (_loseInterestTimer > 0f) return false;

        EnterRoaming();
        return true;
    }

    // ===== State: KnockedBack =====

    private void TickKnockBack() {
        if (_knockback == null || !_knockback.IsKnockedBack) return;

        _stateBeforeKnockback = _state;
        _state = State.KnockedBack;
        _pathfinding.StopMovement();
    }

    // ===== State Transitions =====

    private void EnterRoaming() {
        _state = State.Roaming;
        _targetPlayer = null;
        _roamTimer = 0f;
        _pathfinding.SetPaused(false);
        _pathfinding.SetSpeed(_roamSpeed);
        _pathfinding.ResumeMovement();
    }

    private void EnterChasing() {
        _state = State.Chasing;
        _loseInterestTimer = _loseInterestDuration;
        _pathfinding.SetPaused(false);
        _pathfinding.SetSpeed(_chaseSpeed);
        _pathfinding.ResumeMovement();
        _pathfinding.SetTargetPosition(_targetPlayer.position);
    }

    private void EnterLunging() {
        _state = State.Lunging;
        _pathfinding.StopMovement();
        _lunge.StartLunge(_targetPlayer.position);
    }

    // ===== Event Handlers =====

    private void OnKnockbackEnd() {
        _pathfinding.ResumeMovement();

        if (_stateBeforeKnockback == State.Chasing && IsTargetValid())
            EnterChasing();
        else
            EnterRoaming();
    }

    private void OnLungeEnd() {
        if (IsTargetValid() && GetDistanceToTarget() <= _detectionRadius)
            EnterChasing();
        else
            EnterRoaming();
    }

    // ===== Helpers =====

    private Transform FindNearestPlayer() {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _detectionRadius, _playerLayer);

        Transform nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (hit.transform.root == transform.root) continue;
            if (hit.GetComponent<IHittable>() == null) continue;
            if (!HasLineOfSight(hit.transform)) continue;

            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = hit.transform;
            }
        }

        return nearest;
    }

    private bool IsTargetValid() {
        return _targetPlayer != null && _targetPlayer.gameObject.activeInHierarchy;
    }

    private bool HasLineOfSight(Transform target) {
        Vector2 origin = transform.position;
        Vector2 direction = (Vector2)target.position - origin;

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction.normalized, direction.magnitude, _obstacleLayer);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.isTrigger) continue;
            if (hit.transform.root == transform.root) continue;
            if (hit.transform.root == target.root) continue;

            return false;
        }

        return true;
    }

    private float GetDistanceToTarget() {
        return Vector2.Distance(transform.position, _targetPlayer.position);
    }

    // ===== Editor =====

#if UNITY_EDITOR
    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _lungeRange);
    }
#endif
}
