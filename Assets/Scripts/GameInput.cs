using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance;

    public event Action OnPlayerAttack;
    public event Action OnPlayerCancelAttack;
    public event Action OnPlayerDash;
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
    }

    // private void Inventory_Performed(InputAction.CallbackContext context, int pressedNum)
    // {
    //     OnPlayerInventory?.Invoke(new OnPlayerInventoryArgs
    //     {
    //         pressedButton = pressedNum
    //     });
    // }

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

    private void OnDestroy()
    {
        playerInputActions.Disable();
        playerInputActions.Dispose();
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
        return Camera.main.ScreenToWorldPoint(mouseScreenPos);
    }
}
