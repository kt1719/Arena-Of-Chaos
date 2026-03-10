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

    private int _currentDashTick;
    private Vector3 _networkPosition;

    private void Start() => GameInput.Instance.OnPlayerDash += OnDash;
    private void OnDestroy() => GameInput.Instance.OnPlayerDash -= OnDash;

    private void OnDash()
    {
        if (IsDashing) return;
        IsDashing = true;
        _currentDashTick = 0;
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

        if (playerController.PlayerState is PlayerState.Move or PlayerState.Attack)
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