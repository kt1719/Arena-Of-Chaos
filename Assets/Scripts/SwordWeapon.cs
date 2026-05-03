using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

// ===== State Enum=====
public enum SwordSwipe 
{
    UP,
    DOWN
}

public class SwordWeapon : BaseWeapon
{
    // ===== Networked Fields =====
    [Networked] private Vector2 WeaponAimDirection { get; set; }
    
    // ===== Events =====
    public event Action<SwordSwipe> OnSwordSwipe;

    // ===== Serialized Fields =====
    [SerializeField] private SwordHitbox swordHitbox;
    [SerializeField] private EnvironmentInteractible environmentInteractible;

    // ===== Private Fields =====
    public SwordSwipe CurrentSwordSwipe { get; private set; }
    private readonly HashSet<NetworkId> _hitCache = new();

    public override void Spawned() {
        base.Spawned();
        CurrentSwordSwipe = SwordSwipe.DOWN;
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (GetInput(out NetworkInputData data))
        {
            Vector2 weaponAimDirection = data.weaponAimDirection;

            WeaponAimDirection = weaponAimDirection;
        }
    }
    
    protected override bool AttackAction()
    {
        if (Runner.IsForward) {
            OnSwordSwipe?.Invoke(CurrentSwordSwipe);
            UpdateWeaponState();
        }
        
        HitEnv();
        HitTargets();

        return true;
    }

    private void HitEnv() {
        environmentInteractible.HitEnvironments();
    }

    private void HitTargets()
    {
        Vector2 hitDirection = WeaponAimDirection;

        List<LagCompensatedHit> hits = DetectHits(hitDirection);

        foreach (var hit in hits)
        {
            ApplyHit(hitDirection, hit);
        }

        PurgeHitCache();
    }

    private void ApplyHit(Vector2 hitDirection, LagCompensatedHit hit)
    {
        if (hit.Hitbox == null) return;

        NetworkObject targetNetObj = hit.Hitbox.Root.Object;
        if (targetNetObj == null || targetNetObj == playerCombat || _hitCache.Contains(targetNetObj.Id)) return;

        IHittable hittable = targetNetObj.GetComponent<IHittable>();
        if (hittable == null) return;

        _hitCache.Add(targetNetObj.Id);
        hittable.ApplyHit(weaponInfo.weaponDamage, hitDirection, weaponInfo.knockbackForce, weaponInfo.knockbackDuration, Object.InputAuthority);
    }

    private void PurgeHitCache()
    {
        _hitCache.Clear();
    }

    private List<LagCompensatedHit> DetectHits(Vector2 aimDirection)
    {
        var hits = new List<LagCompensatedHit>();
        var filtered = new List<LagCompensatedHit>();

        Vector3 origin = playerCombat.transform.position;

        Runner.LagCompensation.OverlapSphere(
            origin,
            swordHitbox.attackRange,
            Object.InputAuthority,
            hits,
            options: HitOptions.SubtickAccuracy // To make it more precise
        );

        foreach (var hit in hits)
        {
            if (HitIsInvalid(hit)) continue;

            if (swordHitbox.IsInsideArc(aimDirection, origin, hit.Hitbox.transform.position))
            {
                filtered.Add(hit);
            }
        }

        return filtered;
    }

    private bool HitIsInvalid(LagCompensatedHit hit)
    {
        return hit.Hitbox == null || hit.Hitbox.Root.Object.InputAuthority == Object.InputAuthority;
    }

    private void OnDrawGizmos()
    {
        if (swordHitbox == null || !Object || !Object.IsValid) return;
        swordHitbox.aimDirectionDebug = WeaponAimDirection;
    }

    private void UpdateWeaponState()
    {
        byte currentWeaponState = (byte)CurrentSwordSwipe;
        CurrentSwordSwipe = (SwordSwipe)currentWeaponState == SwordSwipe.UP ? SwordSwipe.DOWN : SwordSwipe.UP;
        SetWeaponState((byte)CurrentSwordSwipe);
    }
}
