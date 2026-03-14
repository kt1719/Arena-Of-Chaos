using Fusion;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    private NetworkCharacterController _cc;
    private void Awake() {
        _cc = GetComponent<NetworkCharacterController>();
    }

    public override void Spawned()
    {
        
    }
    public override void Render()
    {
        
    }
    public override void FixedUpdateNetwork()
    {

    }
}
