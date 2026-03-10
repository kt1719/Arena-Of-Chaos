using System;
using UnityEngine;

public class SwordWeapon : BaseWeapon
{
    public event Action OnPlayerAttack;

    [SerializeField] private WeaponDamageSource damageSource;

    // To be used in Weapon States
    public enum SwordSwipe
    {
        Up,
        Down
    }

    public override void InstantiateWeapon()
    {
        WeaponState = (byte)SwordSwipe.Down;
        damageSource.UpdateDamageSource(this.weaponInfo, player.gameObject);
    }
    
    public override void Attack()
    {
        OnPlayerAttack.Invoke();
        SwordSwipe nextSwipe = (SwordSwipe) WeaponState == SwordSwipe.Up ? SwordSwipe.Down : SwordSwipe.Up;
        WeaponState = (byte)nextSwipe;
    }

    public override float GetWeaponCD()
    {
        return weaponInfo.weaponCooldown;
    }

    public void UpdateSwordWeaponState(SwordSwipe currentSwipe)
    {
        UpdateCurrentWeaponState((byte) currentSwipe);
    }
}
