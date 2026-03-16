using Fusion;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    private ChangeDetector _changeDetector;
    public override void Spawned() {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        Init();
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {

    }

    public override void Render() {

    }

    private void Init() {

    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            bool attackPressed = data.buttons.IsSet(NetworkInputData.ATTACK);

            if (attackPressed) {
                Debug.Log("Attack pressed");
            }
        }
    }
}
