using System;
using Photon.Pun;
using UnityEngine;

public class PlayerController : MonoBehaviourPunCallbacks
{
    public event Action<PlayerState> OnPlayerStateChanged;

    public PlayerState PlayerState { get; private set; }
    public bool FacingLeft { get; private set; }

    [SerializeField] private PlayerHittable _playerHittable;
    [SerializeField] private PlayerMovement _playerMovement;

    private void Start()
    {
        _playerHittable.OnHitReceived += OnHitReceived;
        _playerMovement.OnKnockbackEnded += OnKnockbackEnded;
    }

    private void OnDestroy()
    {
        _playerHittable.OnHitReceived -= OnHitReceived;
        _playerMovement.OnKnockbackEnded -= OnKnockbackEnded;
    }

    private void OnHitReceived(int damage, Vector3 sourcePosition) => SetState(PlayerState.Knockback);
    private void OnKnockbackEnded() => SetState(PlayerState.Idle);

    /// <summary>Set state from external events (e.g. knockback). Use for Knockback / Idle when hit starts and ends.</summary>
    public void SetState(PlayerState newState)
    {
        if (newState == PlayerState) return;
        OnPlayerStateChanged?.Invoke(newState);
        PlayerState = newState;
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        if (PlayerState == PlayerState.Knockback) return;

        PlayerControllerInput input = GetPlayerInput();
        CalculatePlayerState(input);
    }

    private PlayerControllerInput GetPlayerInput()
    {
        // Mouse Input
        Vector2 mouseWorldPos = GameInput.Instance.GetMouseInputWorldPos();
        Vector2 playerPos = transform.position;

        // Keyboard Input
        Vector2 movementInput = GameInput.Instance.GetMovementInput();

        // Metadata Calculation
        FacingLeft = mouseWorldPos.x < playerPos.x;

        // Return
        return new PlayerControllerInput
        {
            movementInput = movementInput,
        };
    }

    private void CalculatePlayerState(PlayerControllerInput input)
    {
        PlayerState newState;
        if (input.movementInput != Vector2.zero)
        {
            newState = PlayerState.Move;
        }
        else
        {
            newState = PlayerState.Idle;
        }


        if (newState != PlayerState)
        {
            OnPlayerStateChanged?.Invoke(newState);
            PlayerState = newState;
        }
    }
}

public struct PlayerControllerInput
{
    public Vector2 movementInput;
}