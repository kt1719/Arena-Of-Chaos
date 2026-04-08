using Fusion;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour, IHittable
{

    // ===== Networked Fields =====
    [Networked] private BaseWeapon CurrentWeapon { get; set; }

    // ===== Serialized Fields =====
    [SerializeField] private WeaponInfo _testWeapon;
    [SerializeField] private NetworkObject _weaponParent;
    [SerializeField] private PlayerVisual _playerVisual;
    [SerializeField] private Knockback _playerKnockback;

    // ===== Private Variables =====
    private PlayerStats _stats;

    public override void Spawned() {
        _stats = GetComponent<PlayerStats>();
        if (_stats == null)
        {
            Debug.LogError($"[PlayerCombat] PlayerStats component not found on {gameObject.name}. Combat will not function correctly.", this);
            return;
        }

        SpawnWeapon(_testWeapon);
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        if (!HasInputAuthority) return;

        Runner.Despawn(CurrentWeapon.Object);
    }

    public override void Render() {

    }

    private void SpawnWeapon(WeaponInfo weaponInfo) {
        if (HasStateAuthority) {
            BaseWeapon weaponPrefab = weaponInfo.weaponPrefab;
            CurrentWeapon = Runner.Spawn(weaponPrefab, Vector3.zero, Quaternion.identity, Object.InputAuthority, (runner, o) =>
            {
                o.GetComponent<BaseWeapon>().Init(_testWeapon.instantiationOffset, _weaponParent, Object);
            });
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (_stats == null || CurrentWeapon == null) return;

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

    public void ApplyHit(int damage, Vector2 hitDirection, float knockbackForce, float knockbackDuration) {
        if (!HasStateAuthority) return;

        RPC_TriggerHitFlash();

        // TO-DO: Damage the player - Queue to be consumed on FixedUpdateNetwork

        _playerKnockback.ApplyKnockback(hitDirection, knockbackForce, knockbackDuration);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]
    private void RPC_TriggerHitFlash() {
        _playerVisual.TriggerHitFlash();
    }

    private void Attack(bool attackPressed) {
        if (!attackPressed) return;

        CurrentWeapon.Attack();
    }

    private void UpdatePlayerFacingDirection(Vector2 weaponAimDirection)
    {
        float offset = CalculateMouseFollowWithOffset(weaponAimDirection);
        float yRotation = weaponAimDirection.x < 0 ? 180f : 0f;
        CurrentWeapon.transform.localRotation = Quaternion.Euler(0, yRotation, offset);
    }

    private float CalculateMouseFollowWithOffset(Vector2 weaponAimDirection)
    {
        return Mathf.Atan2(weaponAimDirection.y, Mathf.Abs(weaponAimDirection.x)) * Mathf.Rad2Deg;
    }
}
