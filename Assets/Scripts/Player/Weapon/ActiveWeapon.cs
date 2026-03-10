using System.Collections;
using UnityEngine;

public class ActiveWeapon : MonoBehaviour
{
    [SerializeField] PlayerController playerController;

    private BaseWeapon currentActiveWeapon;
    private Coroutine attackCoroutine;

    [SerializeField] private WeaponInfo testWeapon;

    private void Start()
    {
        TestWeaponSpawn();
    }

    private void TestWeaponSpawn()
    {
        if (testWeapon == null) return;

        SpawnWeapon(testWeapon);
    }

    private void SpawnWeapon(WeaponInfo weaponInfo)
    {
        GameObject newWeapon = Instantiate(weaponInfo.weaponPrefab, this.transform);
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localRotation = Quaternion.identity;
        currentActiveWeapon = newWeapon.GetComponent<BaseWeapon>();
        currentActiveWeapon.SetPlayer(playerController);
        transform.localPosition = new Vector3(testWeapon.instantiationOffset.x, testWeapon.instantiationOffset.y, 0f);
    }

    private void OnDestroy()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
    }

    public void Attack()
    {
        if (attackCoroutine != null) return; // If a coroutine is already running, then don't start another attack coroutine
        if (!currentActiveWeapon) return; // If player is not holding a weapon then return

        attackCoroutine = StartCoroutine(AttackCDRoutine());
    }

    private IEnumerator AttackCDRoutine()
    {
        currentActiveWeapon.Attack();
        yield return new WaitForSeconds(currentActiveWeapon.GetWeaponCD());
        attackCoroutine = null;
    }

    public void UpdatePlayerFacingDirection(Vector2 directionFromPlayer, bool facingLeft)
    {
        float offset = CalculateMouseFollowWithOffset(directionFromPlayer);
        if (facingLeft)
        {
            transform.localRotation = Quaternion.Euler(0, 180f, offset);
        }
        else
        {
            transform.localRotation = Quaternion.Euler(0, 0, offset);
        }
    }

    private float CalculateMouseFollowWithOffset(Vector2 directionFromPlayer)
    {
        float angle = Mathf.Atan2(directionFromPlayer.y, Mathf.Abs(directionFromPlayer.x)) * Mathf.Rad2Deg;

        return angle;
    }
}
