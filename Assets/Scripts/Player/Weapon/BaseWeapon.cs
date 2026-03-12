using Photon.Realtime;
using UnityEngine;

public abstract class BaseWeapon : MonoBehaviour, IWeapon
{
    [SerializeField] protected WeaponInfo weaponInfo;

    public abstract void Attack();
    public abstract void InstantiateWeapon();
    
    public PlayerController player { get; protected set; }
    public byte WeaponState { get; protected set; }

    public virtual float GetWeaponCD()
    {
        if (!weaponInfo)
        {
            Debug.LogError("No Weapon Info Assigned!");
        }
        
        return weaponInfo.weaponCooldown;
    }

    // For storing custom weapon states
    public virtual void UpdateCurrentWeaponState(byte newWeaponState)
    {
        WeaponState = newWeaponState;
    }

    public virtual byte GetCurrentWeaponState()
    {
        return WeaponState;
    }

    public void SetPlayer(PlayerController player)
    {
        this.player = player;
    }
}