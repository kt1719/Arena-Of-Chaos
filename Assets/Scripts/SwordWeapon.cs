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
    [Networked] private Vector2 _weaponAimDirection { get; set; } // Only networked for debugging purposes - this can be private
    // ===== Events =====
    public event Action<SwordSwipe> OnSwordSwipe;

    // ===== Serialized Fields =====
    [SerializeField] private SwordHitbox swordHitbox;

    // ===== Private Fields =====
    public SwordSwipe currentSwordSwipe { get; private set; }
    private readonly HashSet<NetworkId> _hitCache = new();

    public override void Spawned() {
        base.Spawned();
        currentSwordSwipe = SwordSwipe.DOWN;
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (GetInput(out NetworkInputData data))
        {
            Vector2 weaponAimDirection = data.weaponAimDirection;

            _weaponAimDirection = weaponAimDirection;
        }
    }
    
    protected override bool AttackAction()
    {
        if (Runner.IsForward) {
            Debug.Log($"Invoking OnSwordSwipe {Runner.Tick}");
            OnSwordSwipe?.Invoke(currentSwordSwipe);
            UpdateWeaponState();
        }
        
        Hit();

        return true;
    }

    private void Hit()
    {
        Vector2 hitDirection = _weaponAimDirection;
        List<LagCompensatedHit> hits = DetectPlayerHits(hitDirection);

        foreach (var hit in hits)
        {
            Debug.Log($"Hit: {hit.Hitbox.Root.Object.name}");
            ApplyHit(hitDirection, hit);
        }

        PurgeHitCache();
    }

    private void ApplyHit(Vector2 hitDirection, LagCompensatedHit hit)
    {
        if (hit.Hitbox == null) return;

        NetworkObject targetNetObj = hit.Hitbox.Root.Object;
        PlayerCombat targetPlayerCombat = targetNetObj.GetComponent<PlayerCombat>();
        if (targetNetObj == null || targetPlayerCombat == null || targetNetObj == Object || _hitCache.Contains(targetNetObj.Id)) return;

        _hitCache.Add(targetNetObj.Id);
        targetPlayerCombat.ApplyHit(Object.InputAuthority, weaponInfo.weaponDamage, hitDirection, weaponInfo.knockbackForce, weaponInfo.knockbackDuration);
    }

    private void PurgeHitCache()
    {
        _hitCache.Clear();
    }

    private List<LagCompensatedHit> DetectPlayerHits(Vector2 aimDirection)
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
        if (swordHitbox == null) return;
        swordHitbox.aimDirectionDebug = _weaponAimDirection;
    }

    private void UpdateWeaponState()
    {
        byte currentWeaponState = (byte)currentSwordSwipe;
        currentSwordSwipe = (SwordSwipe)currentWeaponState == SwordSwipe.UP ? SwordSwipe.DOWN : SwordSwipe.UP;
        SetWeaponState((byte)currentSwordSwipe);
    }
}
