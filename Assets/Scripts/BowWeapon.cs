using System;
using Fusion;
using UnityEngine;

public class BowWeapon : BaseWeapon
{
    // ===== Events =====
    public event Action OnBowShoot;

    // ===== Networked Fields =====
    [Networked] private Vector2 WeaponAimDirection { get; set; }

    // ===== Serialized Fields =====
    [SerializeField] private NetworkPrefabRef arrowPrefab;
    [SerializeField] private Transform arrowSpawnPoint;

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (GetInput(out NetworkInputData data))
        {
            WeaponAimDirection = data.weaponAimDirection;
        }
    }

    protected override bool AttackAction()
    {
        SpawnArrow();

        if (Runner.IsForward)
        {
            OnBowShoot?.Invoke();
        }

        return true;
    }

    private void SpawnArrow()
    {
        if (!HasStateAuthority) return;

        Vector2 aimDir = WeaponAimDirection.normalized;
        if (aimDir.sqrMagnitude < 0.01f) aimDir = Vector2.right;

        Vector3 spawnPos = arrowSpawnPoint.position;
        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        Runner.Spawn(arrowPrefab, spawnPos, rotation, Object.InputAuthority, (runner, o) =>
        {
            o.GetComponent<Arrow>().Init(
                aimDir,
                weaponInfo.weaponDamage,
                weaponInfo.knockbackForce,
                weaponInfo.knockbackDuration
            );
        });
    }
}
