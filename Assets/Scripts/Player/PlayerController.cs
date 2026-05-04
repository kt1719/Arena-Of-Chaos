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
        _localInputData.inventorySlotPressed = NetworkInputData.NO_INVENTORY_PRESS;

        if (HasInputAuthority) {
            // This is OUR player on this client
            NetworkManager.Instance.RegisterLocalPlayer(this);

            GameInput.Instance.OnPlayerDash += DashPressed;
            GameInput.Instance.OnPlayerAttack += AttackPressed;
            GameInput.Instance.OnPlayerCancelAttack += AttackReleased;
            GameInput.Instance.OnPlayerInventory += InventoryPressed;
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
        GameInput.Instance.OnPlayerInventory -= InventoryPressed;
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

    private void InventoryPressed(GameInput.OnPlayerInventoryArgs e) {
        _localInputData.inventorySlotPressed = (byte)e.pressedButton;
    }

    // ConsumeInput is called by NetworkManager to clear the input data
    public PlayerControllerLocalInputData ConsumeInput() {
        if (!PlayerEnabled) {
            return new PlayerControllerLocalInputData { inventorySlotPressed = NetworkInputData.NO_INVENTORY_PRESS };
        }

        _localInputData.movementDirection = GameInput.Instance.GetMovementInput();
        _localInputData.weaponAimDirection = GameInput.Instance.GetWeaponAimDirection(transform);

        // First we store a snapshot of the local input data before clearing
        PlayerControllerLocalInputData localInputDataCopy = _localInputData;

        // Clear the local input data
        _localInputData.dashPressed = false;
        _localInputData.inventorySlotPressed = NetworkInputData.NO_INVENTORY_PRESS;

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
    public byte inventorySlotPressed;
}