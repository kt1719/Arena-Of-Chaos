using System.Collections.Generic;
using Fusion;
using UnityEngine;

// Environments should only be breakable on Host/State Authority
public class EnvironmentInteractible : NetworkBehaviour {
    [SerializeField] private Collider2D envCollider;

    public override void Spawned()
    {
        ChangeColliderState(true);
    }

    public void HitEnvironments() {
        if (!HasStateAuthority) return;

        List<Collider2D> envColliders = GetDestructiblesHit();

        foreach(var collider in envColliders) {
            Destructible destructible = collider.GetComponent<Destructible>();

            if (!destructible) continue;

            destructible.DestroyGameObject();
        }
    }

    private List<Collider2D> GetDestructiblesHit() {
        // Get the things that the envCollider has collided with
        List<Collider2D> results = new List<Collider2D>();

        Physics2D.OverlapCollider(envCollider, ContactFilter2D.noFilter, results);
        return results;
    }

    public void SetCollider(Collider2D collider2D) {
        envCollider = collider2D;
    }

    private void ChangeColliderState(bool state) {
        envCollider.enabled = state;
    }
}