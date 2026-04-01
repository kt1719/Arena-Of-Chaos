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
    // ===== Events =====
    public event Action<SwordSwipe> OnSwordSwipe;

    // ===== Serialized Fields =====
    // [SerializeField] private PolygonCollider2D _swipeCollider;
    [SerializeField] private SwordHitbox _swordHitbox;

    // ===== Private Fields =====
    public SwordSwipe currentSwordSwipe { get; private set; }
    private readonly HashSet<NetworkId> _hitCache = new();

    public override void Spawned() {
        base.Spawned();
        currentSwordSwipe = SwordSwipe.DOWN;
    }
    
    protected override bool AttackAction()
    {
        if (Runner.IsForward) {
            OnSwordSwipe?.Invoke(currentSwordSwipe);
            UpdateWeaponState();
        }
        
        Hit();

        return true;
    }

    private void Hit()
    {
        // List<Collider2D> colliders = DetectPlayerHits();
        Vector2 hitDirection = GetHitDirection();
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
        targetPlayerCombat.ApplyHit(Object.InputAuthority, weaponInfo.weaponDamage, hitDirection);
    }

    private void PurgeHitCache()
    {
        _hitCache.Clear();
    }

    private Vector2 GetHitDirection()
    {
        return GameInput.Instance.GetWeaponAimDirection(playerCombat.transform);
    }

    private List<LagCompensatedHit> DetectPlayerHits(Vector2 aimDirection)
    {
        var hits = new List<LagCompensatedHit>();
        var filtered = new List<LagCompensatedHit>();

        Vector3 origin = playerCombat.transform.position;
        Debug.Log($"Origin player combat: {origin}");

        Runner.LagCompensation.OverlapSphere(
            origin,
            _swordHitbox.attackRange,
            Object.InputAuthority,
            hits,
            options: HitOptions.SubtickAccuracy // To make it more precise
        );

        foreach (var hit in hits)
        {
            if (HisIsInvalid(hit)) continue;

            if (_swordHitbox.IsInsideArc(aimDirection, origin, hit.Hitbox.transform.position))
            {
                filtered.Add(hit);
            }
        }

        return filtered;
    }

    private bool HisIsInvalid(LagCompensatedHit hit)
    {
        return hit.Hitbox == null || hit.Hitbox.Root.Object.InputAuthority == Object.InputAuthority;
    }

    private void UpdateWeaponState()
    {
        byte currentWeaponState = (byte)currentSwordSwipe;
        currentSwordSwipe = (SwordSwipe)currentWeaponState == SwordSwipe.UP ? SwordSwipe.DOWN : SwordSwipe.UP;
        SetWeaponState((byte)currentSwordSwipe);
    }
}
