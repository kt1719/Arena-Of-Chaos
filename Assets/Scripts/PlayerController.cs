using Fusion;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    public PlayerControllerLocalInputData LocalInputData { get; private set; }

    public override void Spawned() {
        if (HasInputAuthority) {
            // This is OUR player on this client
            NetworkManager.Instance.RegisterLocalPlayer(this);

            GameInput.Instance.OnPlayerDash += OnPlayerDash;
        }
    }
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        GameInput.Instance.OnPlayerDash -= OnPlayerDash;
    }

    private void OnPlayerDash() {
        var data = LocalInputData;
        data.isDashing = true;
        LocalInputData = data;
    }

    public override void Render() {
        var data = LocalInputData;
        data.movementDirection = GameInput.Instance.GetMovementInput();
        // Don't clear isDashing here — let OnInput consume it
        LocalInputData = data;
    }

    public void ConsumeInput() {
        var data = LocalInputData;
        data.isDashing = false;
        LocalInputData = data;
    }
}

public struct PlayerControllerLocalInputData {
    public Vector2 movementDirection;
    public bool isDashing;
}