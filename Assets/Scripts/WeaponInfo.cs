using UnityEngine;

[CreateAssetMenu(fileName = "WeaponInfo", menuName = "Scriptable Objects/WeaponInfo")]
public class WeaponInfo : ScriptableObject
{
    public BaseWeapon weaponPrefab;
    public float weaponCooldown;
    public float weaponAttackDuration;
    public float attackSpeed;
    public int weaponDamage;
    public int weaponRange;
    public float knockbackForce;
    public float knockbackDuration;

    public Vector2 instantiationOffset = Vector2.zero;
}
