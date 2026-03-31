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
    [Networked] public int swingStartTick { get; protected set; }
    // ===== Events =====
    public event Action<SwordSwipe> OnSwordSwipe;

    // ===== Serialized Fields =====
    [SerializeField] private PolygonCollider2D _swipeCollider;
    [SerializeField] private SwordVisual _swordVisual;

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
        List<Collider2D> colliders = DetectPlayerHits();
        Vector2 hitDirection = GetHitDirection();

        foreach (Collider2D collider in colliders)
        {
            NetworkObject targetNetObj = collider.GetComponentInParent<NetworkObject>();
            PlayerCombat targetPlayerCombat = targetNetObj.GetComponent<PlayerCombat>();

            if (targetNetObj == null || targetPlayerCombat == null) continue;

            // Hit the target and apply via PlayerCombat
            _hitCache.Add(targetNetObj.Id);
            targetPlayerCombat.ApplyHit(Object.InputAuthority, weaponInfo.weaponDamage, hitDirection);
        }

        PurgeHitCache();
    }

    private void PurgeHitCache()
    {
        _hitCache.Clear();
    }

    private Vector2 GetHitDirection()
    {
        return GameInput.Instance.GetWeaponAimDirection(playerCombat.transform);
    }

    private List<Collider2D> DetectPlayerHits()
    {
        List<Collider2D> colliders = new List<Collider2D>();
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.useTriggers = true;
        contactFilter.useLayerMask = true;
        contactFilter.layerMask = LayerMask.GetMask("PlayerHurtBox");
        Physics2D.OverlapCollider(_swipeCollider, contactFilter, colliders);
        return colliders;
    }

    private void UpdateWeaponState()
    {
        byte currentWeaponState = (byte)currentSwordSwipe;
        currentSwordSwipe = (SwordSwipe)currentWeaponState == SwordSwipe.UP ? SwordSwipe.DOWN : SwordSwipe.UP;
        SetWeaponState((byte)currentSwordSwipe);
    }
}
