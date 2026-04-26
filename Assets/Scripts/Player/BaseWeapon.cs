using System;
using Fusion;
using UnityEngine;

public abstract class BaseWeapon : NetworkBehaviour {

    // ===== Networked Fields =====
    [Networked] protected byte weaponState { get; private set; }
    [Networked] protected NetworkObject weaponParent { get; private set; }
    [Networked] protected NetworkObject playerCombat { get; private set; }
    [Networked] protected Vector3 weaponInstantiationOffset { get; private set; }
    [Networked] protected float weaponCooldown { get; private set; }

    // ===== Serialized Fields =====
    [SerializeField] protected WeaponInfo weaponInfo;

    // ===== Private Fields =====
    protected abstract bool AttackAction();

    public bool Attack() {
        if (weaponCooldown > 0) return false;

        ResetWeaponCooldown();

        bool result = AttackAction();
        return result;
    }

    public override void Spawned() {
        ApplyParenting();
    }

    public override void Render() {
        if (weaponParent != null && transform.parent != weaponParent.transform) {
            ApplyParenting();
        }
    }

    private void ApplyParenting() {
        transform.parent = weaponParent != null ? weaponParent.transform : null;
        transform.localPosition = new Vector3(weaponInstantiationOffset.x, weaponInstantiationOffset.y, 0);
    }

    public override void FixedUpdateNetwork()
    {
        UpdateWeaponCooldown();
    }

    private void UpdateWeaponCooldown()
    {
        if (weaponCooldown > 0)
        {
            weaponCooldown = Mathf.Max(0, weaponCooldown - Runner.DeltaTime);
        }
    }

    private void ResetWeaponCooldown()
    {
        weaponCooldown = weaponInfo.weaponCooldown;
    }

    // Called before Spawned()
    public void Init(Vector2 weaponInstantiationOffset, NetworkObject weaponParent, NetworkObject playerCombat) {
        this.weaponParent = weaponParent;
        this.playerCombat = playerCombat;
        this.weaponInstantiationOffset = new Vector3(weaponInstantiationOffset.x, weaponInstantiationOffset.y, 0);
    }

    public void SetWeaponState(byte weaponState) {
        if (HasStateAuthority) {
            this.weaponState = weaponState;
        }
    }

    public byte GetWeaponState() => weaponState;
    public float GetWeaponTotalCoolDown() => weaponInfo.weaponCooldown;
}