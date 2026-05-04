using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance;

    public event Action OnPlayerAttack;
    public event Action OnPlayerCancelAttack;
    public event Action OnPlayerDash;
    public event Action OnScoreboardPressed;
    public event Action OnScoreboardReleased;
    public event Action<OnPlayerInventoryArgs> OnPlayerInventory;

    public class OnPlayerInventoryArgs : EventArgs
    {
        public int pressedButton;
    }

    public class OnPlayerMoveArgs: EventArgs
    {
        public Vector2 inputDirectionNormalised;
    }

    private PlayerInputActions playerInputActions;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        playerInputActions = new PlayerInputActions();
        playerInputActions.Enable();

        playerInputActions.Player.Attack.performed += Attack_Performed;
        playerInputActions.Player.Attack.canceled += Attack_Released;
        playerInputActions.Player.Dash.performed += Dash_Performed;
        playerInputActions.Player.Scoreboard.performed += Scoreboard_Performed;
        playerInputActions.Player.Scoreboard.canceled += Scoreboard_Released;

        playerInputActions.Inventory.Slot1.performed += ctx => Inventory_Performed(ctx, 0);
        playerInputActions.Inventory.Slot2.performed += ctx => Inventory_Performed(ctx, 1);
        playerInputActions.Inventory.Slot3.performed += ctx => Inventory_Performed(ctx, 2);
        playerInputActions.Inventory.Slot4.performed += ctx => Inventory_Performed(ctx, 3);
        playerInputActions.Inventory.Slot5.performed += ctx => Inventory_Performed(ctx, 4);
    }

    private void Inventory_Performed(InputAction.CallbackContext _, int pressedNum)
    {
        OnPlayerInventory?.Invoke(new OnPlayerInventoryArgs
        {
            pressedButton = pressedNum
        });
    }

    private void Dash_Performed(InputAction.CallbackContext context)
    {
        OnPlayerDash?.Invoke();
    }

    private void Attack_Performed(InputAction.CallbackContext context)
    {
        OnPlayerAttack?.Invoke();
    }
    private void Attack_Released(InputAction.CallbackContext context)
    {
        OnPlayerCancelAttack?.Invoke();
    }

    private void Scoreboard_Performed(InputAction.CallbackContext context)
    {
        OnScoreboardPressed?.Invoke();
    }

    private void Scoreboard_Released(InputAction.CallbackContext context)
    {
        OnScoreboardReleased?.Invoke();
    }

    private void OnDestroy()
    {
        if (playerInputActions != null)
        {
            playerInputActions.Disable();
            playerInputActions.Dispose();
        }
        if (Instance == this) Instance = null;
    }

    public Vector2 GetMovementInput()
    {
        return playerInputActions.Player.Move.ReadValue<Vector2>().normalized;
    }

    public Vector2 GetWeaponAimDirection(Transform playerTransform)
    {
        return (GetMouseInputWorldPos() - (Vector2)playerTransform.position).normalized;
    }

    private Vector2 GetMouseInputWorldPos()
    {
        Vector2 mouseScreenPos = Input.mousePosition;
        return GameManager.Instance.CurrentActiveCamera.ScreenToWorldPoint(mouseScreenPos);
    }
}
