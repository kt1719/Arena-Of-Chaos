using System.Collections;
using Fusion;
using UnityEngine;

public class Destructible : NetworkBehaviour
{
    [SerializeField] private Transform destroyVFX;

    private bool _isQuitting;

    void OnApplicationQuit()
    {
        _isQuitting = true;
    }


    public void DestroyGameObject() {
        if (!HasStateAuthority) return;

        Runner.Despawn(this.Object);
    }

    private void OnDestroy() {
        if (_isQuitting) return;
        
        PlayVFX();
    }

    private void PlayVFX() {
        if (!destroyVFX) return;

        Instantiate(destroyVFX, transform.position, Quaternion.identity);
    }
}
