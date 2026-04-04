using System;
using Fusion;
using UnityEngine;

public class SwordSlash : NetworkBehaviour
{
    // ===== Serialized Fields =====
    [SerializeField] private SwordWeapon swordWeapon;
    [SerializeField] private Transform slashAnimPrefab;
    [SerializeField] private Transform slashAnimSpawnPoint;

    // ===== Private Fields =====
    private ChangeDetector _changeDetector;

    public override void Spawned() {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }
    
    private void Start()
    {
        swordWeapon.OnSwordSwipe += QueueSlash;
    }
    
    private void QueueSlash(SwordSwipe swipe) {
        if (HasStateAuthority) {
            RPC_SlashProxies(swipe);
        }
        SpawnSlash(swipe);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_SlashProxies(SwordSwipe currentSwordSwipe, RpcInfo info = default)
    {
        SpawnSlash(currentSwordSwipe);
    }

    

    private void SpawnSlash(SwordSwipe currentSwordSwipe)
    {
        Debug.Log("Instnatiating Slash Anim");

        Transform slashAnim;
        slashAnim = Instantiate(slashAnimPrefab, slashAnimSpawnPoint.transform.position, transform.parent.rotation);
        slashAnim.parent = transform;

        if (currentSwordSwipe == SwordSwipe.UP)
        {
            SwingUpFlipAnim(slashAnim);
        }
        else {
            slashAnim.localRotation = Quaternion.identity;
        }
        
        slashAnim.parent = null; // De-attach slash from parent gameobject so it does not follow the player
    }

    private void SwingUpFlipAnim(Transform slashAnim)
    {
        slashAnim.localRotation = Quaternion.Euler(-180f, 0f, 0f);
    }
}
