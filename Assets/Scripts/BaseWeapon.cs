using System;
using Fusion;
using UnityEngine;

public abstract class BaseWeapon : NetworkBehaviour {

    // ===== Networked Fields =====
    [Networked] public byte weaponState { get; protected set; }
    [Networked] public NetworkObject weaponParent { get; protected set; }
    [Networked] public NetworkObject playerCombat { get; protected set; }
    [Networked] public Vector3 weaponInstantiationOffset { get; protected set; }
    [Networked] public float weaponCooldown { get; protected set; }

    // ===== Serialized Fields =====
    [SerializeField] protected WeaponInfo weaponInfo;

    // ===== Private Fields =====
    private ChangeDetector _changeDetector;
    protected abstract bool AttackAction();

    public bool Attack() {
        if (weaponCooldown > 0) return false;

        ResetWeaponCooldown();
        
        bool result = AttackAction();
        return result;
    }

    public override void Spawned() {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (weaponParent != null) {
            transform.parent = weaponParent.transform;
            transform.localPosition = new Vector3(weaponInstantiationOffset.x, weaponInstantiationOffset.y, 0);
        }
    }

    public override void Render() {
        foreach (var change in _changeDetector.DetectChanges(this)) {
            switch (change) {
                case nameof(weaponParent):
                    transform.parent = weaponParent != null ? weaponParent.transform : null;
                    transform.localPosition = new Vector3(weaponInstantiationOffset.x, weaponInstantiationOffset.y, 0);
                    break;
            }
        }
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