using System;
using Fusion;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{

    // ===== Events =====
    public event Action OnPlayerAttack;

    // ===== Networked Fields =====
    [Networked] private BaseWeapon _currentWeapon { get; set; }

    // ===== Serialized Fields =====
    [SerializeField] private WeaponInfo _testWeapon;
    [SerializeField] private NetworkObject _activeWeaponTransform;

    public override void Spawned() {
        SpawnWeapon(_testWeapon);
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        Runner.Despawn(_currentWeapon.Object);
    }

    public override void Render() {

    }

    private void SpawnWeapon(WeaponInfo weaponInfo) {
        if (HasStateAuthority) {
            BaseWeapon weaponPrefab = weaponInfo.weaponPrefab;
            _currentWeapon = Runner.Spawn(weaponPrefab, Vector3.zero, Quaternion.identity, Object.InputAuthority, (runner, o) =>
            {
                o.GetComponent<BaseWeapon>().Init(_testWeapon.instantiationOffset, _activeWeaponTransform);
            });
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            bool attackPressed = data.buttons.IsSet(NetworkInputData.ATTACK);
            Attack(attackPressed);
        }
    }

    private void Attack(bool attackPressed) {
        if (!attackPressed) return;

        _currentWeapon.Attack();
    }
}
