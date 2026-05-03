using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    // ===== Networked Components =====
    [Networked] private bool PlayerEnabled { get; set;} = false; // Initialised as false

    // ===== Serialized Fields =====
    [SerializeField] Transform playerVisuals;

    // ===== Private Variables =====
    private PlayerControllerLocalInputData _localInputData;
    private ChangeDetector _changeDetector;

    public override void Spawned() {
        if (HasInputAuthority) {
            // This is OUR player on this client
            NetworkManager.Instance.RegisterLocalPlayer(this);

            GameInput.Instance.OnPlayerDash += DashPressed;
            GameInput.Instance.OnPlayerAttack += AttackPressed;
            GameInput.Instance.OnPlayerCancelAttack += AttackReleased;
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        playerVisuals.gameObject.SetActive(false);
    }
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority)
        {
            NetworkManager.Instance.UnregisterLocalPlayer();
        }
        GameInput.Instance.OnPlayerDash -= DashPressed;
        GameInput.Instance.OnPlayerAttack -= AttackPressed;
        GameInput.Instance.OnPlayerCancelAttack -= AttackReleased;
    }

    public override void Render() {
        if (_changeDetector == null) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(PlayerEnabled))
                playerVisuals.gameObject.SetActive(PlayerEnabled);
        }
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
        if (!PlayerEnabled) {return default;} // If it is disabled do not allow for the player to move;

        _localInputData.movementDirection = GameInput.Instance.GetMovementInput();
        _localInputData.weaponAimDirection = GameInput.Instance.GetWeaponAimDirection(transform);
        
        // First we store a snapshot of the local input data before clearing
        PlayerControllerLocalInputData localInputDataCopy = _localInputData;

        // Clear the local input data
        _localInputData.dashPressed = false;

        // Return the snapshot
        return localInputDataCopy;
    }

    public void ChangePlayerEnable(bool active) {
        PlayerEnabled = active;
    }
}

public struct PlayerControllerLocalInputData {
    public Vector2 movementDirection;
    public Vector2 weaponAimDirection;
    public bool dashPressed;
    public bool attackPressed;
}