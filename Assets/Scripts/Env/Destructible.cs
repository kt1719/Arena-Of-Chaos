using System.Collections;
using Fusion;
using UnityEngine;

public class Destructible : NetworkBehaviour
{
    [SerializeField] private Transform destroyVFX;

    public void DestroyGameObject() {
        if (!HasStateAuthority) return;

        Runner.Despawn(this.Object);
    }

    private void OnDestroy() {
        PlayVFX();
    }

    private void PlayVFX() {
        if (!destroyVFX) return;

        Instantiate(destroyVFX, transform.position, Quaternion.identity);
    }
}
