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
    [SerializeField] private PolygonCollider2D _swipeCollider;
    [SerializeField] private SwordVisual _swordVisual;

    // ===== Private Fields =====
    public SwordSwipe currentSwordSwipe { get; private set; }

    public override void Spawned() {
        base.Spawned();
        currentSwordSwipe = SwordSwipe.DOWN;
    }
    
    protected override bool AttackAction()
    {
        if (Runner.IsForward) {
            // Only invoke for forward ticks so we don't invoke multiple times on re-simulations.
            OnSwordSwipe?.Invoke(currentSwordSwipe);
        }

        UpdateWeaponState();
        Hit();

        return true;
    }

    private void Hit()
    {
        List<Collider2D> colliders = new List<Collider2D>();
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.useTriggers = true;
        contactFilter.useLayerMask = true;
        contactFilter.layerMask = LayerMask.GetMask("PlayerHurtBox");
        int count = Physics2D.OverlapCollider(_swipeCollider, contactFilter, colliders);
        
        // Hit direction is the direction of the sword
        Vector2 hitDirection = transform.right;
        float knockbackForce = weaponInfo.knockbackForce;
        float knockbackDuration = weaponInfo.knockbackDuration;

        for (int i = 0; i < count; i++)
        {
            Collider2D collider = colliders[i];
            // Potentially apply a hit to the target.
            Debug.Log("SwordWeapon: Hit " + collider.transform.name);
            PlayerHurtBox playerHurtBox = collider.GetComponent<PlayerHurtBox>();
            PlayerKnockback playerKnockback = playerHurtBox.GetPlayerKnockback();
            playerKnockback.ApplyKnockback(hitDirection, knockbackForce, knockbackDuration);
        }
    }

    private void UpdateWeaponState()
    {
        byte currentWeaponState = (byte)currentSwordSwipe;
        currentSwordSwipe = (SwordSwipe)currentWeaponState == SwordSwipe.UP ? SwordSwipe.DOWN : SwordSwipe.UP;
        SetWeaponState((byte)currentSwordSwipe);
    }
}
