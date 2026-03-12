using System;
using Photon.Pun;
using UnityEngine;

public class PlayerMovement : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float dashSpeedMultiplier = 5f;
    [SerializeField] private int dashTicks = 20;
    [SerializeField] private float regularInterpolationSpeed = 5f;
    [SerializeField] private float dashInterpolationSpeed = 15f;

    public bool IsDashing { get; private set; }

    /// <summary>Invoked when knockback timer expires (owner only). Subscribe to clear Knockback state.</summary>
    public Action OnKnockbackEnded;

    public bool IsKnockbackActive => _knockbackTimeLeft > 0f;

    private int _currentDashTick;
    private Vector3 _networkPosition;
    private Vector2 _knockbackVelocity;
    private float _knockbackTimeLeft;

    private void Start() => GameInput.Instance.OnPlayerDash += OnDash;
    private void OnDestroy() => GameInput.Instance.OnPlayerDash -= OnDash;

    private void OnDash()
    {
        if (IsDashing) return;
        IsDashing = true;
        _currentDashTick = 0;
    }

    /// <summary>Apply knockback (call from PlayerHittable RPC when IsMine). Direction away from damage source.</summary>
    public void SetKnockback(Vector2 direction, float force, float duration)
    {
        if (duration <= 0f) return;
        float speed = force / duration;
        _knockbackVelocity = direction.normalized * speed;
        _knockbackTimeLeft = duration;
    }

    private void FixedUpdate()
    {
        if (photonView.IsMine)
            LocalUpdate();
        else
            RemoteUpdate();
    }

    // ── Local ────────────────────────────────────────────────────────────────

    private void LocalUpdate()
    {
        if (_knockbackTimeLeft > 0f)
        {
            float delta = Mathf.Min(Time.fixedDeltaTime, _knockbackTimeLeft);
            transform.position += (Vector3)(_knockbackVelocity * delta);
            _knockbackTimeLeft -= delta;
            if (_knockbackTimeLeft <= 0f)
            {
                _knockbackTimeLeft = 0f;
                OnKnockbackEnded?.Invoke();
            }
            return;
        }

        TickDash();
        Move();
    }

    private void TickDash()
    {
        if (!IsDashing) return;
        if (++_currentDashTick >= dashTicks)
            IsDashing = false;
    }

    private void Move()
    {
        if (IsDashing)
        {
            ApplyMovement(dashSpeedMultiplier);
            return;
        }

        if (playerController.PlayerState is PlayerState.Move or PlayerState.Attack && !IsKnockbackActive)
            ApplyMovement();
    }

    private void ApplyMovement(float speedMultiplier = 1f)
    {
        Vector2 input = GameInput.Instance.GetMovementInput();
        transform.position += (Vector3)(input * moveSpeed * speedMultiplier * Time.fixedDeltaTime);
    }

    // ── Remote ───────────────────────────────────────────────────────────────

    private void RemoteUpdate()
    {
        float interpolationSpeed = IsDashing ? dashInterpolationSpeed : regularInterpolationSpeed;

        transform.position = Vector3.Lerp(transform.position, _networkPosition, interpolationSpeed * Time.fixedDeltaTime);
    }

    // ── Photon ───────────────────────────────────────────────────────────────

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(IsDashing);
            return;
        }
        else if (stream.IsReading)
        {
            _networkPosition = (Vector3)stream.ReceiveNext(); // Do not immediately update since we will interpolate
            IsDashing = (bool)stream.ReceiveNext();
        }
    }
}