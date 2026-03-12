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
        currentActiveWeapon.InstantiateWeapon();
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

    public bool Attack(bool remoteServerAttack = false)
    {
        // If the attack is coming from the remote server, then we don't need to check for cooldown or weapon state
        if (remoteServerAttack)
        {
            currentActiveWeapon.Attack();
            return true;
        }

        if (attackCoroutine != null) return false; // If a coroutine is already running, then don't start another attack coroutine
        if (!currentActiveWeapon) return false; // If player is not holding a weapon then return

        attackCoroutine = StartCoroutine(AttackCDRoutine());
        return true;
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

    public void UpdateWeaponState(byte weaponState)
    {
        if (!currentActiveWeapon) {
            Debug.LogError("No active weapon found");
            return;
        }

        currentActiveWeapon.UpdateCurrentWeaponState(weaponState);
    }

    public byte GetCurrentWeaponState()
    {
        if (!currentActiveWeapon) {
            Debug.LogError("No active weapon found");
            return 0;
        }

        return currentActiveWeapon.GetCurrentWeaponState();
    }
}
