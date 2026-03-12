using UnityEngine;

public class WeaponDamageSource : DamageSource
{
    private GameObject player;
    private WeaponInfo weaponInfo;

    public void UpdateDamageSource(WeaponInfo weaponInfo, GameObject player)
    {
        Debug.Log("Updating damage source for player: " + player.name);
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
        Debug.Log("Dealing damage: " + weaponInfo.weaponDamage);
        hittableGameObject?.TakeDamage(weaponInfo.weaponDamage, player.transform, weaponInfo.knockbackForce, weaponInfo.knockbackDuration);
    }
}
