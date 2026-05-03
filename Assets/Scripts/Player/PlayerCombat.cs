using Fusion;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour, IHittable
{

    // ===== Networked Fields =====
    [Networked] private BaseWeapon CurrentWeapon { get; set; }
    [Networked] public NetworkBool IsDead { get; private set; }
    [Networked] private TickTimer RespawnTimer { get; set; }

    // ===== Serialized Fields =====
    [SerializeField] private WeaponInfo _testWeapon;
    [SerializeField] private NetworkObject _weaponParent;
    [SerializeField] private PlayerVisual _playerVisual;
    [SerializeField] private Knockback _playerKnockback;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private float _respawnDelay = 3f;

    // ===== Private Variables =====
    private PlayerStats _stats;
    private Collider2D _collider;

    public override void Spawned() {
        _stats = GetComponent<PlayerStats>();
        _collider = GetComponent<Collider2D>();

        if (_stats == null)
        {
            Debug.LogError($"[PlayerCombat] PlayerStats component not found on {gameObject.name}. Combat will not function correctly.", this);
            return;
        }

        if (HasStateAuthority)
        {
            _playerKnockback.OnKnockbackEnd += CheckDeath;
        }

        SpawnWeapon(_testWeapon);
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        if (HasStateAuthority && _playerKnockback != null)
        {
            _playerKnockback.OnKnockbackEnd -= CheckDeath;
        }

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

        if (HasStateAuthority && IsDead)
        {
            if (RespawnTimer.Expired(Runner))
            {
                Respawn();
            }
            return;
        }

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
        if (IsDead) return;
        if (_playerMovement.IsDashing) return;

        _stats.CurrentHealth = Mathf.Max(0, _stats.CurrentHealth - damage);

        RPC_TriggerHitFlash();
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

    // ===== Death & Respawn =====

    private void CheckDeath() {
        if (_stats.CurrentHealth <= 0)
            Die();
    }

    private void Die() {
        if (!HasStateAuthority || IsDead) return;

        IsDead = true;
        RespawnTimer = TickTimer.CreateFromSeconds(Runner, _respawnDelay);

        if (_collider != null) _collider.enabled = false;
        PlayerManager.Instance.DisablePlayer(Object.InputAuthority);
    }

    private void Respawn() {
        if (!HasStateAuthority) return;

        IsDead = false;

        _stats.CurrentHealth = _stats.MaxHealth;
        if (_collider != null) _collider.enabled = true;

        Vector3 spawnPosition = GameManager.Instance.GetPlayerSpawnPoint(Object.InputAuthority);
        transform.position = spawnPosition;

        PlayerManager.Instance.EnablePlayer(Object.InputAuthority);
    }

    /// <summary>
    /// Called by GameManager on round start to cleanly reset a player who is still dead.
    /// </summary>
    public void ForceResetDeathState() {
        if (!HasStateAuthority) return;

        IsDead = false;
        RespawnTimer = default;
        if (_collider != null) _collider.enabled = true;
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
