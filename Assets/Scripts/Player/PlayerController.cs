using Fusion;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    private PlayerControllerLocalInputData _localInputData;

    public override void Spawned() {
        if (HasInputAuthority) {
            // This is OUR player on this client
            NetworkManager.Instance.RegisterLocalPlayer(this);

            GameInput.Instance.OnPlayerDash += DashPressed;
            GameInput.Instance.OnPlayerAttack += AttackPressed;
            GameInput.Instance.OnPlayerCancelAttack += AttackReleased;
        }
    }
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        GameInput.Instance.OnPlayerDash -= DashPressed;
        GameInput.Instance.OnPlayerAttack -= AttackPressed;
        GameInput.Instance.OnPlayerCancelAttack -= AttackReleased;
    }

    private void DashPressed() {
        _localInputData.dashPressed = true;
    }

    private void AttackPressed() {
        _localInputData.attackPressed = true;
    }

    private void AttackReleased() {
        _localInputData.attackPressed = false;
    }

    // ConsumeInput is called by NetworkManager to clear the input data
    public PlayerControllerLocalInputData ConsumeInput() {
        _localInputData.movementDirection = GameInput.Instance.GetMovementInput();
        _localInputData.weaponAimDirection = GameInput.Instance.GetWeaponAimDirection(transform);
        
        // First we store a snapshot of the local input data before clearing
        PlayerControllerLocalInputData localInputDataCopy = _localInputData;

        // Clear the local input data
        _localInputData.dashPressed = false;

        // Return the snapshot
        return localInputDataCopy;
    }
}

public struct PlayerControllerLocalInputData {
    public Vector2 movementDirection;
    public Vector2 weaponAimDirection;
    public bool dashPressed;
    public bool attackPressed;
}