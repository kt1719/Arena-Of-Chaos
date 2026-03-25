using Fusion;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField, Range(0.05f, 0.45f)] private float _debugOrbitViewportRadius = 0.16f;

    private PlayerControllerLocalInputData _localInputData;

    public override void Spawned() {
        if (HasInputAuthority) {
            // This is OUR player on this client
            NetworkManager.Instance.RegisterLocalPlayer(this);

            GameInput.Instance.OnPlayerDash += DashPressed;
            GameInput.Instance.OnPlayerAttack += AttackPressed;
            GameInput.Instance.OnPlayerCancelAttack += AttackReleased;

            DebugOrbitToggleUI debugOrbitToggleUI = DebugOrbitToggleUI.EnsureExists();
            SetDebugOrbitEnabled(debugOrbitToggleUI.IsEnabled);
            debugOrbitToggleUI.OnToggleChanged += SetDebugOrbitEnabled;
        }
    }
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority)
        {
            GameInput.Instance.OnPlayerDash -= DashPressed;
            GameInput.Instance.OnPlayerAttack -= AttackPressed;
            GameInput.Instance.OnPlayerCancelAttack -= AttackReleased;

            if (DebugOrbitToggleUI.TryGetInstance(out DebugOrbitToggleUI debugOrbitToggleUI))
            {
                debugOrbitToggleUI.OnToggleChanged -= SetDebugOrbitEnabled;
            }
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

    public void SetDebugOrbitEnabled(bool isEnabled)
    {
        _localInputData.debugOrbitEnabled = isEnabled;
    }

    // ConsumeInput is called by NetworkManager to clear the input data
    public PlayerControllerLocalInputData ConsumeInput() {
        _localInputData.movementDirection = GameInput.Instance.GetMovementInput();
        _localInputData.weaponAimDirection = GameInput.Instance.GetWeaponAimDirection(transform);
        if (_localInputData.debugOrbitEnabled && TryGetDebugOrbitData(out Vector2 orbitCenter, out float orbitRadius))
        {
            _localInputData.debugOrbitCenter = orbitCenter;
            _localInputData.debugOrbitRadius = orbitRadius;
        }
        else
        {
            _localInputData.debugOrbitCenter = Vector2.zero;
            _localInputData.debugOrbitRadius = 0f;
        }
        
        // First we store a snapshot of the local input data before clearing
        PlayerControllerLocalInputData localInputDataCopy = _localInputData;

        // Clear the local input data
        _localInputData.dashPressed = false;
        _localInputData.debugOrbitEnabled = localInputDataCopy.debugOrbitEnabled;

        // Return the snapshot
        return localInputDataCopy;
    }

    private bool TryGetDebugOrbitData(out Vector2 orbitCenter, out float orbitRadius)
    {
        Camera cameraRef = Camera.main;
        if (cameraRef == null)
        {
            cameraRef = FindFirstObjectByType<Camera>();
        }

        if (cameraRef == null)
        {
            orbitCenter = Vector2.zero;
            orbitRadius = 0f;
            return false;
        }

        float viewDepth = cameraRef.orthographic
            ? 0f
            : Mathf.Abs(transform.position.z - cameraRef.transform.position.z);
        Vector3 centerWorld = cameraRef.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, viewDepth));
        Vector3 edgeWorld = cameraRef.ViewportToWorldPoint(new Vector3(0.5f + _debugOrbitViewportRadius, 0.5f, viewDepth));

        orbitCenter = new Vector2(centerWorld.x, centerWorld.y);
        orbitRadius = Vector2.Distance(orbitCenter, new Vector2(edgeWorld.x, edgeWorld.y));
        return orbitRadius > 0.001f;
    }
}

public struct PlayerControllerLocalInputData {
    public Vector2 movementDirection;
    public Vector2 weaponAimDirection;
    public Vector2 debugOrbitCenter;
    public float debugOrbitRadius;
    public bool dashPressed;
    public bool attackPressed;
    public bool debugOrbitEnabled;
}