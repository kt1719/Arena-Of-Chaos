using System;
using Photon.Pun;
using UnityEngine;

public class PlayerController : MonoBehaviourPunCallbacks
{
    public event Action<PlayerState> OnPlayerStateChanged;

    public PlayerState PlayerState { get; private set; }
    public bool FacingLeft { get; private set; }

    private void Update()
    {
        if (!photonView.IsMine) return;

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