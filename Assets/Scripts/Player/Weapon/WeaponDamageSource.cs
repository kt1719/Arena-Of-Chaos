using UnityEngine;

public class WeaponDamageSource : DamageSource
{
    private GameObject player;
    private WeaponInfo weaponInfo;

    public void UpdateDamageSource(WeaponInfo weaponInfo, GameObject player)
    {
        this.weaponInfo = weaponInfo;
        this.player = player;
    }

    protected void OnTriggerEnter2D(Collider2D collision)
    {
        if (!weaponInfo)
        {
            Debug.LogError(gameObject.name + ": Did not call UpdateDamageSource upon instantiation");
        }
        if (collision.gameObject == player) return;
        IHittable hittableGameObject = collision.gameObject.GetComponent<IHittable>();
        hittableGameObject?.TakeDamage(weaponInfo.weaponDamage, player.transform);
    }
}
