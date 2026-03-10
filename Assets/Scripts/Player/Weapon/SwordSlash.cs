using UnityEngine;

public class SwordSlash : MonoBehaviour
{
    [SerializeField] private SwordWeapon swordWeapon;
    [SerializeField] private Transform slashAnimPrefab;
    [SerializeField] private Transform slashAnimSpawnPoint;

    private Transform slashAnim;
    
    private void Start()
    {
        swordWeapon.OnPlayerAttack += SwordWeapon_OnPlayerAttack;
    }

    private void SwordWeapon_OnPlayerAttack()
    {
        slashAnim = Instantiate(slashAnimPrefab, slashAnimSpawnPoint.transform.position, transform.parent.rotation);
        slashAnim.parent = transform;

        if ((SwordWeapon.SwordSwipe) swordWeapon.WeaponState == SwordWeapon.SwordSwipe.Up)
        {
            SwingUpFlipAnim();
        }
        
        slashAnim.parent = null; // De-attach slash from parent gameobject so it does not follow the player
    }

    public void SwingUpFlipAnim()
    {
        slashAnim.localRotation = Quaternion.Euler(-180f, 0f, 0f);
    }
}
