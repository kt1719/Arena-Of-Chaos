using Fusion;
using UnityEngine;

public abstract class BaseWeapon : NetworkBehaviour {

    // ===== Networked Fields =====
    [Networked] public byte weaponState { get; protected set; }
    [Networked] public NetworkObject weaponParent { get; protected set; }
    [Networked] public Vector3 weaponInstantiationOffset { get; protected set; }

    // ===== Serialized Fields =====
    [SerializeField] protected WeaponInfo weaponInfo;

    // ===== Private Fields =====
    private ChangeDetector _changeDetector;

    public abstract void Attack();

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

    // Called before Spawned()
    public void Init(Vector2 weaponInstantiationOffset, NetworkObject weaponParent) {
        this.weaponParent = weaponParent;
        this.weaponInstantiationOffset = new Vector3(weaponInstantiationOffset.x, weaponInstantiationOffset.y, 0);
    }

    public void SetWeaponState(byte weaponState) {
        if (HasStateAuthority) {
            this.weaponState = weaponState;
        }
    }

    public byte GetWeaponState() => weaponState;
    public float GetWeaponCooldown() => weaponInfo.weaponCooldown;
}