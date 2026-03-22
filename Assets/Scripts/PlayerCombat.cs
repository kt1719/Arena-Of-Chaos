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
    [SerializeField] private NetworkObject _weaponParent;

    public override void Spawned() {
        SpawnWeapon(_testWeapon);
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        if (!HasInputAuthority) return;

        Runner.Despawn(_currentWeapon.Object);
    }

    public override void Render() {

    }

    private void SpawnWeapon(WeaponInfo weaponInfo) {
        if (HasStateAuthority) {
            BaseWeapon weaponPrefab = weaponInfo.weaponPrefab;
            _currentWeapon = Runner.Spawn(weaponPrefab, Vector3.zero, Quaternion.identity, Object.InputAuthority, (runner, o) =>
            {
                o.GetComponent<BaseWeapon>().Init(_testWeapon.instantiationOffset, _weaponParent);
            });
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            // Input
            bool attackPressed = data.buttons.IsSet(NetworkInputData.ATTACK);
            Vector2 weaponAimDirection = data.weaponAimDirection;

            // Action
            UpdatePlayerFacingDirection(weaponAimDirection);
            Attack(attackPressed);
        }
    }

    private void Attack(bool attackPressed) {
        if (!attackPressed) return;

        _currentWeapon.Attack();
    }

    private void UpdatePlayerFacingDirection(Vector2 weaponAimDirection)
    {
        float offset = CalculateMouseFollowWithOffset(weaponAimDirection);
        float yRotation = weaponAimDirection.x < 0 ? 180f : 0f;
        _currentWeapon.transform.localRotation = Quaternion.Euler(0, yRotation, offset);
    }

    private float CalculateMouseFollowWithOffset(Vector2 weaponAimDirection)
    {
        return Mathf.Atan2(weaponAimDirection.y, Mathf.Abs(weaponAimDirection.x)) * Mathf.Rad2Deg;
    }
}
